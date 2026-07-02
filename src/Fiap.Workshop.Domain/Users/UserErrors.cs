namespace Fiap.Workshop.Domain.Users;

public static class UserErrors
{
    public const string NameEmpty = "Name cannot be empty.";
    public const string EmailEmpty = "Email cannot be empty.";
    public static readonly string EmailTooLong = $"Email cannot exceed {Email.MaxLength} characters.";
    public const string EmailInvalidFormat = "Email format is invalid.";
    public const string NotFound = "User was not found.";
    public const string EmailAlreadyInUse = "The provided email is already in use.";
}
