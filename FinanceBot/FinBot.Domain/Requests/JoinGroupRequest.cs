namespace FinBot.Domain.Requests;

public record JoinGroupRequest(long UserTgId, string Code);