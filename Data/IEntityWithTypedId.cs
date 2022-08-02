namespace Data;

public interface IEntityWithTypedId<TPrimaryKey>
{
    TPrimaryKey Id { get; }
}