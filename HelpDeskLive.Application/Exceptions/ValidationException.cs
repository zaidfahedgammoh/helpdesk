namespace HelpDeskLive.Application.Exceptions;
public sealed class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.") => Errors = new Dictionary<string, string[]>(errors);
}