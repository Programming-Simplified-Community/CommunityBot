namespace CodeAssistant.Reports.Python;

internal class PythonSummary
{
    public DateTime Started { get; set; }
    public string[] Libraries { get; set; }
    public string[] Strictness { get; set; }
    public string Profiles { get; set; }
    public string[] Tools { get; set; }
    public int MessageCount { get; set; }
    public DateTime Completed { get; set; }
    public string TimeTaken { get; set; }
    public string Formatter { get; set; }
}