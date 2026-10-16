namespace ERP.Domain.Entities.Common;

/// <summary>
/// How to read a setting's stored text. Settings are kept as strings so one table holds them
/// all; this is what says whether "1" means a number, a flag or a piece of text.
/// </summary>
public enum SettingValueType
{
    Text = 0,
    Number = 1,
    Boolean = 2,
    Date = 3,
    Json = 4
}
