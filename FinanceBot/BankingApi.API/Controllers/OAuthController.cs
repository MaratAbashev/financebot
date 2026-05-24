using BankingApi.API.Requests;
using BankingApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.API.Controllers;

[ApiController]
[Route("oauth")]
public class OAuthController(
    IOAuthService oAuthService,
    IAuthService authService) : ControllerBase
{
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string redirectUri,
        [FromQuery] string state,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            return BadRequest(new { error = "redirect_uri обязателен" });

        if (string.IsNullOrWhiteSpace(state))
            return BadRequest(new { error = "state обязателен" });

        var result = await oAuthService.InitiateAsync(redirectUri, state, ct);
        return Ok(result);
    }

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("Неверная ссылка");

        return Content(LoginPageHtml(code), "text/html");
    }

    [HttpPost("login/send-code")]
    public async Task<IActionResult> SendCode(
        [FromBody] OAuthSendCodeRequest request,
        CancellationToken ct)
    {
        await authService.SendVerificationCodeAsync(request.PhoneNumber, ct);
        return Ok(new { message = "Код отправлен" });
    }

    [HttpPost("login/verify")]
    public async Task<IActionResult> Verify(
        [FromBody] OAuthVerifyRequest request,
        CancellationToken ct)
    {
        var user = await authService.GetOrCreateUserAsync(request.PhoneNumber, request.Code, ct);

        if (user is null)
            return BadRequest(new { error = "Неверный или истёкший код" });

        await oAuthService.CompleteAsync(request.AuthCode, user, ct);

        return Content(SuccessPageHtml(), "text/html");
    }

    private static string LoginPageHtml(string authCode)
    {
        var html = System.IO.File.ReadAllText("wwwroot/oauth-login.html");
        return html.Replace("{{AUTH_CODE}}", authCode);
    }

    private static string SuccessPageHtml()
    {
        return System.IO.File.ReadAllText("wwwroot/oauth-success.html");
    }
}