using ProxyForwarder.Core.Common;
using Xunit;

public class ProxyParserTests
{
    [Fact]
    public void Parse_HostPortUserPass()
    {
        var ok = ProxyParser.TryParse("1.2.3.4:8080:user:pass", out var rec);
        Assert.True(ok);
        Assert.Equal("1.2.3.4", rec.Host);
        Assert.Equal(8080, rec.Port);
        Assert.Equal("user", rec.Username);
        Assert.Equal("pass", rec.Password);
    }
}
