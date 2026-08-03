namespace Fiap.Workshop.Domain.Errors;

public static class UserErrors
{
    public const string NameEmpty = "Name cannot be empty.";
    public const string NotFound = "User was not found.";
    public const string EmailAlreadyInUse = "The provided email is already in use.";
    public const string PasswordEmpty = "Password cannot be empty.";
    public static readonly string PasswordTooShort = $"Password must be at least {10} characters long.";
    public const string PasswordMissingUppercase = "Password must contain at least one uppercase letter.";
    public const string PasswordMissingLowercase = "Password must contain at least one lowercase letter.";
    public const string PasswordMissingDigit = "Password must contain at least one number.";
    public const string PasswordHashEmpty = "Password hash cannot be empty.";
    public const string InvalidCredentials = "Invalid credentials.";
}
