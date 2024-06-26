using Newtonsoft.Json;

public class InlineKeyboardMarkup
{
    [JsonProperty("inline_keyboard")]
    public InlineKeyboardButton[][] InlineKeyboard { get; set; }

    public InlineKeyboardMarkup(InlineKeyboardButton[][] inlineKeyboard)
    {
        InlineKeyboard = inlineKeyboard;
    }
}