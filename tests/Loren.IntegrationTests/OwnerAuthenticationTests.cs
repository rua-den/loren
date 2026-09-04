using Loren.Web;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class OwnerAuthenticationTests
{
    [Fact]
    public void ConfiguredOwnerPasswordAcceptsExactPassword()
    {
        OwnerPasswordAuthenticator authenticator = new("correct horse battery staple");

        Assert.True(authenticator.IsConfigured);
        Assert.True(authenticator.Verify("correct horse battery staple"));
    }

    [Fact]
    public void ConfiguredOwnerPasswordRejectsDifferentPassword()
    {
        OwnerPasswordAuthenticator authenticator = new("correct horse battery staple");

        Assert.False(authenticator.Verify("wrong password"));
        Assert.False(authenticator.Verify(string.Empty));
        Assert.False(authenticator.Verify(null));
    }

    [Fact]
    public void MissingOwnerPasswordFailsClosed()
    {
        OwnerPasswordAuthenticator authenticator = new(null);

        Assert.False(authenticator.IsConfigured);
        Assert.False(authenticator.Verify("anything"));
    }
}
