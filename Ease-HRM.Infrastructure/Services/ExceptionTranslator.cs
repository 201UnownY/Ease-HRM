using Ease_HRM.Application.Common.Interfaces;

namespace Ease_HRM.Infrastructure.Services;

public class ExceptionTranslator : IExceptionTranslator
{
    public bool IsUniqueConstraintViolation(Exception ex)
    {
        var message = GetDeepestMessage(ex).ToLowerInvariant();

        return message.Contains("unique")
            || message.Contains("duplicate")
            || message.Contains("cannot insert duplicate")
            || message.Contains("ix_")
            || message.Contains("uq_");
    }

    private static string GetDeepestMessage(Exception ex)
    {
        while (ex.InnerException != null)
        {
            ex = ex.InnerException;
        }

        return ex.Message;
    }
    public bool IsConcurrencyConflict(Exception ex)
    {
        if (ex.GetType().Name.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            return true;

        return ex.InnerException?.GetType().Name.Contains("Concurrency", StringComparison.OrdinalIgnoreCase) == true;
    }
}
