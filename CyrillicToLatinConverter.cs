using System;
using System.Collections.Generic;
using System.Text;

public static class CyrillicToLatinConverter
{
    private static readonly Dictionary<char, string> UzbekCyrillicToLatinMap = new()
    {
        { 'А', "A" }, { 'Б', "B" }, { 'В', "V" }, { 'Г', "G" }, { 'Д', "D" },
        { 'Е', "E" }, { 'Ё', "Yo" }, { 'Ж', "J" }, { 'З', "Z" }, { 'И', "I" },
        { 'Й', "Y" }, { 'К', "K" }, { 'Қ', "Q" }, { 'Л', "L" }, { 'М', "M" },
        { 'Н', "N" }, { 'О', "O" }, { 'П', "P" }, { 'Р', "R" }, { 'С', "S" },
        { 'Т', "T" }, { 'У', "U" }, { 'Ў', "O‘" }, { 'Ф', "F" }, { 'Х', "X" },
        { 'Ц', "S" }, { 'Ч', "Ch" }, { 'Ш', "Sh" }, { 'Щ', "Sh" }, { 'Ъ', "" },
        { 'Ы', "I" }, { 'Ь', "" }, { 'Э', "E" }, { 'Ю', "Yu" }, { 'Я', "Ya" },
        { 'а', "a" }, { 'б', "b" }, { 'в', "v" }, { 'г', "g" }, { 'д', "d" },
        { 'е', "e" }, { 'ё', "yo" }, { 'ж', "j" }, { 'з', "z" }, { 'и', "i" },
        { 'й', "y" }, { 'к', "k" }, { 'қ', "q" }, { 'л', "l" }, { 'м', "m" },
        { 'н', "n" }, { 'о', "o" }, { 'п', "p" }, { 'р', "r" }, { 'с', "s" },
        { 'т', "t" }, { 'у', "u" }, { 'ў', "o‘" }, { 'ф', "f" }, { 'х', "x" },
        { 'ц', "s" }, { 'ч', "ch" }, { 'ш', "sh" }, { 'щ', "sh" }, { 'ъ', "" },
        { 'ы', "i" }, { 'ь', "" }, { 'э', "e" }, { 'ю', "yu" }, { 'я', "ya" },
        { 'Ғ', "G‘" }, { 'ғ', "g‘" }, { 'Ҳ', "H" }, { 'ҳ', "h" }
    };

    public static string ConvertToLatin(string cyrillicText)
    {
        if (string.IsNullOrEmpty(cyrillicText))
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();

        foreach (var character in cyrillicText)
        {
            if (UzbekCyrillicToLatinMap.TryGetValue(character, out var latinEquivalent))
            {
                stringBuilder.Append(latinEquivalent);
            }
            else
            {
                stringBuilder.Append(character); // Leave non-Cyrillic characters unchanged
            }
        }

        return stringBuilder.ToString();
    }
}