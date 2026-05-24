using System.Net;
using System.Net.Http.Json;
using FinBot.Bll.Tests.Infrastructure;
using FinBot.Domain.Models;
using FinBot.Domain.Requests;

namespace FinBot.Bll.Tests.Api;

public class UserApiTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // TC-API-012 — Добавление пользователя (POST /Users/)
    [Fact]
    public async Task TC_API_012_CreateUser_Success()
    {
        // arrange
        var body = new CreateUserRequest(TgId: 100, DisplayName: "Test");

        // act
        var response = await _client.PostAsJsonAsync("/Users/", body);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(user);
        Assert.Equal(100, user.TelegramId);
        Assert.Equal("Test", user.DisplayName);
    }

    // TC-API-013 — Добавление уже существующего пользователя (POST /Users/)
    [Fact]
    public async Task TC_API_013_CreateUser_AlreadyExists_Conflict()
    {
        // arrange
        var userTgId = Random.Shared.NextInt64();
        await factory.SeedAsync(db =>
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                TelegramId = userTgId,
                DisplayName = "Test",
            });
        });

        var body = new CreateUserRequest(TgId: userTgId, DisplayName: "Test");

        // act
        var response = await _client.PostAsJsonAsync("/Users/", body);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}