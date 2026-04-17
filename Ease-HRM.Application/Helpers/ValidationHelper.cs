namespace Ease_HRM.Application.Helpers;

public static class ValidationHelper
{
    public static Guid RequireGuid(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{fieldName} is required.");
        }

        return value;
    }

    public static string RequireString(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    public static void EnsureNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
        {
            throw new ArgumentException($"{fieldName} cannot be negative.");
        }
    }

    public static void EnsurePositive(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"{fieldName} must be greater than 0.");
        }
    }

    public static void RequireValidYear(int year)
    {
        var currentYear = DateTime.UtcNow.Year;

        if (year < 2000 || year > currentYear)
        {
            throw new ArgumentException("Invalid year.");
        }
    }

    public static void RequireValidMonth(int month)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentException("Month must be between 1 and 12.");
        }
    }

    public static string NormalizeEmail(string? value)
    {
        var valid = RequireString(value, "Email");
        return valid.ToLowerInvariant();
    }
}
