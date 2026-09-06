namespace Loren.Core.Organization;

public readonly record struct OrganizationItemId
{
    public OrganizationItemId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Organization item ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static OrganizationItemId New() => new(Guid.NewGuid());

    public static OrganizationItemId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString("N");
}
