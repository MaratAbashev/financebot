using FinBot.Domain.Models.Enums;

namespace FinBot.Domain.Requests;

public record AddUserToGroupRequest(
    long UserTgId,
    Role UserRole,
    decimal[] OldUsersAllocations,
    decimal NewUserAllocation,
    SavingStrategy UserSavingStrategy);