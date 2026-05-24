using System.Text;

namespace FinBot.Bll.Implementation.Dialogs.Utils;

public static class TelegramExtensions
{
    private static readonly char[] SpecialChars = 
        ['_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!'];

    public static string EscapeMarkdownV2(this string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (Array.IndexOf(SpecialChars, c) >= 0)
                sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }
}