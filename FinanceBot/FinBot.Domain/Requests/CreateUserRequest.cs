namespace FinBot.Domain.Requests;

public record CreateUserRequest(long TgId, string DisplayName);