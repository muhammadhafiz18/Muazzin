using Newtonsoft.Json;

public class InlineKeyboardButton
{
    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("callback_data")]
    public string CallbackData { get; set; }
}
