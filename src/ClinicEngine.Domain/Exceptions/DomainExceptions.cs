namespace ClinicEngine.Domain.Exceptions;


public sealed class SlotUnavailableException : DomainException
{
    public SlotUnavailableException(string message) : base(message) { }
}

public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string message) : base(message) { }
}


public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message) { }
}


public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with identifier '{key}' was not found.") { }
}


public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
