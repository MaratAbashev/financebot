using System.Text.RegularExpressions;

namespace FinBot.Domain.Attributes;

public class TextCommandAttribute(string textCommand, bool isRegularExpression = false): Attribute
{
    public string TextCommand { get; } = textCommand;
    public bool IsRegularExpression { get; } = isRegularExpression;
}