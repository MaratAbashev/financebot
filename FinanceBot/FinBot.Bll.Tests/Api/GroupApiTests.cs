using System.Net;
using System.Net.Http.Json;
using FinBot.Bll.Tests.Infrastructure;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Requests;

namespace FinBot.Bll.Tests.Api;

public class GroupApiTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
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

    // TC-API-001 — Создание группы (POST /Groups/New)
    [Fact]
    public async Task TC_API_001_CreateGroup_Success()
    {
        // arrange
        var userId = Guid.NewGuid();
        await factory.SeedAsync(db =>
        {
            db.Users.Add(new User
            {
                Id = userId,
                TelegramId = 1,
                DisplayName = "Test1",
            });
        });

        var body = new CreateGroupRequest(
            GroupName: "Group",
            Replenishment: 10_000m,
            GroupSavingStrategy: SavingStrategy.Save,
            AccountSavingStrategy: SavingStrategy.Save,
            DebtStrategy: DebtStrategy.Nullify,
            SavingTargetName: "Отпуск",
            SavingTargetAmount: 300_000m);

        // act
        var response = await _client.PostAsJsonAsync("/Groups/New?userTgId=1", body);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Group>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Group", created.Name);
        Assert.Equal(10_000m, created.MonthlyReplenishment);
        Assert.Equal(SavingStrategy.Save, created.SavingStrategy);

        Assert.NotNull(created.Accounts);
        Assert.NotEmpty(created.Accounts);
        var account = created.Accounts.First();
        Assert.Equal(userId, account.UserId);
        Assert.NotEqual(0m, account.Balance);
        Assert.Equal(SavingStrategy.Save, account.SavingStrategy);

        Assert.NotNull(created.Saving);
        var saving = created.Saving;
        Assert.Equal("Отпуск", saving.Name);
        Assert.Equal(300_000m, saving.TargetAmount);
        Assert.Equal(0m, saving.CurrentAmount);
    }

    // TC-API-002 — Создание группы без обязательных полей (POST /Groups/New)
    [Fact]
    public async Task TC_API_002_CreateGroup_MissingFields_BadRequest()
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

        var body = new
        {
            GroupName = "",
            GroupSavingStrategy = (int)SavingStrategy.Save,
            AccountSavingStrategy = (int)SavingStrategy.Save,
            DebtStrategy = (int)DebtStrategy.Nullify
        };

        // act
        var response = await _client.PostAsJsonAsync($"/Groups/New?userTgId={userTgId}", body);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // TC-API-003 — Получение всех групп пользователя (GET /Groups/Users?userTgId)
    [Fact]
    public async Task TC_API_003_GetUserGroups_Success()
    {
        // arrange
        var userTgId = Random.Shared.NextInt64();
        await factory.SeedAsync(db =>
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                TelegramId = userTgId,
                DisplayName = "Test3",
            };

            var group1 = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Group1",
                GroupBalance = 100_000m,
                MonthlyReplenishment = 100_000m,
                SavingStrategy = SavingStrategy.Spread,
                DebtStrategy = DebtStrategy.Nullify,
                CreatorId = user.Id
            };

            var saving = new Saving
            {
                Id = Guid.NewGuid(),
                Name = "Отпуск",
                TargetAmount = 1_000_000m,
                CurrentAmount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                GroupId = group1.Id
            };

            var account1 = new Account
            {
                Id = Random.Shared.Next(),
                Role = Role.Admin,
                DailyAllocation = 10_000m,
                MonthlyAllocation = 100_000m,
                SavingStrategy = SavingStrategy.Save,
                Balance = 0,
                UserId = user.Id,
                GroupId = group1.Id,
            };

            var group2 = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Group2",
                GroupBalance = 2_000m,
                MonthlyReplenishment = 2_000m,
                SavingStrategy = SavingStrategy.SaveForNextPeriod,
                DebtStrategy = DebtStrategy.Nullify,
                CreatorId = user.Id,
            };

            var account2 = new Account
            {
                Id = Random.Shared.Next(),
                Role = Role.Member,
                DailyAllocation = 100m,
                MonthlyAllocation = 2_000m,
                SavingStrategy = SavingStrategy.Spread,
                Balance = 1_000m,
                UserId = user.Id,
                GroupId = group2.Id,
            };

            db.Users.Add(user);
            db.Groups.Add(group1);
            db.Groups.Add(group2);
            db.Savings.Add(saving);
            db.Accounts.Add(account1);
            db.Accounts.Add(account2);
        });

        // act
        var response = await _client.GetAsync($"/Groups/Users?userTgId={userTgId}&adminOnly=false");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var groups = await response.Content.ReadFromJsonAsync<List<Group>>();
        Assert.NotNull(groups);
        Assert.Equal(2, groups.Count);
    }

    // TC-API-004 — Получение групп несуществующего пользователя (GET /Groups/Users?userTgId)
    [Fact]
    public async Task TC_API_004_GetUserGroups_NonexistentUser_NotFound()
    {
        // act
        var response = await _client.GetAsync("/Groups/Users?userTgId=999999999&adminOnly=false");

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // TC-API-005 — Получение группы с пользователями и копилкой (GET /Groups/{groupId})
    [Fact]
    public async Task TC_API_005_GetGroupById_Success()
    {
        // arrange
        var groupId = Guid.NewGuid();
        await factory.SeedAsync(db =>
        {
            var user1 = CreateUser();
            var user2 = CreateUser();
            var user3 = CreateUser();

            var group = new Group
            {
                Id = groupId,
                Name = "Group1",
                GroupBalance = 300_000m,
                MonthlyReplenishment = 300_000m,
                SavingStrategy = SavingStrategy.Spread,
                DebtStrategy = DebtStrategy.Nullify,
                CreatorId = user1.Id
            };

            var saving = new Saving
            {
                Id = Guid.NewGuid(),
                Name = "Отпуск",
                TargetAmount = 1_000_000m,
                CurrentAmount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                GroupId = group.Id
            };

            var account1 = new Account
            {
                Id = Random.Shared.Next(),
                Role = Role.Admin,
                DailyAllocation = 10_000m,
                MonthlyAllocation = 100_000m,
                SavingStrategy = SavingStrategy.Save,
                Balance = 0,
                UserId = user1.Id,
                GroupId = group.Id,
            };

            var account2 = new Account
            {
                Id = Random.Shared.Next(),
                Role = Role.Member,
                DailyAllocation = 10_000m,
                MonthlyAllocation = 100_000m,
                SavingStrategy = SavingStrategy.Save,
                Balance = 0,
                UserId = user2.Id,
                GroupId = group.Id,
            };

            var account3 = new Account
            {
                Id = Random.Shared.Next(),
                Role = Role.Member,
                DailyAllocation = 10_000m,
                MonthlyAllocation = 100_000m,
                SavingStrategy = SavingStrategy.Save,
                Balance = 0,
                UserId = user3.Id,
                GroupId = group.Id,
            };

            db.Users.AddRange(user1, user2, user3);
            db.Groups.Add(group);
            db.Savings.Add(saving);
            db.Accounts.AddRange(account1, account2, account3);
        });

        // act
        var response = await _client.GetAsync($"/Groups/{groupId}");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var group = await response.Content.ReadFromJsonAsync<Group>();
        Assert.NotNull(group);
        Assert.Equal(groupId, group.Id);
        Assert.NotNull(group.Accounts);
        Assert.Equal(3, group.Accounts.Count);
        Assert.NotNull(group.Saving);
    }

    // TC-API-006 — Получение несуществующей группы (GET /Groups/{groupId})
    [Fact]
    public async Task TC_API_006_GetGroupById_NotFound()
    {
        // act
        var response = await _client.GetAsync($"/Groups/{Guid.NewGuid()}");

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // TC-API-007 — Изменение группы (PATCH /Groups?groupId)
    [Fact]
    public async Task TC_API_007_UpdateGroup_Success()
    {
        // arrange
        var groupId = Guid.NewGuid();
        await factory.SeedAsync(db =>
        {
            db.Groups.Add(new Group
            {
                Id = groupId,
                Name = "Group1",
                GroupBalance = 300_000m,
                MonthlyReplenishment = 300_000m,
                SavingStrategy = SavingStrategy.Spread,
                DebtStrategy = DebtStrategy.Nullify,
            });
        });

        var body = new UpdateGroupRequest(
            Name: "Group2",
            MonthlyReplenishment: 100_000m,
            SavingStrategy: SavingStrategy.SaveForNextPeriod,
            DebtStrategy: DebtStrategy.FromNextMonth);

        // act
        var response = await _client.PatchAsJsonAsync($"/Groups/?groupId={groupId}", body);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<Group>();
        Assert.NotNull(updated);
        Assert.Equal("Group2", updated.Name);
        Assert.Equal(100_000m, updated.MonthlyReplenishment);
        Assert.Equal(SavingStrategy.SaveForNextPeriod, updated.SavingStrategy);
        Assert.Equal(DebtStrategy.FromNextMonth, updated.DebtStrategy);
    }

    // TC-API-008 — Изменение группы с невалидными данными (PATCH /Groups?groupId)
    [Fact]
    public async Task TC_API_008_UpdateGroup_InvalidData_BadRequest()
    {
        // arrange
        var groupId = Guid.NewGuid();
        await factory.SeedAsync(db =>
        {
            db.Groups.Add(new Group
            {
                Id = groupId,
                Name = "Group1",
                GroupBalance = 300_000m,
                MonthlyReplenishment = 300_000m,
                SavingStrategy = SavingStrategy.Spread,
                DebtStrategy = DebtStrategy.Nullify,
            });
        });

        var body = new UpdateGroupRequest(
            Name: "",
            MonthlyReplenishment: null,
            SavingStrategy: null,
            DebtStrategy: null);

        // act
        var response = await _client.PatchAsJsonAsync($"/Groups/?groupId={groupId}", body);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // TC-API-009 — Изменение копилки (PATCH /Groups/ChangeGoal)
    [Fact]
    public async Task TC_API_009_ChangeGoal_Success()
    {
        // arrange
        var groupId = Guid.NewGuid();
        await factory.SeedAsync(db =>
        {
            var group = new Group
            {
                Id = groupId,
                Name = "Group1",
                GroupBalance = 300_000m,
                MonthlyReplenishment = 300_000m,
                SavingStrategy = SavingStrategy.Spread,
                DebtStrategy = DebtStrategy.Nullify,
            };

            db.Groups.Add(group);
            db.Savings.Add(new Saving
            {
                Id = Guid.NewGuid(),
                Name = "Отпуск",
                TargetAmount = 1_000m,
                CurrentAmount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                GroupId = group.Id
            });
        });

        // act
        var response = await _client.PatchAsync(
            $"/Groups/ChangeGoal?groupId={groupId}&targetName=Car&targetCost=1000000",
            content: null);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saving = await response.Content.ReadFromJsonAsync<Saving>();
        Assert.NotNull(saving);
        Assert.Equal("Car", saving.Name);
        Assert.Equal(1_000_000m, saving.TargetAmount);
    }

    // TC-API-010 — Получение всех пользователей группы (GET /Groups/{groupId})
    [Fact]
    public async Task TC_API_010_GetGroupUsers_Success()
    {
        // arrange
        var groupId = Guid.NewGuid();
        await factory.SeedAsync(db =>
        {
            var user1 = CreateUser();
            var user2 = CreateUser();
            var user3 = CreateUser();

            var group = new Group
            {
                Id = groupId,
                Name = "Group1",
                GroupBalance = 300_000m,
                MonthlyReplenishment = 300_000m,
                SavingStrategy = SavingStrategy.Spread,
                DebtStrategy = DebtStrategy.Nullify,
                CreatorId = user1.Id
            };

            var saving = new Saving
            {
                Id = Guid.NewGuid(),
                Name = "Отпуск",
                TargetAmount = 1_000_000m,
                CurrentAmount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                GroupId = group.Id
            };

            var account1 = new Account
            {
                Id = Random.Shared.Next(),
                Role = Role.Admin,
                DailyAllocation = 10_000m,
                MonthlyAllocation = 100_000m,
                SavingStrategy = SavingStrategy.Save,
                Balance = 0,
                UserId = user1.Id,
                GroupId = group.Id,
            };

            var account2 = new Account
            {
                Id = Random.Shared.Next(),
                Role = Role.Member,
                DailyAllocation = 10_000m,
                MonthlyAllocation = 100_000m,
                SavingStrategy = SavingStrategy.Save,
                Balance = 0,
                UserId = user2.Id,
                GroupId = group.Id,
            };

            var account3 = new Account
            {
                Id = Random.Shared.Next(),
                Role = Role.Member,
                DailyAllocation = 10_000m,
                MonthlyAllocation = 100_000m,
                SavingStrategy = SavingStrategy.Save,
                Balance = 0,
                UserId = user3.Id,
                GroupId = group.Id,
            };

            db.Users.AddRange(user1, user2, user3);
            db.Groups.Add(group);
            db.Savings.Add(saving);
            db.Accounts.AddRange(account1, account2, account3);
        });

        // act
        var response = await _client.GetAsync($"/Groups/{groupId}");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var group = await response.Content.ReadFromJsonAsync<Group>();
        Assert.NotNull(group);
        Assert.NotNull(group.Accounts);
        Assert.All(group.Accounts, a => Assert.NotNull(a.User));
    }
}