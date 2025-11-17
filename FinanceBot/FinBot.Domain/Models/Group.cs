using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class Group : IBusinessEntity<Guid>
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public GroupSetting? Settings { get; set; }
    
    public Guid CreatorId { get; set; }
    public User? Creator { get; set; }
}