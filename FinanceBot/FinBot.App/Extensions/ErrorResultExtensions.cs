using FinBot.Domain.Utils;

namespace FinBot.App.Extensions;

/// <summary>
/// Provides extension methods to convert <see cref="Result"/> and <see cref="Result{T}"/> to appropriate HTTP responses.
/// </summary>
public static class ErrorResultExtensions
{
    /// <summary>
    /// Converts a <see cref="Result{T}"/> with an error to a corresponding <see cref="IResult"/>.
    /// </summary>
    public static IResult ToErrorHttpResult<T>(this Result<T> result)
    {
        return result.ErrorType switch
        {
            ErrorType.NotFound => Results.NotFound(result.ErrorMessage),
            ErrorType.Unauthorized => Results.Unauthorized(),
            ErrorType.Validation => Results.BadRequest(result.ErrorMessage),
            ErrorType.Conflict => Results.Conflict(result.ErrorMessage),
            ErrorType.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
            ErrorType.BadRequest => Results.BadRequest(result.ErrorMessage),
            _ => Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    /// <summary>
    /// Converts a non-generic <see cref="Result"/> with an error to a corresponding <see cref="IResult"/>.
    /// </summary>
    public static IResult ToErrorHttpResult(this Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.NotFound => Results.NotFound(result.ErrorMessage),
            ErrorType.Unauthorized => Results.Unauthorized(),
            ErrorType.Validation => Results.BadRequest(result.ErrorMessage),
            ErrorType.Conflict => Results.Conflict(result.ErrorMessage),
            ErrorType.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
            ErrorType.BadRequest => Results.BadRequest(result.ErrorMessage),
            _ => Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}