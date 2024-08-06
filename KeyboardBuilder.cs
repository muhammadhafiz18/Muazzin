using Newtonsoft.Json;

public class KeyboardBuilder
{
    public static string GetMainKeyboard(string language)
    {
        if (language == "Ru")
        {
            var keyboard = new
            {
                keyboard = new[]
            {
                new[] { new { text = "📍 Изменение местоположения" }, new { text = "✍ Для отзывов и жалоб" } },
                new[] { new { text = "📊 Общее количество пользователей" }, new { text = "🇷🇺 Изменение языка" } }
            },
                resize_keyboard = true,
                one_time_keyboard = true
            };
            return JsonConvert.SerializeObject(keyboard);

        }
        else
        {
            var keyboard = new
            {
                keyboard = new[]
            {
                new[] { new { text = "📍 Joylashuvni o'zgartirish" }, new { text = "✍ Taklif va shikoyatlar uchun" } },
                new[] { new { text = "📊 Umumiy foydalanuvchilar soni" }, new { text = "🇺🇿 Tilni o'zgartirish" } }
            },
                resize_keyboard = true,
                one_time_keyboard = true
            };
            return JsonConvert.SerializeObject(keyboard);

        }

    }
}
