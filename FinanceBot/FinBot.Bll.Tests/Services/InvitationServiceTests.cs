using FinBot.Bll.Implementation.Services;
using FinBot.Bll.Tests.Infrastructure;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinBot.Bll.Tests.Services;

public class InvitationServiceTests
{
    private static InvitationService CreateService(Dal.DbContexts.PDbContext db) =>
        new(db, NullLogger<InvitationService>.Instance);

    private static Group SeedGroup(Dal.DbContexts.PDbContext db, Guid? id = null)
    {
        var creator = new User { Id = Guid.NewGuid(), TelegramId = Random.Shared.NextInt64(), DisplayName = "Creator" };
        var group = new Group
        {
            Id = id ?? Guid.NewGuid(),
            Name = "G",
            MonthlyReplenishment = 10_000m,
            SavingStrategy = SavingStrategy.Save,
            DebtStrategy = DebtStrategy.Nullify,
            CreatorId = creator.Id,
        };
        db.Users.Add(creator);
        db.Groups.Add(group);
        return group;
    }

    private static User SeedUser(Dal.DbContexts.PDbContext db, long tgId)
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = tgId, DisplayName = "U" };
        db.Users.Add(user);
        return user;
    }

    // TC-INV-001 — GenerateInviteCodeAsync: код совпадает с groupId
    [Fact]
    public async Task TC_INV_001_GenerateInviteCode_ReturnsGroupId()
    {
        await using var db = TestDbFactory.Create();
        var groupId = Guid.NewGuid();

        var result = await CreateService(db).GenerateInviteCodeAsync(groupId);

        Assert.True(result.IsSuccess);
        Assert.Equal(groupId.ToString(), result.Data);
    }

    // TC-INV-002 — JoinGroupByCodeAsync: успешное присоединение
    [Fact]
    public async Task TC_INV_002_JoinGroupByCode_Success()
    {
        await using var db = TestDbFactory.Create();
        var user = SeedUser(db, tgId: 1);
        var group = SeedGroup(db);
        await db.SaveChangesAsync();

        var result = await CreateService(db).JoinGroupByCodeAsync(1, group.Id.ToString());

        Assert.True(result.IsSuccess);
        Assert.Equal(group.Id, result.Data.Id);
        var pending = db.JoinRequests.Local.SingleOrDefault();
        Assert.NotNull(pending);
        Assert.Equal(user.Id, pending.UserId);
        Assert.Equal(group.Id, pending.GroupId);
    }

    // TC-INV-003 — JoinGroupByCodeAsync: пользователь не найден
    [Fact]
    public async Task TC_INV_003_JoinGroupByCode_UserNotFound()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroup(db);
        await db.SaveChangesAsync();

        var result = await CreateService(db).JoinGroupByCodeAsync(999, group.Id.ToString());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // TC-INV-004 — JoinGroupByCodeAsync: код не парсится как Guid
    [Fact]
    public async Task TC_INV_004_JoinGroupByCode_InvalidCodeFormat()
    {
        await using var db = TestDbFactory.Create();
        SeedUser(db, tgId: 1);
        await db.SaveChangesAsync();

        var result = await CreateService(db).JoinGroupByCodeAsync(1, "not-a-guid");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    // TC-INV-005 — JoinGroupByCodeAsync: код парсится, но группа не существует
    [Fact]
    public async Task TC_INV_005_JoinGroupByCode_GroupNotFound()
    {
        await using var db = TestDbFactory.Create();
        SeedUser(db, tgId: 1);
        await db.SaveChangesAsync();

        var result = await CreateService(db).JoinGroupByCodeAsync(1, Guid.NewGuid().ToString());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    // TC-INV-006 — GetPendingUsersAsync: возвращает всех пользователей с join-запросами в группу
    [Fact]
    public async Task TC_INV_006_GetPendingUsers_Success()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroup(db);
        var u1 = SeedUser(db, tgId: 1);
        var u2 = SeedUser(db, tgId: 2);
        var u3 = SeedUser(db, tgId: 3);
        db.JoinRequests.AddRange(
            new JoinRequest { CreatedAt = DateTime.UtcNow, UserId = u1.Id, GroupId = group.Id },
            new JoinRequest { CreatedAt = DateTime.UtcNow, UserId = u2.Id, GroupId = group.Id },
            new JoinRequest { CreatedAt = DateTime.UtcNow, UserId = u3.Id, GroupId = group.Id });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetPendingUsersAsync(group.Id);

        Assert.True(result.IsSuccess);
        var ids = result.Data.Select(u => u.Id).ToHashSet();
        Assert.Equal(3, ids.Count);
        Assert.Contains(u1.Id, ids);
        Assert.Contains(u2.Id, ids);
        Assert.Contains(u3.Id, ids);
    }

    // TC-INV-007 — GetPendingUsersAsync: возвращает только пользователей запрашиваемой группы
    [Fact]
    public async Task TC_INV_007_GetPendingUsers_FiltersByGroup()
    {
        await using var db = TestDbFactory.Create();
        var group1 = SeedGroup(db);
        var group2 = SeedGroup(db);
        var u1 = SeedUser(db, tgId: 1);
        var u2 = SeedUser(db, tgId: 2);
        db.JoinRequests.AddRange(
            new JoinRequest { CreatedAt = DateTime.UtcNow, UserId = u1.Id, GroupId = group1.Id },
            new JoinRequest { CreatedAt = DateTime.UtcNow, UserId = u2.Id, GroupId = group2.Id });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetPendingUsersAsync(group1.Id);

        Assert.True(result.IsSuccess);
        var single = Assert.Single(result.Data);
        Assert.Equal(u1.Id, single.Id);
    }

    // TC-INV-008 — GetPendingUsersAsync: пустой список, если нет запросов
    [Fact]
    public async Task TC_INV_008_GetPendingUsers_EmptyList()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroup(db);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetPendingUsersAsync(group.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
    }
}