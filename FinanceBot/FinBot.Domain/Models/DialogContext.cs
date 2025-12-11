using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

public class DialogContext: IBusinessEntity<int>
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public string DialogName { get; set; }
    public int CurrentStep { get; set; }
    public Dictionary<string, object>? DialogStorage { get; set; }
}