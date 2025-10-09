namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Підтримувані мови системи
/// </summary>
public enum Language
{
    Ukrainian = 1,
    English = 2
}

/// <summary>
/// Методи розширення для Language enum
/// </summary>
public static class LanguageExtensions
{
    public static string GetDisplayName(this Language language)
    {
        return language switch
        {
            Language.Ukrainian => "Українська",
            Language.English => "English",
            _ => "Українська"
        };
    }

    public static string GetCode(this Language language)
    {
        return language switch
        {
            Language.Ukrainian => "uk",
            Language.English => "en",
            _ => "uk"
        };
    }

    public static string GetFlag(this Language language)
    {
        return language switch
        {
            Language.Ukrainian => "🇺🇦",
            Language.English => "🇺🇸",
            _ => "🇺🇦"
        };
    }

    public static Language FromCode(string code)
    {
        return code?.ToLower() switch
        {
            "en" => Language.English,
            "uk" => Language.Ukrainian,
            _ => Language.Ukrainian
        };
    }
}