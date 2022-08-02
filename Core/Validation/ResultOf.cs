using System.Net;

namespace Core.Validation;

/// <summary>
/// Simple response object which allows the use of <see cref="HttpStatusCode"/> to
/// denote actions taken, or not taken
/// </summary>
/// <param name="Result"></param>
/// <param name="Message"></param>
/// <param name="StatusCode"></param>
/// <typeparam name="T"></typeparam>
public record ResultOf<T>(T? Result, string? Message, HttpStatusCode StatusCode)
{
    public static ResultOf<T> Success(T result) => new (result, null, HttpStatusCode.OK);
    public static ResultOf<T> NotFound(string? message) => new(default, message, HttpStatusCode.NotFound);
    public static ResultOf<T> Error(string? message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) =>
        new(default, message, statusCode);

    public static ResultOf<T> Success(T result, string message) => new(result, message, HttpStatusCode.OK);
}

public record ResultOf(string? Message, HttpStatusCode StatusCode)
{
    public static ResultOf Success() => new(null, HttpStatusCode.OK);
    public static ResultOf Success(HttpStatusCode statusCode) => new(null, statusCode);
    public static ResultOf NotFound() => new(null, HttpStatusCode.NotFound);
    public static ResultOf Error(string? message, HttpStatusCode statusCode) => new(message, statusCode);

    public bool IsSuccess
    {
        get
        {
            var code = (int)StatusCode;
            return code is >= 200 and <= 299;
        }
    }
}
