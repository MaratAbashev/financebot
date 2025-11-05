namespace FinBot.Domain.Models;

public class Account
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    
    public decimal Balance { get; set; }
    
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }
}