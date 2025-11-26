namespace FinBot.Domain.Attributes;

public class SlashCommandAttribute(string command): Attribute
{
    public string Command { get; } = command;
}