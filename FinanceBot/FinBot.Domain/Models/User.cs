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
    public required string DisplayName { get; set; }
}