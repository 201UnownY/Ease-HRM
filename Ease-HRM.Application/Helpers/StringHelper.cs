namespace Ease_HRM.Application.Helpers;

public static class StringHelper
{
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be null or whitespace.");
        }

        return input.Trim().ToLowerInvariant();
    }

    public static string Normalize(string? input, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException($"{fieldName} is required.");
        }

        return input.Trim().ToLowerInvariant();
    }
}
