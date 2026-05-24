using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class JoinRequest : IBusinessEntity<int>
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid GroupId { get; set; }
    public Group? Group { get; set; }
}
