using FinBot.Bll.Implementation.Services;
using FinBot.Bll.Tests.Infrastructure;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinBot.Bll.Tests.Services;

public class UserServiceTests
{
    private static UserService CreateService(Dal.DbContexts.PDbContext db) =>
        new(db, NullLogger<UserService>.Instance);

    // TC-USER-001 — GetUserByTgIdAsync: пользователь найден
    [Fact]
    public async Task TC_USER_001_GetUserByTgId_Success()
    {
        await using var db = TestDbFactory.Create();
        var user = new User { Id = Guid.NewGuid(), TelegramId = 42, DisplayName = "Иван" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetUserByTgIdAsync(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Data.Id);
        Assert.Equal(42, result.Data.TelegramId);
    }

    // TC-USER-002 — GetUserByTgIdAsync: пользователь не найден
    [Fact]
    public async Task TC_USER_002_GetUserByTgId_NotFound()
    {
        await using var db = TestDbFactory.Create();

        var result = await CreateService(db).GetUserByTgIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // TC-USER-003 — GetUserByGuidIdAsync: пользователь найден
    [Fact]
    public async Task TC_USER_003_GetUserByGuidId_Success()
    {
        await using var db = TestDbFactory.Create();
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, TelegramId = 1, DisplayName = "Иван" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetUserByGuidIdAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Data.Id);
    }

    // TC-USER-004 — GetUserByGuidIdAsync: пользователь не найден
    [Fact]
    public async Task TC_USER_004_GetUserByGuidId_NotFound()
    {
        await using var db = TestDbFactory.Create();

        var result = await CreateService(db).GetUserByGuidIdAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // TC-USER-005 — CreateUserAsync: успешное создание
    [Fact]
    public async Task TC_USER_005_CreateUser_Success()
    {
        await using var db = TestDbFactory.Create();

        var result = await CreateService(db).CreateUserAsync(100, "Иван");

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Data.TelegramId);
        Assert.Equal("Иван", result.Data.DisplayName);
        Assert.Single(db.Users);
    }

    // TC-USER-006 — CreateUserAsync: пользователь уже существует
    [Fact]
    public async Task TC_USER_006_CreateUser_AlreadyExists_Conflict()
    {
        await using var db = TestDbFactory.Create();
        db.Users.Add(new User { Id = Guid.NewGuid(), TelegramId = 100, DisplayName = "old" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).CreateUserAsync(100, "new");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Single(db.Users);
    }

    // TC-USER-007 — GetOrCreateUserAsync: пользователь существует — возврат без создания
    [Fact]
    public async Task TC_USER_007_GetOrCreateUser_Existing_ReturnsExisting()
    {
        await using var db = TestDbFactory.Create();
        var existingId = Guid.NewGuid();
        db.Users.Add(new User { Id = existingId, TelegramId = 100, DisplayName = "old" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetOrCreateUserAsync(100, "new");

        Assert.True(result.IsSuccess);
        Assert.Equal(existingId, result.Data.Id);
        Assert.Equal("old", result.Data.DisplayName);
        Assert.Single(db.Users);
    }

    // TC-USER-008 — GetOrCreateUserAsync: пользователь не существует — создание
    [Fact]
    public async Task TC_USER_008_GetOrCreateUser_New_CreatesUser()
    {
        await using var db = TestDbFactory.Create();

        var result = await CreateService(db).GetOrCreateUserAsync(100, "Иван");

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Data.TelegramId);
        Assert.Equal("Иван", result.Data.DisplayName);
        Assert.Single(db.Users);
    }
}