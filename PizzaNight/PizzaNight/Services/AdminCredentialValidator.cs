using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PizzaNight.Configuration;

namespace PizzaNight.Services;

public sealed class AdminCredentialValidator(IOptions<AdminOptions> options)
{
    private readonly AdminOptions credentials = options.Value;

    public bool IsValid(string username, string password)
    {
        if (!string.Equals(username.Trim(), credentials.Username, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var suppliedPassword = Encoding.UTF8.GetBytes(password);
        var configuredPassword = Encoding.UTF8.GetBytes(credentials.Password);
        return suppliedPassword.Length == configuredPassword.Length
            && CryptographicOperations.FixedTimeEquals(suppliedPassword, configuredPassword);
    }
}
