namespace FinBot.Domain.Utils;

public record ReplyKeyboardOptions
{
    public bool ResizeKeyboard { get; init; }
    public bool OneTimeKeyboard { get; init; }
    public string? InputFieldPlaceholder { get; init; }
    public bool Selective { get; init; }
    public bool IsPersistent { get; init; }
    public static readonly ReplyKeyboardOptions DefaultOptions = new()
    {
        ResizeKeyboard = true, 
        OneTimeKeyboard = false, 
        Selective = false, 
        IsPersistent = false
    };
}