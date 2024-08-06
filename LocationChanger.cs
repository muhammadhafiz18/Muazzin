using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Models;

namespace WebAPI
{
    public class LocationChanger
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<bool> ChangingLocation(Message message, ILogger _logger, IConfiguration configuration, string CurrentCityOfUser)
        {
            string telegramBotToken = "7263708391:AAEvRUGtiUcx2F1L1L0W0sjH-unyF__6OUA";

            var telegramApiUrl = $"https://api.telegram.org/bot{telegramBotToken}/sendMessage";
            List<string> places = new List<string>
            {
                "Тошкент", "Самарқанд", "Қўқон", "Андижон", "Фарғона", "Зомин", "Наманган", "Ангрен","Арнасой", "Ашхабод", "Бекобод", "Бишкек", "Бойсун", "Булоқбоши", "Бухоро", "Бурчмулла",
                "Газли", "Ғазалкент", "Ғаллаорол", "Ғузор", "Гулистон", "Денов", "Деҳқонобод", "Дўстлик", "Душанбе", "Жалолобод",
                "Жамбул", "Жиззах", "Жомбой", "Конибодом", "Конимех", "Қарши", "Қоровулбозор", "Қоракўл", "Қўнғирот",
                "Қоракўл", "Қизилтепа", "Қўрғонтепа", "Қумқўрғон", "Марғилон", "Мингбулоқ", "Мўйноқ", "Муборак", "Навоий",
                "Нукус", "Нурота", "Олот", "Олмаота", "Олтинкўл", "Олтиариқ", "Поп", "Пахтаобод", "Риштон", "Сайрам",
                "Таллимаржон", "Тахтакўпир", "Термиз", "Томди", "Туркистон", "Туркманобод", "Тошҳовуз", "Тўрткўл", "Учқўрғон",
                "Учқудуқ", "Учтепа", "Ўғиз", "Ўсмат", "Ўш", "Урганч", "Ургут", "Узунқудуқ", "Хазорасп", "Хива", "Хонқа",
                "Хонобод", "Хўжаобод", "Хўжанд", "Чимбой", "Чимкент", "Чортоқ", "Чуст", "Шаҳрихон", "Шеробод", "Шовот", "Шуманай",
                "Янгибозор", "Зарафшон"
            };

            string currentCityOfUser = CurrentCityOfUser;

            var inlineKeyboard = CreateInlineKeyboard(places);
            var inlineKeyboardJson = JsonConvert.SerializeObject(inlineKeyboard);

            string cityNameInLatin = CyrillicToLatinConverter.ConvertToLatin(currentCityOfUser);

            var requestData = new Dictionary<string, string>
            {
                { "chat_id",  message.From.UserID },
                { "text", $"Sizning joylashuvingiz: {cityNameInLatin}\n" +
                          $"Yangi joylashuvingizni tanlang" },
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

