public sealed record Todo(string Content)
{
    public Guid Id { get; } = Guid.NewGuid();
}
