namespace VinhKhanhAudioGuide.Web.Services;

using BCrypt.Net;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(password))
            return false;

        try
        {
            return BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            // Fallback for transition period if password is not yet hashed
            return password == hashedPassword;
        }
    }
}
