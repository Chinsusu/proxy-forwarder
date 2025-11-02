using ProxyForwarder.Forwarding;
using Xunit;

public class PortAllocatorTests
{
    [Fact]
    public void GetNext_ReturnsPort()
    {
        var pa = new PortAllocator(12000, 12010);
        var p = pa.Next();
        Assert.InRange(p, 12000, 12010);
    }
}
