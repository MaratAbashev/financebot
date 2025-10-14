using FinBot.Domain.Utils;
using Microsoft.AspNetCore.Mvc;

namespace FinBot.WebApi.Extensions;

/// <summary>
/// Provides extension methods to convert <see cref="Result"/> and <see cref="Result{T}"/> to appropriate HTTP responses.
/// </summary>
public static class ErrorResultExtensions
{
    /// <summary>
    /// Converts a <see cref="Result{T}"/> with an error to a corresponding <see cref="IActionResult"/>.
    /// </summary>
    /// <typeparam name="T">The type of the successful result (not used in error conversion).</typeparam>
    /// <param name="result">The failed result object.</param>
    /// <returns>An <see cref="IActionResult"/> with the appropriate status code and error message.</returns>
    public static IActionResult ToErrorHttpResult<T>(this Result<T> result)
    {
        return result.ErrorType switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(result.ErrorMessage),
            ErrorType.Unauthorized => new UnauthorizedResult(),
            ErrorType.Validation => new BadRequestObjectResult(result.ErrorMessage),
            ErrorType.Conflict => new ConflictObjectResult(result.ErrorMessage),
            ErrorType.Forbidden => new StatusCodeResult(StatusCodes.Status403Forbidden),
            ErrorType.BadRequest => new BadRequestObjectResult(result.ErrorMessage),
            _ => new ObjectResult(result.ErrorMessage)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
    }

    /// <summary>
    /// Converts a non-generic <see cref="Result"/> with an error to a corresponding <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="result">The failed result object.</param>
    /// <returns>An <see cref="IActionResult"/> with the appropriate status code and error message.</returns>
    public static IActionResult ToErrorHttpResult(this Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(result.ErrorMessage),
            ErrorType.Unauthorized => new UnauthorizedResult(),
            ErrorType.Validation => new BadRequestObjectResult(result.ErrorMessage),
            ErrorType.Conflict => new ConflictObjectResult(result.ErrorMessage),
            ErrorType.Forbidden => new StatusCodeResult(StatusCodes.Status403Forbidden),
            ErrorType.BadRequest => new BadRequestObjectResult(result.ErrorMessage),
            _ => new ObjectResult(result.ErrorMessage) // TODO: Заменить на "Что-то пошло не так :P"
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
    }
}