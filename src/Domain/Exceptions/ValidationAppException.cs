namespace Domain.Exceptions;

/// <summary>
/// Thrown when one or more business/validation rules fail.
/// Named "ValidationAppException" to avoid colliding with FluentValidation.ValidationException.
/// </summary>
public class ValidationAppException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationAppException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationAppException(IDictionary<string, string[]> errors) : this()
    {
        Errors = errors;
    }
}
