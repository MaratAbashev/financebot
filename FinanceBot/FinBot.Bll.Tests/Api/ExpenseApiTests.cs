using System.Net;
using System.Net.Http.Json;
using FinBot.Bll.Tests.Infrastructure;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Requests;

namespace FinBot.Bll.Tests.Api;

public class ExpenseApiTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // TC-API-014 — Внесение новой траты (POST /Expenses/Add)
    [Fact]
    public async Task TC_API_014_AddExpense_Success()
    {
        // arrange
        var userTgId = Random.Shared.NextInt64();
        var groupId = Guid.NewGuid();
        var oldAccountBalance = 30_000m;
        await factory.SeedAsync(db =>
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                TelegramId = userTgId,
                DisplayName = "Test3",
            };

            var group = new Group
            {
                Id = groupId,
                Name = "Group1",
                GroupBalance = 100_000m,
                MonthlyReplenishment = 100_000m,
                SavingStrategy = SavingStrategy.Spread,
                DebtStrategy = DebtStrategy.Nullify,
                CreatorId = user.Id
            };

            db.Users.Add(user);
            db.Groups.Add(group);
            db.Accounts.Add(new Account
            {
                Id = Random.Shared.Next(),
                Role = Role.Admin,
                DailyAllocation = 10_000m,
                MonthlyAllocation = 100_000m,
                SavingStrategy = SavingStrategy.Save,
                Balance = oldAccountBalance,
                UserId = user.Id,
                GroupId = group.Id,
            });
        });

        var body = new AddExpenseRequest(
            GroupId: groupId,
            Amount: 10_000m,
            Category: (ExpenseCategory)3);

        // act
        var response = await _client.PostAsJsonAsync($"/Expenses/Add?userTgId={userTgId}", body);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var newBalance = await response.Content.ReadFromJsonAsync<decimal>();
        Assert.Equal(oldAccountBalance - body.Amount, newBalance);
    }

    // TC-API-015 — Внесение траты без обязательных полей (POST /Expenses/Add)
    [Fact]
    public async Task TC_API_015_AddExpense_MissingFields_BadRequest()
    {
        // arrange
        var userTgId = Random.Shared.NextInt64();
        await factory.SeedAsync(db =>
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                TelegramId = userTgId,
                DisplayName = "Test3",
            });
        });

        var body = new
        {
            GroupId = (Guid?)null,
            Amount = 10_000m,
            Category = 3
        };

        // act
        var response = await _client.PostAsJsonAsync($"/Expenses/Add?userTgId={userTgId}", body);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // TC-API-016 — Получение нераспределённых трат (GET /Expenses/Pending)
    [Fact]
    public async Task TC_API_016_GetPendingExpenses_Success()
    {
        // arrange
        var userTgId = Random.Shared.NextInt64();
        var userId = Guid.NewGuid();
        await factory.SeedAsync(db =>
        {
            db.Users.Add(new User
            {
                Id = userId,
                TelegramId = userTgId,
                DisplayName = "Test3",
            });

            db.AddRange(
                new Expense
                {
                    Id = 10, Category = ExpenseCategory.Food, Amount = 100m, Date = DateTime.UtcNow, UserId = userId
                },
                new Expense
                {
                    Id = 11, Category = ExpenseCategory.Transport, Amount = 200m, Date = DateTime.UtcNow,
                    UserId = userId
                },
                new Expense
                {
                    Id = 12, Category = ExpenseCategory.Health, Amount = 300m, Date = DateTime.UtcNow, UserId = userId
                }
            );
        });

        // act
        var response = await _client.GetAsync($"/Expenses/Pending?userTgId={userTgId}");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pending = await response.Content.ReadFromJsonAsync<List<Expense>>();
        Assert.NotNull(pending);
        Assert.All(pending, e => Assert.Null(e.GroupId));
        Assert.All(pending, e => Assert.Equal(e.UserId, userId));
    }
}