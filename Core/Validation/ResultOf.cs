using System.Net;

namespace Core.Validation;

public record ResultOf<T>(T? Result, string? Message, HttpStatusCode StatusCode)
{
    public static ResultOf<T> Success(T result) => new (result, null, HttpStatusCode.OK);
    public static ResultOf<T> NotFound(string? message) => new(default, message, HttpStatusCode.NotFound);
    public static ResultOf<T> Error(string? message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) =>
        new(default, message, statusCode);

    public static ResultOf<T> Success(T result, string message) => new(result, message, HttpStatusCode.OK);
}
