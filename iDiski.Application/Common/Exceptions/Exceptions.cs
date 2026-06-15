namespace iDiski.Application.Common.Exceptions;

/// <summary>
/// Thrown when an entity lookup by ID yields no result.
/// The global exception handler maps this to HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found.") { }
}

/// <summary>
/// Thrown by the ValidationBehaviour pipeline when FluentValidation finds errors.
/// The global exception handler maps this to HTTP 422 with the error dictionary.
/// </summary>
public sealed class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : base("One or more validation failures occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}

/// <summary>
/// Thrown when authentication fails (invalid credentials, inactive user, etc.)
/// The global exception handler maps this to HTTP 401 Unauthorized.
/// </summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a user lacks permission for a resource or operation.
/// The global exception handler maps this to HTTP 403 Forbidden.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
