namespace Ease_HRM.Application.Common.Interfaces;

public interface IExceptionTranslator
{
    bool IsUniqueConstraintViolation(Exception ex);
    bool IsConcurrencyConflict(Exception ex);
}
