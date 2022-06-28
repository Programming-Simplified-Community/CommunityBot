using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Core.Validation;

public record ValidatorResponse(bool IsValid, List<string?> Errors);

public static class Validator
{
    public static ValidatorResponse Validate<T>(T? data)
    {
        if (data is null)
            return new(false, new());

        var context = new ValidationContext(data);
        var results = new List<ValidationResult>();

        if (System.ComponentModel.DataAnnotations.Validator.TryValidateObject(data, context, results))
            return new(true, new());

        return new(false, results.Select(x => x.ErrorMessage).ToList());
    }

    public static ResultOf<HttpStatusCode> ExitWith<T>(this ValidatorResponse validationResult, ILogger<T> logger,
        [CallerMemberName] string memberName = "")
    {
        var errors = string.Join(", ", validationResult.Errors);
        logger.LogWarning("Invalid data was provided to {Member}: {Errors}", memberName, errors);
        
        return ResultOf<HttpStatusCode>.Error(errors);
    }
}