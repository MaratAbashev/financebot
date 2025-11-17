namespace FinBot.Domain.Models;

public class GroupSetting
{
    public class UserSetting
    {
        public Guid UserId { get; set; }
        public int? Weight { get; set; }
        public decimal? Amount { get; set; }
    }
    public List<UserSetting> UserSettings { get; set; } = [];
}
