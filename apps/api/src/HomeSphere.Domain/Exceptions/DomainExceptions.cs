namespace HomeSphere.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key ({key}) was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message)
    {
    }
}

public class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string message) : base(message)
    {
    }
}

public class ForbiddenAccessException : DomainException
{
    public ForbiddenAccessException(string message = "You do not have permission to access this resource.")
        : base(message)
    {
    }
}

