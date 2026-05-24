using FinBot.Bll.Implementation.Services;
using FinBot.Bll.Tests.Infrastructure;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinBot.Bll.Tests.Services;

public class GroupBackgroundServiceTests
{
    private const decimal MonthlyReplenishment = 10_000m;

    private static GroupBackgroundService CreateService(Dal.DbContexts.PDbContext db) =>
        new(db, NullLogger<GroupBackgroundService>.Instance);

    private static Group SeedGroupWithSingleAccount(
        Dal.DbContexts.PDbContext db,
        decimal initialGroupBalance,
        SavingStrategy groupSavingStrategy,
        DebtStrategy debtStrategy,
        decimal savingCurrent = 0m,
        decimal savingTarget = 1_000_000m)
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, DisplayName = "U" };
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "G",
            GroupBalance = initialGroupBalance,
            MonthlyReplenishment = MonthlyReplenishment,
            SavingStrategy = groupSavingStrategy,
            DebtStrategy = debtStrategy,
            CreatorId = user.Id,
        };

        var account = new Account
        {
            Id = Random.Shared.Next(),
            Role = Role.Admin,
            DailyAllocation = 0,
            MonthlyAllocation = MonthlyReplenishment,
            SavingStrategy = SavingStrategy.SaveForNextPeriod,
            Balance = 0,
            UserId = user.Id,
            GroupId = group.Id,
        };

        var saving = new Saving
        {
            Id = Guid.NewGuid(),
            Name = "Goal",
            TargetAmount = savingTarget,
            CurrentAmount = savingCurrent,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            GroupId = group.Id,
        };

        db.Users.Add(user);
        db.Groups.Add(group);
        db.Accounts.Add(account);
        db.Savings.Add(saving);
        return group;
    }

    // TC-DIST-001 — SavingStrategy.Save: остаток уходит в копилку, GroupBalance = MonthlyReplenishment
    [Fact]
    public async Task TC_DIST_001_MonthlyRefresh_SaveStrategy_LeftoverGoesToSaving()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithSingleAccount(db,
            initialGroupBalance: 3500m,
            groupSavingStrategy: SavingStrategy.Save,
            debtStrategy: DebtStrategy.Nullify);
        await db.SaveChangesAsync();

        var result = await CreateService(db).MonthlyGroupRefreshAsync(group.Id);

        Assert.True(result.IsSuccess);
        var savingAfter = await db.Savings.AsNoTracking().FirstAsync(s => s.GroupId == group.Id);
        var groupAfter = await db.Groups.AsNoTracking().FirstAsync(g => g.Id == group.Id);
        Assert.Equal(3500m, savingAfter.CurrentAmount);
        Assert.Equal(MonthlyReplenishment, groupAfter.GroupBalance);
    }

    // TC-DIST-002 — SavingStrategy.Spread: остаток переносится на следующий период
    [Fact]
    public async Task TC_DIST_002_MonthlyRefresh_SpreadStrategy_LeftoverRollsOver()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithSingleAccount(db,
            initialGroupBalance: 3500m,
            groupSavingStrategy: SavingStrategy.Spread,
            debtStrategy: DebtStrategy.Nullify);
        await db.SaveChangesAsync();

        var result = await CreateService(db).MonthlyGroupRefreshAsync(group.Id);

        Assert.True(result.IsSuccess);
        var groupAfter = await db.Groups.AsNoTracking().FirstAsync(g => g.Id == group.Id);
        Assert.Equal(MonthlyReplenishment + 3500m, groupAfter.GroupBalance);
    }

    // TC-DIST-003 — DebtStrategy.FromSaving, копилки хватает: долг покрывается из копилки
    [Fact]
    public async Task TC_DIST_003_MonthlyRefresh_FromSaving_SufficientFunds()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithSingleAccount(db,
            initialGroupBalance: -3500m,
            groupSavingStrategy: SavingStrategy.Save,
            debtStrategy: DebtStrategy.FromSaving,
            savingCurrent: 4000m);
        await db.SaveChangesAsync();

        var expectedSavings = group.Saving!.CurrentAmount + group.GroupBalance;

        var result = await CreateService(db).MonthlyGroupRefreshAsync(group.Id);

        Assert.True(result.IsSuccess);
        var savingAfter = await db.Savings.AsNoTracking().FirstAsync(s => s.GroupId == group.Id);
        var groupAfter = await db.Groups.AsNoTracking().FirstAsync(g => g.Id == group.Id);
        Assert.Equal(expectedSavings, savingAfter.CurrentAmount);
        Assert.Equal(MonthlyReplenishment, groupAfter.GroupBalance);
    }

    // TC-DIST-004 — DebtStrategy.FromSaving, копилки не хватает: копилка обнуляется, остаток долга уменьшает пополнение
    [Fact]
    public async Task TC_DIST_004_MonthlyRefresh_FromSaving_InsufficientFunds()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithSingleAccount(db,
            initialGroupBalance: -3500m,
            groupSavingStrategy: SavingStrategy.Save,
            debtStrategy: DebtStrategy.FromSaving,
            savingCurrent: 2000m);
        await db.SaveChangesAsync();

        var expectedGroupBalance = MonthlyReplenishment + (group.GroupBalance + group.Saving!.CurrentAmount);
        var expectedSavings = 0m;

        var result = await CreateService(db).MonthlyGroupRefreshAsync(group.Id);

        Assert.True(result.IsSuccess);
        var savingAfter = await db.Savings.AsNoTracking().FirstAsync(s => s.GroupId == group.Id);
        var groupAfter = await db.Groups.AsNoTracking().FirstAsync(g => g.Id == group.Id);
        Assert.Equal(expectedSavings, savingAfter.CurrentAmount);
        Assert.Equal(expectedGroupBalance, groupAfter.GroupBalance);
    }

    // TC-DIST-005 — DebtStrategy.FromNextMonth: долг уменьшает следующее пополнение
    [Fact]
    public async Task TC_DIST_005_MonthlyRefresh_FromNextMonth()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithSingleAccount(db,
            initialGroupBalance: -3500m,
            groupSavingStrategy: SavingStrategy.Spread,
            debtStrategy: DebtStrategy.FromNextMonth);
        await db.SaveChangesAsync();

        var result = await CreateService(db).MonthlyGroupRefreshAsync(group.Id);

        Assert.True(result.IsSuccess);
        var groupAfter = await db.Groups.AsNoTracking().FirstAsync(g => g.Id == group.Id);
        Assert.Equal(MonthlyReplenishment - 3500m, groupAfter.GroupBalance);
    }

    // TC-DIST-006 — DebtStrategy.Nullify: долг прощается, GroupBalance = MonthlyReplenishment
    [Fact]
    public async Task TC_DIST_006_MonthlyRefresh_Nullify()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithSingleAccount(db,
            initialGroupBalance: -3500m,
            groupSavingStrategy: SavingStrategy.Spread,
            debtStrategy: DebtStrategy.Nullify);
        await db.SaveChangesAsync();

        var result = await CreateService(db).MonthlyGroupRefreshAsync(group.Id);

        Assert.True(result.IsSuccess);
        var groupAfter = await db.Groups.AsNoTracking().FirstAsync(g => g.Id == group.Id);
        Assert.Equal(MonthlyReplenishment, groupAfter.GroupBalance);
    }

    // Доп. покрытие: MonthlyGroupRefreshAsync — группа не найдена → Failure
    [Fact]
    public async Task MonthlyRefresh_GroupNotFound()
    {
        await using var db = TestDbFactory.Create();

        var result = await CreateService(db).MonthlyGroupRefreshAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // Доп. покрытие: DailyAccountsRecalculateAsync — группа не найдена → NotFound
    [Fact]
    public async Task DailyRecalculate_GroupNotFound()
    {
        await using var db = TestDbFactory.Create();

        var result = await CreateService(db).DailyAccountsRecalculateAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    private static int DaysInMonthLeft()
    {
        var now = DateTime.Now;
        return DateTime.DaysInMonth(now.Year, now.Month) - (now.Day - 1);
    }

    // TC-DAILY-001 — DailyAccountsRecalculateAsync: SaveForNextPeriod, баланс остаётся, пересчёт начислений
    [Fact]
    public async Task TC_DAILY_001_DailyRecalculate_SaveForNextPeriod_Success()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithSingleAccount(db,
            initialGroupBalance: 10_000m,
            groupSavingStrategy: SavingStrategy.Spread,
            debtStrategy: DebtStrategy.Nullify);
        await db.SaveChangesAsync();

        var account = group.Accounts.Single();
        var balanceBefore = account.Balance;
        var groupBalanceBefore = group.GroupBalance;
        var monthlyBefore = account.MonthlyAllocation;
        var daysLeft = DaysInMonthLeft();

        var expectedMonthly = Math.Round(monthlyBefore * 1m, 2, MidpointRounding.ToZero);
        var expectedDaily = Math.Round(expectedMonthly / daysLeft, 2, MidpointRounding.ToZero);

        var result = await CreateService(db).DailyAccountsRecalculateAsync(group.Id);

        Assert.True(result.IsSuccess);
        var accountAfter = await db.Accounts.AsNoTracking().FirstAsync(a => a.Id == account.Id);
        var groupAfter = await db.Groups.AsNoTracking().FirstAsync(g => g.Id == group.Id);
        Assert.Equal(expectedMonthly, accountAfter.MonthlyAllocation);
        Assert.Equal(expectedDaily, accountAfter.DailyAllocation);
        Assert.Equal(balanceBefore + expectedDaily, accountAfter.Balance);
        Assert.Equal(groupBalanceBefore - expectedDaily, groupAfter.GroupBalance);
    }

    // TC-DAILY-002 — DailyAccountsRecalculateAsync: положительный остаток с Spread уходит в groupBalance
    [Fact]
    public async Task TC_DAILY_002_DailyRecalculate_AccountSpread_BalanceRollsIntoGroup()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithSingleAccount(db,
            initialGroupBalance: 0m,
            groupSavingStrategy: SavingStrategy.Spread,
            debtStrategy: DebtStrategy.Nullify);
        var account = group.Accounts.Single();
        account.SavingStrategy = SavingStrategy.Spread;
        account.Balance = 1_500m;
        await db.SaveChangesAsync();

        var daysLeft = DaysInMonthLeft();
        var expectedMonthly = Math.Round(10_000m * (1_500m / 10_000m), 2, MidpointRounding.ToZero);
        var expectedDaily = Math.Round(expectedMonthly / daysLeft, 2, MidpointRounding.ToZero);

        var result = await CreateService(db).DailyAccountsRecalculateAsync(group.Id);

        Assert.True(result.IsSuccess);
        var accountAfter = await db.Accounts.AsNoTracking().FirstAsync(a => a.Id == account.Id);
        var groupAfter = await db.Groups.AsNoTracking().FirstAsync(g => g.Id == group.Id);
        Assert.Equal(expectedMonthly, accountAfter.MonthlyAllocation);
        Assert.Equal(expectedDaily, accountAfter.DailyAllocation);
        Assert.Equal(0m + expectedDaily, accountAfter.Balance);
        Assert.Equal(1_500m - expectedDaily, groupAfter.GroupBalance);
    }

    // TC-DAILY-003 — DailyAccountsRecalculateAsync: положительный остаток с Save уходит в копилку
    [Fact]
    public async Task TC_DAILY_003_DailyRecalculate_AccountSave_BalanceGoesToSaving()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithSingleAccount(db,
            initialGroupBalance: 5_000m,
            groupSavingStrategy: SavingStrategy.Spread,
            debtStrategy: DebtStrategy.Nullify,
            savingCurrent: 1_000m);
        var account = group.Accounts.Single();
        account.SavingStrategy = SavingStrategy.Save;
        account.Balance = 2_000m;
        await db.SaveChangesAsync();

        var result = await CreateService(db).DailyAccountsRecalculateAsync(group.Id);

        Assert.True(result.IsSuccess);
        var savingAfter = await db.Savings.AsNoTracking().FirstAsync(s => s.GroupId == group.Id);
        var accountAfter = await db.Accounts.AsNoTracking().FirstAsync(a => a.Id == account.Id);
        // 1000 (стартовое) + 2000 (account.Balance) = 3000
        Assert.Equal(3_000m, savingAfter.CurrentAmount);
        Assert.NotEqual(2_000m, accountAfter.Balance); // баланс был обнулён, потом += daily
    }

    // TC-DAILY-004 — DailyAccountsRecalculateAsync: отрицательный баланс гасится из groupBalance
    [Fact]
    public async Task TC_DAILY_004_DailyRecalculate_NegativeAccountBalance_AbsorbedByGroup()
    {
        await using var db = TestDbFactory.Create();
        var group = SeedGroupWithSingleAccount(db,
            initialGroupBalance: 10_000m,
            groupSavingStrategy: SavingStrategy.Spread,
            debtStrategy: DebtStrategy.Nullify);
        var account = group.Accounts.Single();
        account.Balance = -2_000m;
        await db.SaveChangesAsync();

        var daysLeft = DaysInMonthLeft();
        var expectedMonthly = Math.Round(10_000m * 0.8m, 2, MidpointRounding.ToZero);
        var expectedDaily = Math.Round(expectedMonthly / daysLeft, 2, MidpointRounding.ToZero);

        var result = await CreateService(db).DailyAccountsRecalculateAsync(group.Id);

        Assert.True(result.IsSuccess);
        var accountAfter = await db.Accounts.AsNoTracking().FirstAsync(a => a.Id == account.Id);
        var groupAfter = await db.Groups.AsNoTracking().FirstAsync(g => g.Id == group.Id);
        Assert.Equal(expectedMonthly, accountAfter.MonthlyAllocation);
        Assert.Equal(0m + expectedDaily, accountAfter.Balance);
        Assert.Equal(8_000m - expectedDaily, groupAfter.GroupBalance);
    }
}