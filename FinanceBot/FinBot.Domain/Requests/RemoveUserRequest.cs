namespace FinBot.Domain.Requests;

public record RemoveUserRequest(
    long UserTgId,
    decimal[] OldUsersAllocations);