using Loren.Core.Projects;

namespace Loren.Core.Actions;

public sealed record AuthenticatedOwnerContext
{
    public AuthenticatedOwnerContext(
        string ownerPrincipalReference,
        ProjectId? projectId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPrincipalReference);

        OwnerPrincipalReference = ownerPrincipalReference.Trim();
        ProjectId = projectId;
    }

    public string OwnerPrincipalReference { get; }

    public ProjectId? ProjectId { get; }
}
