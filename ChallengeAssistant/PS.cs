using System.Management.Automation;

namespace ChallengeAssistant;

public static class PS
{
    /// <summary>
    /// Execute powershell script
    /// </summary>
    /// <param name="script"></param>
    /// <param name="onProgress"></param>
    /// <param name="onError"></param>
    public static async Task Execute(string script,
        Action<object?, DataAddingEventArgs>? onProgress = null,
        Action<object?, DataAddingEventArgs>? onError = null)
    {
        using var ps = PowerShell.Create();
        ps.AddScript(script);

        if (onProgress is not null)
            ps.Streams.Progress.DataAdding += (sender, args) => onProgress(sender, args);

        if (onError is not null)
            ps.Streams.Error.DataAdding += (sender, args) => onError(sender, args);

        await ps.InvokeAsync();
    }

    public static async Task SetExecutionPolicy(bool enabled)
    {
        await Execute($"Set-ExecutionPolicy {(enabled ? "Unrestricted" : "AllSigned")} LocalMachine");
    }
}