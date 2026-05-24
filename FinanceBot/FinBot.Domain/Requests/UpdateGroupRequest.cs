using FinBot.Domain.Models.Enums;

namespace FinBot.Domain.Requests;

public record UpdateGroupRequest(
    string? Name,
    decimal? MonthlyReplenishment,
    SavingStrategy? SavingStrategy,
    DebtStrategy? DebtStrategy);