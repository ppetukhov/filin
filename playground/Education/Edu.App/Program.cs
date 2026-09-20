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

var oldName = (string?)null;
var courseGuid = Guid.Parse("0F033F3C-32A6-4295-AB7B-B5F93FE56E6D");

long completedCount = 0;

await Task.Run(async () =>
{
    const int Repeate = 2;

    var courseTitles = Resources.CourseTitles.Split("\n");

    var tasks = new List<Task>(capacity: courseTitles.Length * Repeate);

    var sw = Stopwatch.StartNew();

    foreach (var _ in Enumerable.Range(0, Repeate))
    {
        foreach (var course in courseTitles)
        {
            var task = Task.Run(async () =>
            {
                var command = new CreateCourse(Guid.NewGuid(), course);

                var res = await executor.ExecuteAsync<CreateCourse, CreateCourseState>(command, apptLifeTime.ApplicationStopping).ConfigureAwait(false);

                Interlocked.Increment(ref completedCount);

                var timeTaken = sw.ElapsedMilliseconds;

                if (res.IsSuccess)
                {
                    logger.LogInformation("Ok. {TimeTaken} ms, Command: {Command}; Result: Current course name: {Result}", timeTaken, command, res.State.CourseName);
                }
                else
                {
                    logger.LogWarning("OK. {TimeTaken} ms, Business rule validation error. Command: {Command}; Result: {Result}", timeTaken, command, res.Error);
                }
            });

            tasks.Add(task);
        }
    }

    try
    {
        await Task.WhenAll(tasks).ConfigureAwait(false);

        sw.Stop();
        logger.LogCritical("CreateCourse WhenAll. Timetaken: {Timetaken} ms. Total tasks: {TaskCount}", sw.ElapsedMilliseconds, completedCount);
    }
    catch (Exception ex)
    {
        sw.Stop();
        logger.LogError(ex, "CreateCourse WhenAll exception. Timetaken: {Timetaken} ms. Total tasks: {TaskCount}", sw.ElapsedMilliseconds, completedCount);
    }
}).ConfigureAwait(false);

_ = Task.Run(async () =>
{
    if (host is not null)
    {
        return;
    }

    var tasks = new List<Task<ExecuteResult<CreateCourseState>>>();

    var getGetActiveCourses = new GetActiveCourses();

    try
    {
        var activeCourses = await executor.QueryAsync<GetActiveCourses, ActiveCourses>(getGetActiveCourses, apptLifeTime.ApplicationStopping).ConfigureAwait(false);

        logger.LogInformation("Active courses {ActiveCourses}", activeCourses?.Courses.Count);

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
        tasks.Add(executor.ExecuteAsync<CreateCourse, CreateCourseState>(command, apptLifeTime.ApplicationStopping));
    }

    var renameCommands = new List<RenameCourse>();

    foreach (var course in Enumerable.Range(1, 10))
    {
        string CalcCourseName(int num) => $"Course .Net for beginner #{course}";

        var renameCommand = new RenameCourse(courseGuid, CalcCourseName(Random.Shared.Next(1, 11)), oldName ?? "Course ADO and .Net");

        renameCommands.Add(renameCommand);

        tasks.Add(executor.ExecuteAsync<RenameCourse, CreateCourseState>(renameCommand, apptLifeTime.ApplicationStopping));
    }

    try
    {
        await Task.WhenAll(tasks).ConfigureAwait(false);

        sw.Stop();
        logger.LogInformation("RenameCourse WhenAll. Timetaken: {Timetaken} ms.", sw.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        sw.Stop();
        logger.LogError(ex, "RenameCourse WhenAll exception. Timetaken: {Timetaken} ms.", sw.ElapsedMilliseconds);
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

                logger.LogInformation("Course created");
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

            var res = result.IsSuccess
                ? $"{result.State.CourseId} {result.State.CourseName}"
                : result.Error.ToString();

            if (hasOneSuccess && result.IsSuccess)
            {
                logger.LogError("ERROR. Command: {Command}; Result: {Result}", command, res);
            }
            else if (result.IsSuccess)
            {
                hasOneSuccess = true;
                logger.LogInformation("OK. Command: {Command}; Result: {Result}", command, res);
            }
            else
            {
                logger.LogWarning("OK. Business error. Command: {Command}; Result: {Result}", command, res);
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
        logger.LogError("Command: {Command}; Exception: {Ex}", command.NewName, ex.Message);
    }
}, apptLifeTime.ApplicationStopping);

//Console.WriteLine("Press any key to stop...");
//Console.ReadKey();
//Console.WriteLine("Key press detected. Initiating application shutdown...");

//await host.StopAsync().ConfigureAwait(false);

#pragma warning restore CA1873 // Avoid potentially expensive logging
#pragma warning restore CA1848 // Use the LoggerMessage delegates
#pragma warning restore CA1303 // Do not pass literals as localized parameters
