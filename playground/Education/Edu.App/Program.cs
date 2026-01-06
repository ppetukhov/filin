// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Edu.ReadModels;
using Edu.UseCases;

using Filin;
using Filin.Configuration;
using Filin.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1303 // Do not pass literals as localized parameters
#pragma warning disable CA1848 // Use the LoggerMessage delegates

Console.WriteLine("Ins App 1");

#pragma warning disable IL3002 // Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file
var hostBuilder = CreateHostBuilder(args);
#pragma warning restore IL3002 // Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file

[RequiresAssemblyFiles("Calls Microsoft.Extensions.Hosting.FilinGenericHostExtensions.UseFilin(Action<IExecutorBuilder>)")]
static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseFilin()
        ;

var host = hostBuilder.Build();

host.Start();

var logger = host.Services.GetRequiredService<ILogger<Program>>();


var optionsLogger = host.Services.GetRequiredService<OptionsLogger>();

optionsLogger.LogOptions();

var apptLifeTime = host.Services.GetRequiredService<IHostApplicationLifetime>();

var eventStore = host.Services.GetRequiredService<IEventStore>();

_ = Task.Run(async () =>
{
    logger.LogInformation("Subscribed. Started");

    var queryStream = new DcbQuery([new DcbQueryItem(["CourseRenamed"], [])]);

    await foreach (var @event in eventStore.Subscribe(queryStream).WithCancellation(apptLifeTime.ApplicationStopping))
    {
        logger.LogInformation("Subscribed. Recived Event {Event}", @event);
    }

    logger.LogInformation("Subscribed. Finished");
});

var executor = host.Services.GetRequiredService<IDcbExecutor>();

var oldName = (string?)null;
var courseGuid = Guid.Parse("0F033F3C-32A6-4295-AB7B-B5F93FE56E6D");
var tasks = new List<Task<ActiveCourses?>>();

var getGetActiveCourses = new GetActiveCourses();

try
{
    var activeCourses = await executor.QueryAsync<GetActiveCourses, ActiveCourses>(getGetActiveCourses).ConfigureAwait(false);

    logger.LogInformation("Active courses {ActiveCourses}", activeCourses?.Courses.Select((k, v) => $"{k};").Aggregate((txt, item) => txt += " " + item));

    activeCourses?.Courses.TryGetValue(courseGuid, out oldName);
}
catch (Exception e)
{
    logger.LogError(e, "GetActiveCourses error");
}

var sw = Stopwatch.StartNew();

if (oldName is null)
{
    var command = new CreateCourse(courseGuid, "Course ADO and .Net");
    tasks.Add(executor.ExecuteAsync<CreateCourse, ActiveCourses>(command));
}

var renameCommands = new List<RenameCourse>();

foreach (var course in Enumerable.Range(1, 18))
{
    string CalcCourseName(int num) => $"Course .Net for beginner #{course}";

    var renameCommand = new RenameCourse(courseGuid, CalcCourseName(Random.Shared.Next(1, 11)), oldName ?? "Course ADO and .Net");

    renameCommands.Add(renameCommand);

    tasks.Add(executor.ExecuteAsync<RenameCourse, ActiveCourses>(renameCommand));
}

try
{
    await Task.WhenAll(tasks).ConfigureAwait(false);

    sw.Stop();
    logger.LogInformation("Commands WhenAll. Timetaken: {Timetaken} ms.", sw.ElapsedMilliseconds);
}
catch (Exception ex)
{
    sw.Stop();
    logger.LogError(ex, "Commands WhenAll exception. Timetaken: {Timetaken} ms.", sw.ElapsedMilliseconds);
}

var idx = 0;

foreach (var task in tasks)
{
    // task create cource
    if (idx == 0 && oldName is null)
    {
        try
        {
            var result = task.Result;

            logger.LogInformation("Course created: {Courses}", result);
        }
        catch (Exception exc)
        {
            logger.LogError(exc, "Create course error");
        }

        continue;
    }

    var command = renameCommands[idx++];

    var hasOneSuccess = false;
    try
    {
        var result = task.Result;

        if (hasOneSuccess)
        {
            logger.LogError("WRONG. Command: {Command}; Result: {Result}", command.NewName, result?.Courses.Select((k, v) => $"{k};").Aggregate((txt, item) => txt += " " + item));
        }
        else
        {
            hasOneSuccess = true;
            logger.LogInformation("Executed.       Command: {Command}; Result: {Result}", command.NewName, result?.Courses.Select((k, v) => $"{k};").Aggregate((txt, item) => txt += " " + item));
        }
    }
    catch (AggregateException ex)
    {
        LogExeption(command, ex.Flatten().InnerExceptions.First());
    }
    catch (Exception ex)
    {
        LogExeption(command, ex);
    }
}

void LogExeption(RenameCourse command, Exception ex)
{
    switch (ex)
    {
        case DcbConflictException conflict:
            logger.LogWarning("DcbConflict.    Command: {Command}", command.NewName);
            break;
        default:
            logger.LogError("Command: {Command}; Exception: {Ex}", command.NewName, ex.Message);
            break;
    }
}

if (tasks.Count is not 0)
{
    Console.WriteLine("Press any key to stop...");
    Console.ReadKey();

    await host.StopAsync().ConfigureAwait(false);

    return;
}

var query = new DcbQuery([new DcbQueryItem(["Created", "CourseBooked"], ["course:77"])]);

logger.LogInformation("Search query {Query}", query);

foreach (var (eventTypes, tags) in query.Items)
{
    foreach (var item in eventTypes)
    {
        logger.LogInformation("{HashCode}, {EventType}", item.GetHashCode(), item.ToString());
    }

    foreach (var item in tags)
    {
        logger.LogInformation("{HashCode}, {Tag}", item.GetHashCode(), item.ToString());
    }
}

logger.LogInformation("Call..");

using var cts = new CancellationTokenSource();

await foreach (var ev in eventStore.ReadAsync(query).WithCancellation(cts.Token).ConfigureAwait(false))
    logger.LogInformation("Event {Event}", ev);

logger.LogInformation("Search Completed");

logger.LogInformation("Append event");

var @event = new DcbEvent { EventType = "CourseBooked", Timestamp = DateTimeOffset.UtcNow, Tags = ["course:77", "user:129"], Data = new() };

try
{
    var failIfMatch = new DcbQuery([new DcbQueryItem([@event.EventType], [.. @event.Tags])]);

    var lastPosition = await eventStore.AppendAsync(@event, failIfMatch, cts.Token).ConfigureAwait(false);

    logger.LogInformation("{EventType} Success save {Event} at position {LastPosition}", @event.EventType, @event, lastPosition);
}
catch (DcbConflictException)
{
    logger.LogWarning("{EventType} Conflict save {Event}", @event.EventType, @event);
}

logger.LogInformation("Append event");

var evt = new DcbEvent() { EventType = "CourseBooked", Timestamp = DateTime.UtcNow, Tags = ["course:77"], Data = new() };

var events = new List<DcbEvent>
{
    evt with { Tags = evt.Tags.Add("user:134") },
    evt with { Tags = evt.Tags.Add("user:146") },
    evt with { Tags = evt.Tags.Add("user:152") },
    evt with { Tags = evt.Tags.Add("user:166") }
};

var after = new EventPosition(36);

try
{
    var failIfMatch = new DcbQuery([.. events.GroupBy(x => x.Tags).Select(x => new DcbQueryItem([.. x.Select(e => e.EventType)], [.. x.Key ?? []]))]);

    logger.LogInformation("failIfMatch {FailIfMatch}", failIfMatch);

    var lastPosition = await eventStore.AppendAsync(events, failIfMatch, after, cts.Token).ConfigureAwait(false);

    logger.LogInformation("{EventTypes} Success save at position {LastPosition}", string.Join(',', events.Select(x => x.EventType.ToString()).Distinct()), lastPosition);
}
catch (DcbConflictException)
{
    logger.LogWarning("{EventTypes} Conflict save after position {After}", string.Join(',', events.Select(x => x.EventType.ToString()).Distinct()), after);
}

await host.StopAsync().ConfigureAwait(false);

#pragma warning restore CA1848 // Use the LoggerMessage delegates
#pragma warning restore CA1303 // Do not pass literals as localized parameters
