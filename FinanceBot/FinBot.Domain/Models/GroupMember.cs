using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class GroupMember : IBusinessEntity<int>
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public Role Role { get; set; }
}