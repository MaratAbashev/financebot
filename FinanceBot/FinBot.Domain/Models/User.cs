using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

/// <summary>
/// Пользователь
/// </summary>
public class User : IBusinessEntity<Guid>
{
    /// <summary>
    /// Guid id бд
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Числовой id telegram
    /// </summary>
    public long TelegramId { get; set; }
    
    /// <summary>
    /// Отображаемое имя пользователя
    /// </summary>
    public string DisplayName { get; set; }

    // Группы, в которых юз - создатель
    public List<Group> Groups { get; set; } = [];
    // Просто счета из всех групп, в которых состоти юз
    public List<Account> Accounts { get; set; } = [];
}