using System.Net;
using System.Net.Http.Json;
using FinBot.Bll.Tests.Infrastructure;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Requests;

namespace FinBot.Bll.Tests.Api;

public class InvitationApiTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static User CreateUser()
    {
        var id = Guid.NewGuid();
        return new User
        {
            Id = id,
            TelegramId = Random.Shared.NextInt64(),
            DisplayName = id.ToString(),
        };
    }

    // TC-API-011 — Получение пользователей, ожидающих добавления (GET /Invitations/Pending)
    [Fact]
    public async Task TC_API_011_GetPendingUsers_Success()
    {
        // arrange
        var groupId = Guid.NewGuid();
        await factory.SeedAsync(db =>
        {
            var user1 = CreateUser();
            var user2 = CreateUser();
            var user3 = CreateUser();

            db.Groups.Add(new Group
            {
                Id = groupId,
                Name = "Group1",
                GroupBalance = 100_000m,
                MonthlyReplenishment = 100_000m,
                SavingStrategy = SavingStrategy.Spread,
                DebtStrategy = DebtStrategy.Nullify
            });

            db.Users.AddRange(user1, user2, user3);

            db.JoinRequests.AddRange(
                new JoinRequest { CreatedAt = DateTime.UtcNow, UserId = user1.Id, GroupId = groupId },
                new JoinRequest { CreatedAt = DateTime.UtcNow, UserId = user2.Id, GroupId = groupId },
                new JoinRequest { CreatedAt = DateTime.UtcNow, UserId = user3.Id, GroupId = groupId }
            );
        });

        // act
        var response = await _client.GetAsync($"/Invitations/Pending?groupId={groupId}");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pending = await response.Content.ReadFromJsonAsync<List<User>>();
        Assert.NotNull(pending);
        Assert.Equal(3, pending.Count);
    }

    // TC-API-017 — Отправка кода приглашения (POST /Invitations/Join) валидный код
    [Fact]
    public async Task TC_API_017_JoinGroupByCode_Success()
    {
        // arrange
        var groupId = Guid.NewGuid();
        var userTgId = Random.Shared.NextInt64();
        await factory.SeedAsync(db =>
        {
            var creator = CreateUser();

            db.Users.AddRange(
                new User { Id = Guid.NewGuid(), TelegramId = userTgId, DisplayName = "User1" },
                creator
            );

            db.Groups.Add(new Group
            {
                Id = groupId,
                Name = "Group1",
                GroupBalance = 100_000m,
                MonthlyReplenishment = 100_000m,
                SavingStrategy = SavingStrategy.Spread,
                DebtStrategy = DebtStrategy.Nullify,
                CreatorId = creator.Id,
            });
        });

        var body = new JoinGroupRequest(UserTgId: userTgId, Code: groupId.ToString());

        // act
        var response = await _client.PostAsJsonAsync("/Invitations/Join", body);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var group = await response.Content.ReadFromJsonAsync<Group>();
        Assert.NotNull(group);
        Assert.Equal(groupId, group.Id);
    }

    // TC-API-018 — Отправка недействительного кода приглашения (POST /Invitations/Join)
    [Fact]
    public async Task TC_API_018_JoinGroupByCode_InvalidCode_BadRequest()
    {
        // arrange
        var userTgId = Random.Shared.NextInt64();
        await factory.SeedAsync(db =>
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                TelegramId = userTgId,
                DisplayName = "User1",
            });
        });

        var body = new JoinGroupRequest(UserTgId: userTgId, Code: "invalid-invite-code");

        // act
        var response = await _client.PostAsJsonAsync("/Invitations/Join", body);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // TC-API-019 — Отправка кода приглашения с несуществующим userTgId (POST /Invitations/Join)
    [Fact]
    public async Task TC_API_019_JoinGroupByCode_NonexistentUser_NotFound()
    {
        // arrange
        var body = new JoinGroupRequest(UserTgId: 999999999, Code: Guid.NewGuid().ToString());

        // act
        var response = await _client.PostAsJsonAsync("/Invitations/Join", body);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}