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

Console.Clear();
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
    if (logger.IsEnabled(LogLevel.Information))
        logger.LogInformation("Subscribed. Started");

    var queryStream = new DcbQuery([new DcbQueryItem(["CourseRenamed"], [])]);

    await foreach (var @event in eventStore.Subscribe(queryStream, default).WithCancellation(apptLifeTime.ApplicationStopping))
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Subscribed. Recived Event {Event}", @event);
    }

    if (logger.IsEnabled(LogLevel.Information))
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
    const int PARALLEL_TASKS = 100;

    const int ITERATIONS_COUNT = 1;

    const int CREATE_COURSES_COUNT = 5000;

    int CREATE_COURSES_CHUNK_SIZE = (int)double.Round((double)(CREATE_COURSES_COUNT * ITERATIONS_COUNT) / PARALLEL_TASKS, MidpointRounding.AwayFromZero);

    Console.WriteLine();
    Console.WriteLine($"Execute {nameof(CreateCourse)} commands: {{0}}", CREATE_COURSES_COUNT * ITERATIONS_COUNT);
    Console.WriteLine("Parallel tasks: {0}", PARALLEL_TASKS);
    Console.WriteLine(" . - Success added");
    Console.WriteLine(" ! - Business rule error");
    Console.WriteLine(" x - Exception");
    Console.WriteLine();

    var tasks = new List<Task<List<(object command, long timeTaken, object result)>>>(capacity: allCources.Count * ITERATIONS_COUNT);

    sw.Restart();

    foreach (var _ in Enumerable.Range(0, ITERATIONS_COUNT))
    {
        foreach (var chunk in allCources.Take(CREATE_COURSES_COUNT).Chunk(CREATE_COURSES_CHUNK_SIZE))
        {
            var task = Task.Run(async () =>
            {
                var list = new List<(object command, long timeTaken, object result)>(capacity: chunk.Length);

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

                        list.Add((command, timeTaken, res));

                        if (res.IsSuccess)
                        {
                            Console.Write('.');
                        }
                        else
                        {
                            Console.Write('!');
                        }
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        Console.Write('x');
                    }
                }

                return list;

            }, apptLifeTime.ApplicationStopping);

            tasks.Add(task);
        }
    }

    try
    {
        await Task.WhenAll(tasks).ConfigureAwait(false);

        Console.WriteLine();

        sw.Stop();

        if (logger.IsEnabled(LogLevel.Information))
        {
            foreach (var task in tasks)
            {
                if (task.Exception is Exception e)
                {
                    if (logger.IsEnabled(LogLevel.Error))
                    {
                        Console.WriteLine();
                        logger.LogError(e.Message);
                    }

                    continue;
                }

                var list = task.Result;

                foreach (var (command, timeTaken, result) in list)
                {
                    if (result is ExecuteResult<CreateCourseState> res)
                    {
                        if (res.IsSuccess)
                        {
                            if (logger.IsEnabled(LogLevel.Information))
                                logger.LogInformation("Ok. {TimeTaken} ms, Command: {Command}; Result: Current chunk name: {Result}", timeTaken, command, res.State.CourseName);
                        }
                        else
                        {
                            if (logger.IsEnabled(LogLevel.Warning))
                                logger.LogWarning("OK. {TimeTaken} ms, Business rule validation error. Command: {Command}; Result: {Result}", timeTaken, command, res.Error);
                        }
                    }
                }
            }
        }

        Console.WriteLine("CreateCourse WhenAll. Timetaken: {0} ms. Total tasks: {1}", sw.ElapsedMilliseconds, completedCount);
    }
    catch (Exception ex)
    {
        sw.Stop();

        Console.WriteLine();

        if (logger.IsEnabled(LogLevel.Error))
            logger.LogError(ex, "CreateCourse WhenAll exception. Timetaken: {Timetaken} ms. Total tasks: {TaskCount}", sw.ElapsedMilliseconds, completedCount);
    }
}).ConfigureAwait(false);

await Task.Run(async () =>
{
    if (completedCount > -1)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("No GetActiveCourses ...");
        return;
    }

    try
    {
        var activeCourses = await executor.QueryAsync<GetActiveCourses, ActiveCourses>(new GetActiveCourses(), apptLifeTime.ApplicationStopping).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Active courses {ActiveCourses}", activeCourses?.Courses.Count);
    }
    catch (Exception e)
    {
        if (logger.IsEnabled(LogLevel.Error))
            logger.LogError(e, "GetActiveCourses error");
    }
}).ConfigureAwait(false);

Console.WriteLine();
Console.WriteLine();
Console.WriteLine("Timetaken: {0} ms. Total tasks completed: {1}", sw.ElapsedMilliseconds, completedCount);
Console.WriteLine();
Console.WriteLine();

#pragma warning restore CA1873 // Avoid potentially expensive logging
#pragma warning restore CA1848 // Use the LoggerMessage delegates
#pragma warning restore CA1303 // Do not pass literals as localized parameters
