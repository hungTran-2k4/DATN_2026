using MyProject.Application.Interfaces.Auth;

namespace MyProject.Infrastructure.Services;

/// <summary>
/// Implementation của IPasswordHasher sử dụng BCrypt
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Hash password sử dụng BCrypt
    /// </summary>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    /// <summary>
    /// Verify password với hash
    /// </summary>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }
}
