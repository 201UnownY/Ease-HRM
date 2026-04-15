namespace Ease_HRM.Api.Models;

public static class ApiResponseHelper
{
    public static ApiResponse<T> Success<T>(T data, string message)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }
}