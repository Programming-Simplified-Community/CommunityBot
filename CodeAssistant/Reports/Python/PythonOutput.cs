namespace CodeAssistant.Reports.Python;

internal class PythonOutput : IOutput
{
    public PythonSummary Summary { get; set; }
    public PythonMessage[] Messages { get; set; }
}