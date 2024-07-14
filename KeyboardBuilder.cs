using Newtonsoft.Json;

public class KeyboardBuilder
{
    public static string GetMainKeyboard()
    {
        var keyboard = new
        {
            keyboard = new[]
            {
                new[] { new { text = "📍 Joylashuvni o'zgartirish" }, new { text = "✍ Taklif va shikoyatlar uchun" } },
                new[] { new { text = "📊 Umumiy foydalanuvchilar soni" } }
            },
            resize_keyboard = true,
            one_time_keyboard = true
        };

        return JsonConvert.SerializeObject(keyboard);
    }
}
