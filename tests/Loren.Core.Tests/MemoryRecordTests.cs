using Loren.Core.Memories;
using Loren.Core.Projects;
using Xunit;

namespace Loren.Core.Tests;

public sealed class MemoryRecordTests
{
    [Fact]
    public void MemoryRecordIdUsesCanonicalLowercaseGuidText()
    {
        MemoryRecordId id = new(Guid.Parse("A987FBC9-4BED-3078-CF07-9141BA07C9F3"));

        Assert.Equal("a987fbc94bed3078cf079141ba07c9f3", id.ToString());
    }

    [Fact]
    public void RepositoryScopeRequiresProjectScope()
    {
        DateTimeOffset now = new(2026, 9, 4, 9, 50, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => new MemoryRecord(
            MemoryRecordId.New(),
            MemorySourceClass.OwnerExplicit,
            "Wedding repository belongs to the wedding project.",
            null,
            RepositoryId.New(),
            "owner:authenticated",
            null,
            now,
            now));
    }

    [Fact]
    public void RecordCannotSupersedeItself()
    {
        MemoryRecordId id = MemoryRecordId.New();
        DateTimeOffset now = new(2026, 9, 4, 9, 50, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => new MemoryRecord(
            id,
            MemorySourceClass.OwnerCorrection,
            "Corrected owner memory.",
            ProjectId.New(),
            null,
            "owner:authenticated",
            id,
            now,
            now));
    }

    [Fact]
    public void UpdatedAtCannotPrecedeCreatedAt()
    {
        DateTimeOffset createdAt = new(2026, 9, 4, 9, 50, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => new MemoryRecord(
            MemoryRecordId.New(),
            MemorySourceClass.OwnerExplicit,
            "Durable owner memory.",
            ProjectId.New(),
            null,
            "owner:authenticated",
            null,
            createdAt,
            createdAt.AddSeconds(-1)));
    }
}
