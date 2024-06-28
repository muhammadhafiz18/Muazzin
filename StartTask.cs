using Newtonsoft.Json;
using WebAPI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Functions.Worker.Http;

namespace WebAPI
{
    public class StartTask
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<bool> ChoosingCityNameAsync(Message message, ILogger _logger, IConfiguration configuration, HttpRequestData req)
        {
            string telegramBotToken = configuration["TelegramBotToken"];
            List<string> places = new List<string>
            {
                "Ангрен", "Андижон", "Арнасой", "Ашхабод", "Бекобод", "Бишкек", "Бойсун", "Булоқбоши", "Бухоро", "Бурчмулла",
                "Газли", "Ғазалкент", "Ғаллаорол", "Ғузор", "Гулистон", "Денов", "Деҳқонобод", "Дўстлик", "Душанбе", "Жалолобод",
                "Жамбул", "Жиззах", "Жомбой", "Конибодом", "Конимех", "Қарши", "Қоровулбозор", "Қоракўл", "Қўнғирот", "Қўқон",
                "Қоракўл", "Қизилтепа", "Қўрғонтепа", "Қумқўрғон", "Марғилон", "Мингбулоқ", "Мўйноқ", "Муборак", "Навоий", "Наманган",
                "Нукус", "Нурота", "Олот", "Олмаота", "Олтинкўл", "Олтиариқ", "Поп", "Пахтаобод", "Риштон", "Самарқанд", "Сайрам",
                "Таллимаржон", "Тахтакўпир", "Тошкент", "Термиз", "Томди", "Туркистон", "Туркманобод", "Тошҳовуз", "Тўрткўл", "Учқўрғон",
                "Учқудуқ", "Учтепа", "Ўғиз", "Ўсмат", "Ўш", "Урганч", "Ургут", "Узунқудуқ", "Фарғона", "Хазорасп", "Хива", "Хонқа",
                "Хонобод", "Хўжаобод", "Хўжанд", "Чимбой", "Чимкент", "Чортоқ", "Чуст", "Шаҳрихон", "Шеробод", "Шовот", "Шуманай",
                "Янгибозор", "Зарафшон", "Зомин"
            };

            var inlineKeyboard = CreateInlineKeyboard(places);
            var inlineKeyboardJson = JsonConvert.SerializeObject(inlineKeyboard);

            var requestData = new Dictionary<string, string>
            {
                { "chat_id",  message.From.UserID },
                { "text", "Assalomu alaykum. Ushbu 'Muazzin' boti har namoz vaqtida sizga havar yuboradi. Iltimos joylashuvingizni tanlang: " },
                { "reply_markup", inlineKeyboardJson }
            };

            var requestUrl = $"https://api.telegram.org/bot{telegramBotToken}/sendMessage";
            var content = new FormUrlEncodedContent(requestData);

            try
            {
                var response = await _httpClient.PostAsync(requestUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to send message. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred while sending message: {ex.Message}");
                return false;
            }
        }

        static InlineKeyboardMarkup CreateInlineKeyboard(List<string> cities)
        {
            const int buttonsPerRow = 3; // Adjust as needed
            var inlineKeyboard = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < cities.Count; i += buttonsPerRow)
            {
                var row = cities.Skip(i).Take(buttonsPerRow)
                                .Select(city => new InlineKeyboardButton { Text = city, CallbackData = city })
                                .ToList();

                inlineKeyboard.Add(row);
            }

            var inlineKeyboardArray = inlineKeyboard.Select(row => row.ToArray()).ToArray();

            return new InlineKeyboardMarkup(inlineKeyboardArray);
        }
    }
}
