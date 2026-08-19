using System.Diagnostics.CodeAnalysis;

namespace Fiap.Workshop.Application.Commons;

[ExcludeFromCodeCoverage]
public class Output
{
    private readonly List<string> _messages = [];
    private readonly List<string> _errorMessages = [];

    public IReadOnlyCollection<string> ErrorMessages => _errorMessages.AsReadOnly();
    public bool IsValid { get; private set; } = true;
    public IReadOnlyCollection<string> Messages => _messages.AsReadOnly();
    public object? Result { get; private set; }

    public void AddErrorMessage(string message)
    {
        _errorMessages.Add(message);
        IsValid = false;
    }

    public void AddErrorMessages(IEnumerable<string> messages)
    {
        _errorMessages.AddRange(messages);
        IsValid = false;
    }

    public void AddMessage(string message) => _messages.Add(message);

    public void AddResult(object result)
    {
        Result = result;
        IsValid = true;
    }

    public T? GetResult<T>() => (T?)Result;
}
