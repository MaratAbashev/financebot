using FinBot.Domain.Models.Enums;

namespace FinBot.BankService.Mappers;

public static class CategoryMapper
{
    public static ExpenseCategory Map(string? categoryName) => categoryName?.ToLower() switch
    {
        "продукты"     => ExpenseCategory.Food,
        "рестораны"    => ExpenseCategory.Food,
        "транспорт"    => ExpenseCategory.Transport,
        "жкх"          => ExpenseCategory.Housing,
        "здоровье"     => ExpenseCategory.Health,
        "развлечения"  => ExpenseCategory.Entertainment,
        "фриланс"      => ExpenseCategory.Other,
        "зарплата"     => ExpenseCategory.Other,
        _              => (ExpenseCategory)0
    };
}