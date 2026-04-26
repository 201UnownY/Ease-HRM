namespace Ease_HRM.Application.Common.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message, string? entityName = null) : base(message)
    {
        EntityName = entityName;
    }

    public string? EntityName { get; }
}
