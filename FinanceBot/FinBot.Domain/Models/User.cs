using FinBot.Domain.Utils;

namespace FinBot.Domain.Models;

/// <summary>
/// Пользователь
/// </summary>
public class User : IBusinessEntity<Guid>
{
    /// <summary>
    /// Числовой id бд
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Числовой id telegram
    /// </summary>
    public int TelegramId { get; set; }
    
    /// <summary>
    /// Имя пользователя телеграм через @
    /// </summary>
    public required string Username { get; set; }
    
    /// <summary>
    /// Отображаемое имя пользователя
    /// </summary>
    public required string DisplayName { get; set; }
}