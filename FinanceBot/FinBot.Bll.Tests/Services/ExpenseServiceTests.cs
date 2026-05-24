using FinBot.Bll.Implementation.Services;
using FinBot.Bll.Tests.Infrastructure;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinBot.Bll.Tests.Services;

public class ExpenseServiceTests
{
    private static ExpenseService CreateService(PDbContext db) =>
        new(db, NullLogger<ExpenseService>.Instance);

    private static (Group group, Account account) SeedUserInGroup(
        PDbContext db,
        long userTgId,
        decimal accountBalance)
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = userTgId, DisplayName = "Test" };
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "G",
            GroupBalance = 0,
            MonthlyReplenishment = 30_000m,
            SavingStrategy = SavingStrategy.Save,
            DebtStrategy = DebtStrategy.Nullify,
        };
        var account = new Account
        {
            Id = Random.Shared.Next(),
            Role = Role.Member,
            DailyAllocation = 1_000m,
            MonthlyAllocation = 30_000m,
            SavingStrategy = SavingStrategy.Spread,
            Balance = accountBalance,
            UserId = user.Id,
            GroupId = group.Id,
        };
        db.Users.Add(user);
        db.Groups.Add(group);
        db.Accounts.Add(account);
        return (group, account);
    }

    // TC-USER-009 — AddExpenseAsync: успешное добавление расхода
    [Fact]
    public async Task TC_USER_009_AddExpense_Success()
    {
        await using var db = TestDbFactory.Create();
        var (group, account) = SeedUserInGroup(db, userTgId: 1, accountBalance: 5_000m);
        await db.SaveChangesAsync();

        var result = await CreateService(db).AddExpenseAsync(1, group.Id, 500m, ExpenseCategory.Food);

        Assert.True(result.IsSuccess);
        Assert.Equal(4_500m, result.Data);
        var expense = Assert.Single(db.Expenses);
        Assert.Equal(500m, expense.Amount);
        Assert.Equal(ExpenseCategory.Food, expense.Category);
        Assert.Equal(group.Id, expense.GroupId);
        Assert.Equal(account.UserId, expense.UserId);
    }

    // TC-USER-010 — AddExpenseAsync: пользователь не найден
    [Fact]
    public async Task TC_USER_010_AddExpense_UserNotFound()
    {
        await using var db = TestDbFactory.Create();
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "G",
            MonthlyReplenishment = 10_000m,
            SavingStrategy = SavingStrategy.Save,
            DebtStrategy = DebtStrategy.Nullify,
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var result = await CreateService(db).AddExpenseAsync(999, group.Id, 500m, ExpenseCategory.Food);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(db.Expenses);
    }

    // TC-USER-011 — AddExpenseAsync: группа не существует
    [Fact]
    public async Task TC_USER_011_AddExpense_GroupNotFound()
    {
        await using var db = TestDbFactory.Create();
        db.Users.Add(new User { Id = Guid.NewGuid(), TelegramId = 1, DisplayName = "Test" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).AddExpenseAsync(1, Guid.NewGuid(), 500m, ExpenseCategory.Food);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(db.Expenses);
    }

    // TC-USER-013 — AddExpenseAsync: у пользователя нет аккаунта в данной группе
    [Fact]
    public async Task TC_USER_013_AddExpense_NoAccountInGroup()
    {
        await using var db = TestDbFactory.Create();
        db.Users.Add(new User { Id = Guid.NewGuid(), TelegramId = 1, DisplayName = "Test" });
        db.Groups.Add(new Group
        {
            Id = Guid.NewGuid(),
            Name = "G",
            MonthlyReplenishment = 10_000m,
            SavingStrategy = SavingStrategy.Save,
            DebtStrategy = DebtStrategy.Nullify,
        });
        var foreignGroupId = Guid.NewGuid();
        db.Groups.Add(new Group
        {
            Id = foreignGroupId,
            Name = "Foreign",
            MonthlyReplenishment = 10_000m,
            SavingStrategy = SavingStrategy.Save,
            DebtStrategy = DebtStrategy.Nullify,
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).AddExpenseAsync(1, foreignGroupId, 500m, ExpenseCategory.Food);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(db.Expenses);
    }

    private static void SeedPendingExpense(PDbContext db, int id, Guid userId, decimal amount)
    {
        var expense = new Expense
        {
            Id = id,
            Category = ExpenseCategory.Other,
            Amount = amount,
            Date = DateTime.UtcNow,
            UserId = userId,
            GroupId = null,
        };
        db.Expenses.Add(expense);
    }

    // TC-EXP-DIST-001 — DistributeExpensesAsync: успешное распределение одной траты
    [Fact]
    public async Task TC_EXP_DIST_001_Distribute_Success_SingleExpense()
    {
        await using var db = TestDbFactory.Create();
        var (group, account) = SeedUserInGroup(db, userTgId: 1, accountBalance: 5_000m);
        SeedPendingExpense(db, id: 100, userId: account.UserId, amount: 800m);
        await db.SaveChangesAsync();

        var result = await CreateService(db).DistributeExpensesAsync(
            userTgId: 1,
            100, group.Id, ExpenseCategory.Food);

        Assert.True(result.IsSuccess);
        var expense = await db.Expenses.AsNoTracking().FirstAsync(e => e.Id == 100);
        Assert.Equal(group.Id, expense.GroupId);
        Assert.Equal(ExpenseCategory.Food, expense.Category);
        var accountAfter = await db.Accounts.AsNoTracking().FirstAsync(a => a.Id == account.Id);
        Assert.Equal(5_000m - 800m, accountAfter.Balance);
    }

    // TC-EXP-DIST-002 — DistributeExpensesAsync: пользователь не найден
    [Fact]
    public async Task TC_EXP_DIST_003_Distribute_UserNotFound()
    {
        await using var db = TestDbFactory.Create();
        var (group, account) = SeedUserInGroup(db, userTgId: 1, accountBalance: 5_000m);
        SeedPendingExpense(db, id: 103, userId: account.UserId, amount: 500m);
        await db.SaveChangesAsync();

        var result = await CreateService(db).DistributeExpensesAsync(
            userTgId: 999,
            103, group.Id, ExpenseCategory.Food);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        var expense = await db.Expenses.AsNoTracking().FirstAsync(e => e.Id == 103);
        Assert.Null(expense.GroupId);
    }

    // TC-EXP-DIST-005 — DistributeExpensesAsync: у пользователя нет аккаунта в целевой группе
    [Fact]
    public async Task TC_EXP_DIST_005_Distribute_NoAccountInGroup()
    {
        await using var db = TestDbFactory.Create();
        var (_, account) = SeedUserInGroup(db, userTgId: 1, accountBalance: 5_000m);
        var foreignGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Foreign",
            MonthlyReplenishment = 10_000m,
            SavingStrategy = SavingStrategy.Save,
            DebtStrategy = DebtStrategy.Nullify,
        };
        db.Groups.Add(foreignGroup);
        SeedPendingExpense(db, id: 106, userId: account.UserId, amount: 500m);
        await db.SaveChangesAsync();

        var result = await CreateService(db).DistributeExpensesAsync(
            userTgId: 1,
            106, foreignGroup.Id, ExpenseCategory.Food);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        var balanceAfter = (await db.Accounts.AsNoTracking().FirstAsync(a => a.Id == account.Id)).Balance;
        Assert.Equal(5_000m, balanceAfter);
        var expense = await db.Expenses.AsNoTracking().FirstAsync(e => e.Id == 106);
        Assert.Null(expense.GroupId);
    }
}