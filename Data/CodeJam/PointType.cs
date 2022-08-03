namespace Data.CodeJam;

public class PointType : IEntityWithTypedId<int>
{
    public int Id { get; set; }
    
    /// <summary>
    /// Useful name for referencing point type
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Points to add/subtract to a user
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Is this point type available/visible for everyone? Or reserved
    /// </summary>
    public bool IsPublic { get; set; }
}