// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Edu.App.Properties;
using Edu.ReadModels;
using Edu.UseCases;

using Filin;
using Filin.DependencyInjection;
using Filin.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1303 // Do not pass literals as localized parameters
#pragma warning disable CA1848 // Use the LoggerMessage delegates

Console.Clear();
Console.WriteLine("Edu App");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) => services
        .AddFilin()
        .AddTypeManifestAssembly(typeof(CreateCourse).Assembly, typeof(ReadModelsExtensions).Assembly)
        .AddEventStoreAdoNet()
        .AddProjectionStoreAdoNet())
    .Build();

host.Start();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

var apptLifeTime = host.Services.GetRequiredService<IHostApplicationLifetime>();

var eventStore = host.Services.GetRequiredService<IEventStore>();

_ = Task.Run(async () =>
{
    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation("Subscribed. Started");
    }

    var queryStream = new DcbQuery([new DcbQueryItem(["CourseRenamed"], [])]);

    await foreach (var @event in eventStore.Subscribe(queryStream, default).WithCancellation(apptLifeTime.ApplicationStopping))
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Subscribed. Recived Event {Event}", @event);
        }
    }

    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation("Subscribed. Finished");
    }
});

var executor = host.Services.GetRequiredService<IMediator>();

long completedCount = 0;

await Task.Run(async () =>
{
    const int parallelTasks = 100;

    const int iterationsCount = 1;

    const int createCourcesCount = 5000;

    var createCoursecCountSize = (int)double.Round((double)(createCourcesCount * iterationsCount) / parallelTasks, MidpointRounding.AwayFromZero);

    Console.WriteLine();
    Console.WriteLine($"Execute {nameof(CreateCourse)} commands: {{0}}", createCourcesCount * iterationsCount);
    Console.WriteLine("Parallel tasks: {0}", parallelTasks);
    Console.WriteLine(" . - Success added");
    Console.WriteLine(" ! - Business rule error");
    Console.WriteLine(" x - Exception");
    Console.WriteLine();

    var allCources = new List<(Guid Id, string Name)>(capacity: 5000);
    {
        var all = Resources.AllCourses.AsSpan();

        var lineRanges = all.Split('\n');

        foreach (var lineRange in lineRanges)
        {
            var textLine = all[lineRange];

            var idx = textLine.IndexOf(',');

            if (idx >= 0)
            {
                var guid = Guid.Parse(textLine[..idx]);

                var span = textLine[(idx + 1)..];

                if (char.IsControl(span[^1]))
                {
                    span = span[..^1];
                }

                var name = new string(span);

                allCources.Add((guid, name));
            }
        }
    }

    var tasks = new List<Task<List<(object command, long timeTaken, object result)>>>(capacity: allCources.Count * iterationsCount);

    var sw = Stopwatch.StartNew();

    foreach (var __ in Enumerable.Range(0, iterationsCount))
    {
        foreach (var chunk in allCources.Take(createCourcesCount).Chunk(createCoursecCountSize))
        {
            var task = Task.Run(async () =>
            {
                var list = new List<(object command, long timeTaken, object result)>(capacity: chunk.Length);

                foreach (var (id, name) in chunk)
                {
                    var command = new CreateCourse(id, name);

                    try
                    {
                        apptLifeTime.ApplicationStopping.ThrowIfCancellationRequested();

                        var startAt = sw.ElapsedMilliseconds;

                        var res = await executor.ExecuteAsync<CreateCourse, CreateCourseState>(command, apptLifeTime.ApplicationStopping).ConfigureAwait(false);

                        var timeTaken = sw.ElapsedMilliseconds - startAt;

                        _ = Interlocked.Increment(ref completedCount);

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

#pragma warning disable CA1031 // Do not catch general exception types
    try
    {
        _ = await Task.WhenAll(tasks).ConfigureAwait(false);

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
#pragma warning disable CA2254 // Template should be a static expression
                        logger.LogError(e.Message);
#pragma warning restore CA2254 // Template should be a static expression
                    }

                    continue;
                }

#pragma warning disable CA1849 // Call async methods when in an async method
                var list = task.Result;
#pragma warning restore CA1849 // Call async methods when in an async method

                foreach (var (command, timeTaken, result) in list)
                {
                    if (result is ExecuteResult<CreateCourseState> res)
                    {
                        if (res.IsSuccess)
                        {
                            if (logger.IsEnabled(LogLevel.Information))
                            {
                                logger.LogInformation("Ok. {TimeTaken} ms, Command: {Command}; Result: Current course name: {Result}", timeTaken, command, res.State.CourseName);
                            }
                        }
                        else
                        {
                            if (logger.IsEnabled(LogLevel.Warning))
                            {
                                logger.LogWarning("OK. {TimeTaken} ms, Business rule validation error. Command: {Command}; Result: {Result}", timeTaken, command, res.Error);
                            }
                        }
                    }
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("CreateCourse WhenAll. Timetaken: {0} ms. Total tasks: {1}", sw.ElapsedMilliseconds, completedCount);
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        sw.Stop();

        Console.WriteLine();

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(ex, "CreateCourse WhenAll exception. Timetaken: {Timetaken} ms. Total tasks: {TaskCount}", sw.ElapsedMilliseconds, completedCount);
        }
    }
#pragma warning restore CA1031 // Do not catch general exception types

    Console.WriteLine();
    Console.WriteLine("Timetaken: {0} ms. Total tasks completed: {1}", sw.ElapsedMilliseconds, completedCount);
    Console.WriteLine();
}).ConfigureAwait(false);

await Task.Run(async () =>
{
    var sw = Stopwatch.StartNew();
#pragma warning disable CA1031 // Do not catch general exception types
    try
    {
        var activeCourses = await executor.QueryAsync<GetActiveCourses, ActiveCourses>(new GetActiveCourses(), apptLifeTime.ApplicationStopping).ConfigureAwait(false);

        sw.Stop();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Get all active courses {ActiveCourses}. Timetaken: {Timetaken} ms", activeCourses?.Courses.Count, sw.ElapsedMilliseconds);
        }
    }
    catch (Exception e)
    {
        sw.Stop();

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(e, "GetActiveCourses error. Timetaken: {Timetaken} ms", sw.ElapsedMilliseconds);
        }
    }
#pragma warning restore CA1031 // Do not catch general exception types
}).ConfigureAwait(false);

if (!apptLifeTime.ApplicationStopping.IsCancellationRequested)
{
    Console.WriteLine();
    Console.WriteLine("Stopped after 5s");
    Console.WriteLine();

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(5), apptLifeTime.ApplicationStopping).ConfigureAwait(false);
    }
    catch (OperationCanceledException) { }
}

#pragma warning restore CA1848 // Use the LoggerMessage delegates
#pragma warning restore CA1303 // Do not pass literals as localized parameters
