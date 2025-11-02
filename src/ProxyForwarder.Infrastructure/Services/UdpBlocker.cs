using System.Diagnostics;
using System.Security.Principal;
using ProxyForwarder.Core.Abstractions;

namespace ProxyForwarder.Infrastructure.Services;

/// <summary>
/// Add/remove Windows Firewall rules that block UDP for known browsers.
/// Program-specific rules (less side effects) – requires elevation.
/// </summary>
public sealed class UdpBlocker : IUdpBlocker
{
    private static bool IsAdmin()
    {
        using var id = WindowsIdentity.GetCurrent();
        var p = new WindowsPrincipal(id);
        return p.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static string Expand(string s) => Environment.ExpandEnvironmentVariables(s);

    private record Target(string Name, string Path);

    private static IEnumerable<Target> Candidates()
    {
        // Chrome
        yield return new Target("PFW-Block-UDP-Chrome", @"%ProgramFiles%\Google\Chrome\Application\chrome.exe");
        yield return new Target("PFW-Block-UDP-Chrome-x86", @"%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe");
        // Edge
        yield return new Target("PFW-Block-UDP-Edge", @"%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe");
        // Firefox
        yield return new Target("PFW-Block-UDP-Firefox", @"%ProgramFiles%\Mozilla Firefox\firefox.exe");
        yield return new Target("PFW-Block-UDP-Firefox-x86", @"%ProgramFiles(x86)%\Mozilla Firefox\firefox.exe");
    }

    private static ProcessStartInfo PSi(string args) => new()
    {
        FileName = "netsh",
        Arguments = args,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    private static async Task RunAsync(string args, CancellationToken ct)
    {
        using var p = Process.Start(PSi(args))!;
        await p.WaitForExitAsync(ct);
    }

    public async Task ApplyAsync(bool enable, CancellationToken ct = default)
    {
        if (!IsAdmin())
            throw new InvalidOperationException("Blocking UDP requires Administrator. Please run the app as Administrator.");

        foreach (var t in Candidates())
        {
            var path = Expand(t.Path);
            if (!File.Exists(path)) continue;
            var name = t.Name;
            if (enable)
            {
                // Delete existing rule first, then add outbound + inbound
                try
                {
                    await RunAsync($@"advfirewall firewall delete rule name=""{name}"" program=""{path}""", ct);
                }
                catch { }
                
                await RunAsync($@"advfirewall firewall add rule name=""{name}"" dir=out action=block protocol=UDP program=""{path}""", ct);
                await RunAsync($@"advfirewall firewall add rule name=""{name}"" dir=in  action=block protocol=UDP program=""{path}""", ct);
            }
            else
            {
                try
                {
                    await RunAsync($@"advfirewall firewall delete rule name=""{name}"" program=""{path}""", ct);
                }
                catch { }
            }
        }
    }
}
