namespace Data.CodeJam;

public class Timezone : IEntityWithTypedId<int>
{
    public int Id { get; set; }

    public string Name { get; set; }
}