// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Edu.App.Properties;
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
#pragma warning disable CA1873 // Avoid potentially expensive logging

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

    await foreach (var @event in eventStore.Subscribe(queryStream, default).WithCancellation(apptLifeTime.ApplicationStopping))
    {
        logger.LogInformation("Subscribed. Recived Event {Event}", @event);
    }

    logger.LogInformation("Subscribed. Finished");
});

var executor = host.Services.GetRequiredService<IMediator>();

long completedCount = 0;

var allCources = new List<(Guid Id, string Name)>(capacity: 5000);
{
    var all = Resources.AllCourses.AsSpan();

    var lineRanges = all.Split('\n');

    foreach (var lineRange in lineRanges)
    {
        var textLine = all[lineRange];

        var idx = textLine.IndexOf(',');

        var guid = Guid.Parse(textLine[..idx]);
        var name = new string(textLine[(idx + 1)..]);

        allCources.Add((guid, name));
    }
}

var sw = Stopwatch.StartNew();

await Task.Run(async () =>
{
    const int PARALLEL_TASKS = 500;

    const int ITERATIONS_COUNT = 1;

    const int CREATE_COURSES_COUNT = 5000;

    int CREATE_COURSES_CHUNK_SIZE = (int)double.Round((double)(CREATE_COURSES_COUNT * ITERATIONS_COUNT) / PARALLEL_TASKS, MidpointRounding.AwayFromZero);

    var tasks = new List<Task>(capacity: allCources.Count * ITERATIONS_COUNT);

    sw.Restart();

    foreach (var _ in Enumerable.Range(0, ITERATIONS_COUNT))
    {
        foreach (var chunk in allCources.Take(CREATE_COURSES_COUNT).Chunk(CREATE_COURSES_CHUNK_SIZE))
        {
            var task = Task.Run(async () =>
            {
                foreach (var course in chunk)
                {
                    var command = new CreateCourse(course.Id, course.Name);

                    try
                    {
                        apptLifeTime.ApplicationStopping.ThrowIfCancellationRequested();

                        var startAt = sw.ElapsedMilliseconds;

                        var res = await executor.ExecuteAsync<CreateCourse, CreateCourseState>(command, apptLifeTime.ApplicationStopping).ConfigureAwait(false);

                        var timeTaken = sw.ElapsedMilliseconds - startAt;

                        Interlocked.Increment(ref completedCount);

                        if (res.IsSuccess)
                        {
                            logger.LogInformation("Ok. {TimeTaken} ms, Command: {Command}; Result: Current chunk name: {Result}", timeTaken, command, res.State.CourseName);
                        }
                        else
                        {
                            logger.LogWarning("OK. {TimeTaken} ms, Business rule validation error. Command: {Command}; Result: {Result}", timeTaken, command, res.Error);
                        }
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        logger.LogError(e.Message);
                    }
                }
            }, apptLifeTime.ApplicationStopping);

            tasks.Add(task);
        }
    }

    try
    {
        await Task.WhenAll(tasks).ConfigureAwait(false);

        sw.Stop();
        logger.LogInformation("CreateCourse WhenAll. Timetaken: {Timetaken} ms. Total tasks: {TaskCount}", sw.ElapsedMilliseconds, completedCount);
    }
    catch (Exception ex)
    {
        sw.Stop();
        logger.LogError(ex, "CreateCourse WhenAll exception. Timetaken: {Timetaken} ms. Total tasks: {TaskCount}", sw.ElapsedMilliseconds, completedCount);
    }
}).ConfigureAwait(false);

await Task.Run(async () =>
{
    if (completedCount > -1)
    {
        Console.WriteLine("No GetActiveCourses ...");
        return;
    }

    try
    {
        var activeCourses = await executor.QueryAsync<GetActiveCourses, ActiveCourses>(new GetActiveCourses(), apptLifeTime.ApplicationStopping).ConfigureAwait(false);

        logger.LogInformation("Active courses {ActiveCourses}", activeCourses?.Courses.Count);
    }
    catch (Exception e)
    {
        logger.LogError(e, "GetActiveCourses error");
    }
}).ConfigureAwait(false);

Console.WriteLine();
Console.WriteLine();
Console.WriteLine("Timetaken: {0} ms. Total tasks completed: {1}", sw.ElapsedMilliseconds, completedCount);
Console.WriteLine();
Console.WriteLine();

var dur = TimeSpan.FromSeconds(4);

Console.WriteLine($"Stop after {dur}");
await Task.Delay(dur).ConfigureAwait(false);

//Console.WriteLine("Press any key to stop...");
//Console.ReadKey();
//Console.WriteLine("Key press detected. Initiating application shutdown...");

//await host.StopAsync().ConfigureAwait(false);

#pragma warning restore CA1873 // Avoid potentially expensive logging
#pragma warning restore CA1848 // Use the LoggerMessage delegates
#pragma warning restore CA1303 // Do not pass literals as localized parameters
