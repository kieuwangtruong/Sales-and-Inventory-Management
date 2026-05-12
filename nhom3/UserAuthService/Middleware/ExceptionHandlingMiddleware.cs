using System.Net;
using System.Text.Json;
using UserAuthService.DTOs;

namespace UserAuthService.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;

        var body = JsonSerializer.Serialize(
            new ApiResponse(false, "Lỗi hệ thống. Vui lòng thử lại sau"),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return context.Response.WriteAsync(body);
    }
}
