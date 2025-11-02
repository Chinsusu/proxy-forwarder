using System.Security.Cryptography;
using System.Text;
using ProxyForwarder.Infrastructure.Services;

namespace ProxyForwarder.Infrastructure.Security;

public interface ISecureStorage
{
    Task SaveTokenAsync(string token);
    Task<string?> GetTokenAsync();
}

public sealed class SecureStorage : ISecureStorage
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "token.dat");

    public Task SaveTokenAsync(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, protectedBytes);
        return Task.CompletedTask;
    }
    public Task<string?> GetTokenAsync()
    {
        if (!File.Exists(FilePath)) return Task.FromResult<string?>(null);
        var bytes = File.ReadAllBytes(FilePath);
        var unprot = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
        return Task.FromResult<string?>(Encoding.UTF8.GetString(unprot));
    }
}
