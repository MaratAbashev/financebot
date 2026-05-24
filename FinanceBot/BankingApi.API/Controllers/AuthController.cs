using BankingApi.API.Requests;
using BankingApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("send-code")]
    public async Task<IActionResult> SendCode(
        [FromBody] SendCodeRequest request,
        CancellationToken ct)
    {
        await authService.SendVerificationCodeAsync(request.PhoneNumber, ct);
        return Ok(new { message = "Код отправлен" });
    }

    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyCode(
        [FromBody] VerifyCodeRequest request,
        CancellationToken ct)
    {
        var result = await authService.VerifyCodeAsync(request.PhoneNumber, request.Code, ct);

        if (!result.Succeeded)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken, ct);

        if (!result.Succeeded)
            return Unauthorized(new { error = result.Error });

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken
        });
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        await authService.RevokeTokenAsync(request.RefreshToken, ct);
        return Ok(new { message = "Токен отозван" });
    }
}