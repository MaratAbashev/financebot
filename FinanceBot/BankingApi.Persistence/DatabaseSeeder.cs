using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        var categories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Зарплата", Type = CategoryType.Income },
            new() { Id = Guid.NewGuid(), Name = "Фриланс", Type = CategoryType.Income },
            new() { Id = Guid.NewGuid(), Name = "Продукты", Type = CategoryType.Expense },
            new() { Id = Guid.NewGuid(), Name = "Транспорт", Type = CategoryType.Expense },
            new() { Id = Guid.NewGuid(), Name = "Рестораны", Type = CategoryType.Expense },
            new() { Id = Guid.NewGuid(), Name = "Развлечения", Type = CategoryType.Expense },
            new() { Id = Guid.NewGuid(), Name = "ЖКХ", Type = CategoryType.Expense },
            new() { Id = Guid.NewGuid(), Name = "Здоровье", Type = CategoryType.Expense },
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
    }
}