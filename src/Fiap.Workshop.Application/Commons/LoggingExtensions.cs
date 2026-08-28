using System.Diagnostics.CodeAnalysis;

namespace Fiap.Workshop.Application.Commons;

[ExcludeFromCodeCoverage]
public static class LoggingExtensions
{
    public static string SanitizeForLog(this string value) =>
        value.Replace("\r", string.Empty).Replace("\n", string.Empty);

    public static string MaskEmail(this string email)
    {
        var sanitized = email.SanitizeForLog();
        var atIndex = sanitized.IndexOf('@');

        return atIndex <= 1
            ? "***"
            : $"{sanitized[0]}***{sanitized[atIndex..]}";
    }
}
