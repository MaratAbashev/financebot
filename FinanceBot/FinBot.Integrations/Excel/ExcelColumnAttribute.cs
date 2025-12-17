namespace FinBot.Integrations.Excel;

[AttributeUsage(AttributeTargets.Property)]
public class ExcelColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}