using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class User : IBusinessEntity<Guid>
{
    public Guid Id { get; set; }
    public int TelegramId { get; set; }
    public required string Username { get; set; }
    public required string DisplayName { get; set; }
}
