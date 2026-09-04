namespace Loren.Core.Memories;

public readonly record struct MemoryRecordId
{
    public MemoryRecordId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Memory record ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static MemoryRecordId New() => new(Guid.NewGuid());

    public static MemoryRecordId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString("N");
}
