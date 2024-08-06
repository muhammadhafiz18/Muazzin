using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Models;

namespace WebAPI
{
    public class StartTask
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const int PageSize = 8; // Number of cities per page

        public async Task<bool> ChoosingCityNameAsync(Message message, ILogger _logger, IConfiguration configuration, string language, int page = 1, string currentCityOfUser = "")
        {
            string telegramBotToken = "7263708391:AAEvRUGtiUcx2F1L1L0W0sjH-unyF__6OUA";
            var telegramApiUrl = $"https://api.telegram.org/bot{telegramBotToken}/sendMessage";
            var editMessageApiUrl = $"https://api.telegram.org/bot{telegramBotToken}/editMessageReplyMarkup";

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

            var totalPages = (places.Count + PageSize - 1) / PageSize; // Calculate total pages

            var startIndex = (page - 1) * PageSize;
            var endIndex = startIndex + PageSize;
            var citiesPage = places.Skip(startIndex).Take(PageSize).ToList();

            var inlineKeyboard = GenerateInlineKeyboard(citiesPage, page, totalPages);
            var payload = new
            {
                chat_id = "",
                text = "",
                reply_markup = inlineKeyboard
            };
            if (language == "Ru")
            {
                payload = new
                {
                    chat_id = message.From.UserID,
                    text = "Ассаляму алейкум. Этот бот предупреждает вас о времени молитвы.\nПожалуйста, выберите ваше местоположение:",
                    reply_markup = inlineKeyboard
                };
            } 
            else
            {
                payload = new
                {
                    chat_id = message.From.UserID,
                    text = "Assalomu alaykum. Ushbu 'Muazzin' boti har namoz vaqtida sizga xabar yuboradi\nIltimos joylashuvingizni tanlang:",
                    reply_markup = inlineKeyboard
                };
            }

            if (language == "Ru")
            {
                if (currentCityOfUser != "")
                {
                    payload = new
                    {
                        chat_id = message.From.UserID,
                        text =
                        $"Ваше местоположение: {currentCityOfUser}\n" +
                        $"Пожалуйста, выберите новое местоположение:",
                        reply_markup = inlineKeyboard
                    };
                }
            }
            else
            {
                if (currentCityOfUser != "")
                {
                    string cityNameInLatin = CyrillicToLatinConverter.ConvertToLatin(currentCityOfUser);
                    payload = new
                    {
                        chat_id = message.From.UserID,
                        text =
                        $"Sizning joylashuvingiz: {cityNameInLatin}\n" +
                        $"Yangi joylashuvingizni tanlang",
                        reply_markup = inlineKeyboard
                    };
                }
            }

            


            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response;
                if (message.MessageId != "")
                {
                    // Edit existing message
                    var editPayload = new
                    {
                        chat_id = message.From.UserID,
                        message_id = message.MessageId,
                        reply_markup = inlineKeyboard
                    };
                    var editContent = new StringContent(JsonConvert.SerializeObject(editPayload), Encoding.UTF8, "application/json");
                    response = await _httpClient.PostAsync(editMessageApiUrl, editContent);
                }
                else
                {
                    // Send new message
                    response = await _httpClient.PostAsync(telegramApiUrl, content);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to send inline keyboard. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred while sending inline keyboard: {ex.Message}");
                return false;
            }
        }

        private object GenerateInlineKeyboard(List<string> citiesPage, int currentPage, int totalPages)
        {
            // Generate city buttons in 3 columns
            var cityButtons = new List<object[]>();
            for (int i = 0; i < citiesPage.Count; i += 3)
            {
                var row = new List<object>();
                for (int j = 0; j < 3 && (i + j) < citiesPage.Count; j++)
                {
                    row.Add(new { text = citiesPage[i + j], callback_data = $"{citiesPage[i + j]}" });
                }
                cityButtons.Add(row.ToArray());
            }

            // Navigation buttons
            var navigationButtons = new[]
                    {
                new { text = "<", callback_data = $"page_{currentPage - 1}" },
                new { text = ">", callback_data = $"page_{currentPage + 1}" }
            };

            var inlineKeyboard = new
            {
                inline_keyboard = cityButtons.Concat(new[] { navigationButtons }).ToArray()
            };

            return inlineKeyboard;
        }

    }
}
