namespace CollaborativeWorkspace.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entity, object key) : base($"{entity} with id '{key}' was not found.") { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "You are not authorized to perform this action.") : base(message) { }
}

public class SubscriptionRequiredException : Exception
{
    public SubscriptionRequiredException(string message = "A premium subscription is required to access this resource.") : base(message) { }
}

public class EmailNotVerifiedException : Exception
{
    public EmailNotVerifiedException() : base("Email address must be verified before logging in.") { }
}

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
