namespace Data.CodeJam;

public class Requirement : IEntityWithTypedId<int>
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string Information { get; set; }
    public string AcceptanceCriteria { get; set; }
}