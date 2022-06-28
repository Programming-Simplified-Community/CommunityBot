namespace CodeJam.Requests;

public class UpdateTopicRegistrationDateRangeRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string TopicTitle { get; set; }
}

public class UpdateTopicSubmissionDateRangeRequest : UpdateTopicRegistrationDateRangeRequest
{
}