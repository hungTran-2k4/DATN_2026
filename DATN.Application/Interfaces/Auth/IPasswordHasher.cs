namespace MyProject.Application.Interfaces.Auth;

/// <summary>
/// Interface cho Password Hashing Service
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verify password với hash
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hashedPassword">Hashed password từ database</param>
    /// <returns>True nếu khớp</returns>
    bool VerifyPassword(string password, string hashedPassword);
}
