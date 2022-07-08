using System.Management.Automation;
using Core.Storage;
using Data.Challenges;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace ChallengeAssistant.DockerTypes;

public abstract class DockerTest
{
    protected readonly ILogger<DockerTest> Logger;
    protected readonly DockerClient DockerClient = new DockerClientConfiguration().CreateClient();
    protected ProgrammingTest Test = null!;
    protected CreateContainerResponse ContainerResponse = null!;
    protected static string ReportsDir = Path.Join(AppStorage.TemporaryFileStorage, "Reports");
    
    public DockerTest(ILogger<DockerTest> logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Execute a series of tests on <paramref name="userCode"/>
    /// </summary>
    /// <param name="test">Test information</param>
    /// <param name="userCode">Code to test</param>
    /// <returns>JSON string of the report (varies language to language)</returns>
    public async Task<ProgrammingChallengeReport?> Start(ProgrammingTest test, string userCode)
    {
        Test = test;
        return await Run(userCode);
    }

    /// <summary>
    /// Should create the file which get tested within the docker container
    /// </summary>
    /// <param name="userCode"></param>
    /// <returns></returns>
    protected abstract Task<string> CreateUserCodeFile(string userCode);

    protected abstract Task<ProgrammingChallengeReport?> ParseReport();

    protected virtual void ProcessContainerStats(ContainerStatsResponse stats)
    {
        
    }

    protected virtual async Task PrepContainer(string userCodeFilePath)
    {
        await PS.Execute($"docker pull {Test.TestDockerImage}", OnProgress, OnError);
        
        List<string> mounts = new()
        {
            $"{ReportsDir}:/app/reports",
            $"{userCodeFilePath}:{Test.ExecutableFileMountDestination}"
        };

        // We need to download/pull the docker image for our test
        ContainerResponse = await DockerClient.Containers.CreateContainerAsync(new()
        {
            Image = Test.TestDockerImage,
            Entrypoint = Test.DockerEntryPoint.Split(' '),
            AttachStdout = true,
            AttachStderr = true,
            HostConfig = new HostConfig
            {
                // This should mount all the locations that we specified
                Binds = mounts.ToArray() 
            }
        });
    }
    
    /// <summary>
    /// Logs <paramref name="e"/> to LogError stream
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void OnError(object? sender, DataAddingEventArgs e)
    {
        if(e.ItemAdded is null)
            return;
        
        Logger.LogError(e.ItemAdded.ToString());
    }
    
    /// <summary>
    /// Logs <paramref name="e"/> to LogInfo stream
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void OnProgress(object? sender, DataAddingEventArgs e)
    {
        if (e.ItemAdded is null)
            return;

        Logger.LogInformation(e.ItemAdded?.ToString());
    }

    protected virtual async Task<ProgrammingChallengeReport?> Run(string userCode)
    {
        try
        {
            var userCodeFilePath =  await CreateUserCodeFile(userCode);
            await PrepContainer(userCodeFilePath);
            var startTime = DateTime.Now;
            var success = await DockerClient.Containers.StartContainerAsync(ContainerResponse.ID, new());

            if (!success)
            {
                Logger.LogError("Was unable to start container for {Test}", Test.TestDockerImage);
                return null;
            }

            var attachResponse = await DockerClient.Containers.AttachContainerAsync(ContainerResponse.ID, false, new()
            {
                Stderr = true,
                Stdout = true,
                Stream = true
            });

            CancellationTokenSource cts = new();
            await GetContainerStats(cts);
            
            var (stdout, stderr) = await attachResponse.ReadOutputToEndAsync(default);
            var endTime = DateTime.Now;
            Logger.LogWarning("Time took: {Time}", $"{(endTime-startTime):g}");

            if (!string.IsNullOrEmpty(stderr))
                Logger.LogError(stderr);

            if (!string.IsNullOrEmpty(stdout))
                Logger.LogInformation(stdout);
            
            // time to parse the JSON file which should be in the reports directory
            return await ParseReport();
        }
        catch (Exception ex)
        {
            Logger.LogError("Error occurred while processing submission: {Error}", ex);
        }
        finally
        {
            await Cleanup();
        }

        return null;
    }

    protected virtual async Task GetContainerStats(CancellationTokenSource cts)
    {
        try
        {
            await DockerClient.Containers.GetContainerStatsAsync(ContainerResponse.ID, new()
            {
                Stream = true
            }, ContainerProgress(cts), cts.Token);
        }
        catch
        {
            // ignored
        }
    }

    protected Progress<ContainerStatsResponse> ContainerProgress(CancellationTokenSource cts)
        => new(response =>
        {
            if (response.CPUStats.CPUUsage.TotalUsage <= 0)
            {
                cts.Cancel();
            }

            ProcessContainerStats(response);
        });

    /// <summary>
    /// Cleans up the activities that took place during this docker test. Removes
    /// <list type="number">
    /// <item>Container created for test</item>
    /// <item>Image created for test</item>
    /// <item>Repo directory for test</item>
    /// </list>
    /// </summary>
    protected virtual async Task Cleanup()
    {
        if (!string.IsNullOrEmpty(ReportsDir))
            await Util.DeleteDir(ReportsDir);

        Logger.LogInformation("Pruning containers...");
        await DockerClient.Containers.PruneContainersAsync();
    }
}