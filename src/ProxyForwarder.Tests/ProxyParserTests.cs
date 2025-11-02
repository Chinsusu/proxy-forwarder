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

    [Fact]
    public void Parse_HostIdPortUserPass()
    {
        // Format: HOSTNAME:ID:PORT:USER:PASS
        var ok = ProxyParser.TryParse("ipv4-vt-02.resvn.net:95:19609:lxaNzoF1:p5uVv9F6tnlW", out var rec);
        Assert.True(ok);
        Assert.Equal("ipv4-vt-02.resvn.net", rec.Host);
        Assert.Equal(19609, rec.Port);
        Assert.Equal("lxaNzoF1", rec.Username);
        Assert.Equal("p5uVv9F6tnlW", rec.Password);
    }
}
