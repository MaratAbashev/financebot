using FinBot.Domain.Models.Enums;

namespace FinBot.Domain.Requests;

public record CreateGroupRequest(
    string GroupName,
    decimal Replenishment,
    SavingStrategy GroupSavingStrategy,
    SavingStrategy AccountSavingStrategy,
    DebtStrategy DebtStrategy,
    string? SavingTargetName,
    decimal? SavingTargetAmount);