using FinBot.Bll.Implementation.Services;
using FinBot.Bll.Tests.Infrastructure;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinBot.Bll.Tests.Services;

public class GroupServiceTests
{
    private static GroupService CreateService(Dal.DbContexts.PDbContext db) =>
        new(db, NullLogger<GroupService>.Instance);

    private static int DaysInMonthLeft()
    {
        var now = DateTime.UtcNow;
        return DateTime.DaysInMonth(now.Year, now.Month) - (now.Day - 1);
    }

    private static Group SeedGroupWithAdmin(
        Dal.DbContexts.PDbContext db,
        long adminTgId,
        decimal monthlyReplenishment = 100_000m,
        decimal savingCurrentAmount = 0m,
        decimal savingTargetAmount = 1_000_000m,
        SavingStrategy savingStrategy = SavingStrategy.Save,
        DebtStrategy debtStrategy = DebtStrategy.Nullify)
    {
        var admin = new User { Id = Guid.NewGuid(), TelegramId = adminTgId, DisplayName = "Admin" };
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "G",
            GroupBalance = 0,
            MonthlyReplenishment = monthlyReplenishment,
            SavingStrategy = savingStrategy,
            DebtStrategy = debtStrategy,
            CreatorId = admin.Id,
        };
        var adminAccount = new Account
        {
            Id = Random.Shared.Next(),
            Role = Role.Admin,
            DailyAllocation = 0,
            MonthlyAllocation = monthlyReplenishment,
            SavingStrategy = SavingStrategy.Save,
            Balance = 0,
            UserId = admin.Id,
            GroupId = group.Id,
        };
        var saving = new Saving
        {
            Id = Guid.NewGuid(),
            Name = "Goal",
            TargetAmount = savingTargetAmount,
            CurrentAmount = savingCurrentAmount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            GroupId = group.Id,
        };
        db.Users.Add(admin);
        db.Groups.Add(group);
        db.Accounts.Add(adminAccount);
        db.Savings.Add(saving);
        return group;
    }

    // TC-GS-001 — CreateGroupAsync: успешное создание группы
    [Fact]
    public async Task TC_GS_001_CreateGroup_Success()
    {
        await using var db = TestDbFactory.Create();
        var creator = new User { Id = Guid.NewGuid(), TelegramId = 1, DisplayName = "Иван" };
        db.Users.Add(creator);
        await db.SaveChangesAsync();

        var result = await CreateService(db).CreateGroupAsync(
            "Семья", 1, 100_000m,
            SavingStrategy.Save, SavingStrategy.Save, DebtStrategy.Nullify,
            "Отпуск", 300_000m);

        Assert.True(result.IsSuccess);
        var group = result.Data;
        Assert.Equal("Семья", group.Name);
        Assert.Equal(100_000m, group.MonthlyReplenishment);
        Assert.Equal(SavingStrategy.Save, group.SavingStrategy);
        Assert.Equal(DebtStrategy.Nullify, group.DebtStrategy);

        var account = Assert.Single(group.Accounts);
        Assert.Equal(Role.Admin, account.Role);

        Assert.NotNull(group.Saving);
        Assert.Equal("Отпуск", group.Saving!.Name);
        Assert.Equal(300_000m, group.Saving.TargetAmount);
        Assert.Equal(0m, group.Saving.CurrentAmount);
    }

    // TC-GS-002 — CreateGroupAsync: создатель не найден
    [Fact]
    public async Task TC_GS_002_CreateGroup_CreatorNotFound()
    {
        await using var db = TestDbFactory.Create();

        var result = await CreateService(db).CreateGroupAsync(
            "Семья", 999, 100_000m,
            SavingStrategy.Save, SavingStrategy.Save, DebtStrategy.Nullify,
            "Отпуск", 300_000m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(db.Groups);
    }

    // TC-GS-003 — UpdateGroupAsync: успешное частичное обновление
    [Fact]
    public async Task TC_GS_003_UpdateGroup_PartialUpdate_Success()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithAdmin(db, adminTgId: 1, monthlyReplenishment: 100_000m,
            savingStrategy: SavingStrategy.Spread, debtStrategy: DebtStrategy.Nullify);
        await db.SaveChangesAsync();

        var result = await CreateService(db).UpdateGroupAsync(
            group.Id, name: "Новое имя",
            monthlyReplenishment: null, savingStrategy: null, debtStrategy: null);

        Assert.True(result.IsSuccess);
        Assert.Equal("Новое имя", result.Data.Name);
        Assert.Equal(100_000m, result.Data.MonthlyReplenishment);
        Assert.Equal(SavingStrategy.Spread, result.Data.SavingStrategy);
        Assert.Equal(DebtStrategy.Nullify, result.Data.DebtStrategy);
    }

    // TC-GS-004 — UpdateGroupAsync: группа не найдена
    [Fact]
    public async Task TC_GS_004_UpdateGroup_GroupNotFound()
    {
        await using var db = TestDbFactory.Create();

        var result = await CreateService(db).UpdateGroupAsync(
            Guid.NewGuid(), name: "Новое имя",
            monthlyReplenishment: null, savingStrategy: null, debtStrategy: null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // TC-GS-005 — RecalculateMonthlyAllocationsAsync: корректный пересчёт начислений
    [Fact]
    public async Task TC_GS_005_RecalculateMonthlyAllocations_Success()
    {
        await using var db = TestDbFactory.Create();
        var user1 = new User { Id = Guid.NewGuid(), TelegramId = 1, DisplayName = "U1" };
        var user2 = new User { Id = Guid.NewGuid(), TelegramId = 2, DisplayName = "U2" };
        var group = new Group
        {
            Id = Guid.NewGuid(), Name = "G", MonthlyReplenishment = 100_000m,
            SavingStrategy = SavingStrategy.Save, DebtStrategy = DebtStrategy.Nullify,
        };
        var account1 = new Account
        {
            Id = 1, UserId = user1.Id, GroupId = group.Id, Role = Role.Admin,
            MonthlyAllocation = 50_000m, DailyAllocation = 1_000m, SavingStrategy = SavingStrategy.Save,
        };
        var account2 = new Account
        {
            Id = 2, UserId = user2.Id, GroupId = group.Id, Role = Role.Member,
            MonthlyAllocation = 50_000m, DailyAllocation = 1_000m, SavingStrategy = SavingStrategy.Save,
        };
        db.Users.AddRange(user1, user2);
        db.Groups.Add(group);
        db.Accounts.AddRange(account1, account2);
        await db.SaveChangesAsync();

        var daysLeft = DaysInMonthLeft();

        var result = await CreateService(db).RecalculateMonthlyAllocationsAsync(
            group.Id, [60_000m, 40_000m]);

        Assert.True(result.IsSuccess);
        var updated = await db.Accounts.OrderBy(a => a.Id).ToListAsync();
        Assert.Equal(60_000m, updated[0].MonthlyAllocation);
        Assert.Equal(40_000m, updated[1].MonthlyAllocation);
        Assert.Equal(Math.Round(60_000m / daysLeft, 2, MidpointRounding.ToZero), updated[0].DailyAllocation);
        Assert.Equal(Math.Round(40_000m / daysLeft, 2, MidpointRounding.ToZero), updated[1].DailyAllocation);
    }

    // TC-GS-006 — RecalculateMonthlyAllocationsAsync: группа не найдена
    [Fact]
    public async Task TC_GS_006_RecalculateMonthlyAllocations_GroupNotFound()
    {
        await using var db = TestDbFactory.Create();

        var result = await CreateService(db).RecalculateMonthlyAllocationsAsync(
            Guid.NewGuid(), [60_000m]);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // TC-GS-007 — ChangeGoalAsync: накопленное < цели, сохраняется
    [Fact]
    public async Task TC_GS_007_ChangeGoal_BelowTarget_PreservesAccumulated()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithAdmin(db, adminTgId: 1,
            savingCurrentAmount: 80_000m, savingTargetAmount: 300_000m);
        await db.SaveChangesAsync();

        var result = await CreateService(db).ChangeGoalAsync(group.Id, "Машина", 500_000m);

        Assert.True(result.IsSuccess);
        Assert.Equal("Машина", result.Data.Name);
        Assert.Equal(500_000m, result.Data.TargetAmount);
        Assert.Equal(80_000m, result.Data.CurrentAmount);
    }

    // TC-GS-008 — ChangeGoalAsync: накопленное >= цели, переносится излишек
    [Fact]
    public async Task TC_GS_008_ChangeGoal_AboveOrEqualTarget()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithAdmin(db, adminTgId: 1,
            savingCurrentAmount: 300_000m, savingTargetAmount: 300_000m);
        await db.SaveChangesAsync();

        var result = await CreateService(db).ChangeGoalAsync(group.Id, "Новая цель", 200_000m);

        Assert.True(result.IsSuccess);
        Assert.Equal("Новая цель", result.Data.Name);
        Assert.Equal(200_000m, result.Data.TargetAmount);
        Assert.Equal(0m, result.Data.CurrentAmount);
    }

    // TC-GS-009 — ChangeGoalAsync: группа не найдена
    [Fact]
    public async Task TC_GS_009_ChangeGoal_GroupNotFound()
    {
        await using var db = TestDbFactory.Create();

        var result = await CreateService(db).ChangeGoalAsync(Guid.NewGuid(), "Цель", 200_000m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // TC-GS-010 — AddUserToGroupAsync: успешное добавление участника
    [Fact]
    public async Task TC_GS_010_AddUserToGroup_Success()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithAdmin(db, adminTgId: 1);
        var newUser = new User { Id = Guid.NewGuid(), TelegramId = 2, DisplayName = "New" };
        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        var balanceBefore = group.GroupBalance;
        var daysLeft = DaysInMonthLeft();

        var result = await CreateService(db).AddUserToGroupAsync(
            group.Id, 2, Role.Member,
            oldUserAllocations: [50_000m],
            newUserAllocation: 50_000m,
            newUserSavingStrategy: SavingStrategy.Save);

        Assert.True(result.IsSuccess);
        Assert.Equal(Role.Member, result.Data.Role);
        Assert.Equal(50_000m, result.Data.MonthlyAllocation);
        Assert.Equal(newUser.Id, result.Data.UserId);

        var refreshedGroup = await db.Groups.AsNoTracking().FirstAsync(g => g.Id == group.Id);
        var newDaily = Math.Round(50_000m / daysLeft, 2, MidpointRounding.ToZero);
        Assert.Equal(balanceBefore - newDaily, refreshedGroup.GroupBalance);

        var adminAccount = await db.Accounts.AsNoTracking().FirstAsync(a => a.Role == Role.Admin);
        Assert.Equal(50_000m, adminAccount.MonthlyAllocation);
    }

    // TC-GS-011 — AddUserToGroupAsync: пользователь уже в группе
    [Fact]
    public async Task TC_GS_011_AddUserToGroup_AlreadyMember_Conflict()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithAdmin(db, adminTgId: 1);
        await db.SaveChangesAsync();

        var result = await CreateService(db).AddUserToGroupAsync(
            group.Id, 1, Role.Member, [50_000m], 50_000m, SavingStrategy.Save);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    // TC-GS-012 — AddUserToGroupAsync: пользователь не найден
    [Fact]
    public async Task TC_GS_012_AddUserToGroup_UserNotFound()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithAdmin(db, adminTgId: 1);
        await db.SaveChangesAsync();

        var result = await CreateService(db).AddUserToGroupAsync(
            group.Id, 999, Role.Member, [50_000m], 50_000m, SavingStrategy.Save);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // TC-GS-013 — AddUserToGroupAsync: группа не найдена
    [Fact]
    public async Task TC_GS_013_AddUserToGroup_GroupNotFound()
    {
        await using var db = TestDbFactory.Create();
        db.Users.Add(new User { Id = Guid.NewGuid(), TelegramId = 1, DisplayName = "U" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).AddUserToGroupAsync(
            Guid.NewGuid(), 1, Role.Member, [], 50_000m, SavingStrategy.Save);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // TC-GS-014 — RemoveUserFromGroupAsync: успешное удаление участника
    [Fact]
    public async Task TC_GS_014_RemoveUserFromGroup_Success()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithAdmin(db, adminTgId: 1);
        var member = new User { Id = Guid.NewGuid(), TelegramId = 2, DisplayName = "Member" };
        var memberAccount = new Account
        {
            Id = Random.Shared.Next(), UserId = member.Id, GroupId = group.Id,
            Role = Role.Member, MonthlyAllocation = 50_000m,
            SavingStrategy = SavingStrategy.Save, Balance = 0,
        };
        db.Users.Add(member);
        db.Accounts.Add(memberAccount);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RemoveUserFromGroupAsync(
            group.Id, userTgId: 2, leftUsersAllocations: [100_000m]);

        Assert.True(result.IsSuccess);
        Assert.False(await db.Accounts.AnyAsync(a => a.UserId == member.Id && a.GroupId == group.Id));
        var adminAccount = await db.Accounts.AsNoTracking().FirstAsync(a => a.Role == Role.Admin);
        Assert.Equal(100_000m, adminAccount.MonthlyAllocation);
    }

    // TC-GS-015 — RemoveUserFromGroupAsync: пользователь не найден
    [Fact]
    public async Task TC_GS_015_RemoveUserFromGroup_UserNotFound()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithAdmin(db, adminTgId: 1);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RemoveUserFromGroupAsync(
            group.Id, userTgId: 999, leftUsersAllocations: [100_000m]);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // TC-GS-016 — RemoveUserFromGroupAsync: группа не найдена
    [Fact]
    public async Task TC_GS_016_RemoveUserFromGroup_GroupNotFound()
    {
        await using var db = TestDbFactory.Create();
        db.Users.Add(new User { Id = Guid.NewGuid(), TelegramId = 1, DisplayName = "U" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).RemoveUserFromGroupAsync(
            Guid.NewGuid(), userTgId: 1, leftUsersAllocations: [100_000m]);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // TC-GS-017 — RemoveUserFromGroupAsync: пользователь не является участником группы
    [Fact]
    public async Task TC_GS_017_RemoveUserFromGroup_NotMember()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithAdmin(db, adminTgId: 1);
        db.Users.Add(new User { Id = Guid.NewGuid(), TelegramId = 2, DisplayName = "Outsider" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).RemoveUserFromGroupAsync(
            group.Id, userTgId: 2, leftUsersAllocations: [100_000m]);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // TC-GS-018 — GetGroupByIdAsync: группа найдена
    [Fact]
    public async Task TC_GS_018_GetGroupById_Success()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithAdmin(db, adminTgId: 1);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetGroupByIdAsync(group.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(group.Id, result.Data.Id);
        Assert.NotEmpty(result.Data.Accounts);
        Assert.All(result.Data.Accounts, a => Assert.NotNull(a.User));
        Assert.NotNull(result.Data.Saving);
    }

    // TC-GS-019 — GetGroupByIdAsync: группа не найдена
    [Fact]
    public async Task TC_GS_019_GetGroupById_NotFound()
    {
        await using var db = TestDbFactory.Create();

        var result = await CreateService(db).GetGroupByIdAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
