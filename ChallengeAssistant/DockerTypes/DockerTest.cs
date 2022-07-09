using System.Management.Automation;
using Core.Storage;
using Data.Challenges;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChallengeAssistant.DockerTypes;

public abstract class DockerTest
{
    protected readonly ILogger<DockerTest> Logger;
    protected readonly DockerClient DockerClient = new DockerClientConfiguration().CreateClient();
    protected ProgrammingTest Test = null!;
    protected CreateContainerResponse ContainerResponse = null!;
    protected readonly IConfiguration Configuration;
    private string _imageId;
    private DateTime _containerStartTime;

    public DockerTest(ILogger<DockerTest> logger, IConfiguration config)
    {
        Logger = logger;
        Configuration = config;
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
        _imageId = Guid.NewGuid().ToString().ToLower().Replace("-",string.Empty);
        return await Run(userCode);
    }

    /// <summary>
    /// Should create the file which get tested within the docker container
    /// </summary>
    /// <param name="userCode"></param>
    /// <returns></returns>
    protected abstract Task<string> CreateUserCodeFile(string userCode);

    /// <summary>
    /// Parse the report generated from the Automated testing package
    /// </summary>
    /// <returns><c>null</c> when unable to parse. Otherwise, a populated report</returns>
    protected abstract Task<ProgrammingChallengeReport?> ParseReport();

    /// <summary>
    /// Do something with the container stats. Can be used to measure performance if desired
    /// </summary>
    /// <param name="stats"></param>
    protected virtual void ProcessContainerStats(ContainerStatsResponse stats)
    {
        
    }

    /// <summary>
    /// Create the dockerfile that will be used to test the user's code
    /// </summary>
    /// <param name="userCodeFilePath"></param>
    /// <returns><see cref="string"/> that shall be used in the dockerfile</returns>
    protected abstract Task<string> CreateDockerFile(string userCodeFilePath);

    /// <summary>
    /// Prepares the container.
    ///
    /// <list type="number">
    /// <item>Creates dockerfile for <see cref="Test"/>, inserting <see cref="userCodeFilePath"/></item>
    /// <item>
    ///     <para>Creates the docker container based on <see cref="Test"/></para>
    ///     <para>Sets the container network to none,</para>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="userCodeFilePath"></param>
    protected virtual async Task PrepContainer(string userCodeFilePath)
    {
        // Ensure we have the most update to date version of the image
        await PS.Execute($"docker pull {Test.TestDockerImage}", OnProgress, OnError);
        FileInfo info = new(userCodeFilePath);
        
        // Mounting a singular file is hard, so we need to create a version of the image
        // Copying our 'file' into there
        var dockerFileId = Guid.NewGuid().ToString().Replace("-", string.Empty);
        var dockerPath = Path.Join(AppStorage.ScriptsPath, dockerFileId);
        var dockerfile = await CreateDockerFile(info.Name);
        
        try
        {
            Util.EnsureDir(AppStorage.ScriptsPath);
            Util.EnsureDir(AppStorage.ReportsPath);
            
            await File.WriteAllTextAsync(dockerPath, dockerfile);

            if(!File.Exists(dockerPath))
                Logger.LogWarning("Dockerfile: {Path} - does not exist", dockerfile);

            if (!File.Exists(userCodeFilePath))
                Logger.LogWarning("User Code: {Path} - does not exist", userCodeFilePath);
            
            await PS.Execute($"cd \"{AppStorage.ScriptsPath}\" && docker build -f \"{dockerPath}\" -t \"{_imageId}\" .",
                OnProgress, OnProgress);

            Logger.LogInformation("Cleaning up docker file: {DockerPath}", dockerPath);
            File.Delete(dockerPath);
        }
        finally
        {
            if (File.Exists(dockerPath))
                File.Delete(dockerPath);
        }

        var reportPath = Configuration["ReportPath"] is not null
            ? Configuration["ReportPath"]
            : AppStorage.ReportsPath;
        
        
        List<string> mounts = new()
        {
            $"{reportPath}:/app/Data/Reports:rw",
        };
        
        Logger.LogWarning("Mounting dirs:\n\t{Dirs}", string.Join("\n\t",mounts));

        // We need to download/pull the docker image for our test
        ContainerResponse = await DockerClient.Containers.CreateContainerAsync(new()
        {
            Image = _imageId,
            Entrypoint = Test.DockerEntryPoint.Split(' '),
            AttachStdout = true,
            AttachStderr = true,
            HostConfig = new HostConfig
            {
                // This should mount all the locations that we specified
                Binds = mounts.ToArray()
            },
            // We want to ensure there is no funny business going on with user provided code
            // therefore we're restricting it by having no internet access
            NetworkDisabled = true
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

    /// <summary>
    /// Run the main loop for testing user-provided code
    /// </summary>
    /// <param name="userCode"></param>
    /// <returns><c>null</c> if no report, otherwise a report from testing</returns>
    protected virtual async Task<ProgrammingChallengeReport?> Run(string userCode)
    {
        try
        {
            var userCodeFilePath =  await CreateUserCodeFile(userCode);
            await PrepContainer(userCodeFilePath);
            var startTime = DateTime.UtcNow;
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

            CancellationTokenSource cts = new(); // This token is for stopping container
            await GetContainerStats(cts);
            
            // Display the logs from the container
            var (stdout, stderr) = await attachResponse.ReadOutputToEndAsync(default);
            var endTime = DateTime.UtcNow;
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

    /// <summary>
    /// Allows a developer to tap into container statistics if desired.
    /// </summary>
    /// <param name="cts">Cancellation token source that shall be used for stopping the container</param>
    private async Task GetContainerStats(CancellationTokenSource cts)
    {
        _containerStartTime = DateTime.UtcNow;

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
            // When the usage drops to 0 that typically means the container has completed executing...
            if (response.CPUStats.CPUUsage.TotalUsage <= 0)
                cts.Cancel();
            
            // If the user's code elapses our timeout we'll shut things down
            if (Test.TimeoutInMinutes.HasValue && (DateTime.UtcNow-_containerStartTime).TotalMinutes >= Test.TimeoutInMinutes.Value)
                cts.Cancel();
            
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
        if (!string.IsNullOrEmpty(AppStorage.ReportsPath))
            await Util.DeleteDir(AppStorage.ReportsPath);

        Logger.LogInformation("Pruning containers...");
        await DockerClient.Containers.PruneContainersAsync();
    }
}