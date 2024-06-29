using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text;
using WebAPI.Models;

namespace WebAPI
{
    public class Sender
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<bool> SendMessageAsync(Message message, ILogger _logger, IConfiguration configuration, string? InfoAboutChangedLocation, string? Welcome, string? FeedbackSection, string? NothingIsSent, string? Statistic)
        {
            string telegramBotToken = configuration["TelegramBotToken"];

            var telegramApiUrl = $"https://api.telegram.org/bot{telegramBotToken}/sendMessage";

            string replyMessage = $"{InfoAboutChangedLocation}{Welcome}{FeedbackSection}{NothingIsSent}{Statistic}!";

            var payload = new
            {
                chat_id = message.From.UserID,
                text = replyMessage
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(telegramApiUrl, content);
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
        public async Task<bool> SendCityOfUserAsync(Message message, ILogger _logger, IConfiguration configuration)
        {
            string telegramBotToken = configuration["TelegramBotToken"];
            var telegramApiUrl = $"https://api.telegram.org/bot{telegramBotToken}/editMessageText";

            // getting current date in Gregorian calendar
            DateTime currentDate = DateTime.Today;

            // Getting components
            int year = currentDate.Year;
            int month = currentDate.Month;
            int day = currentDate.Day;

            // getting hijri date
            System.Globalization.HijriCalendar hijriCalendar = new System.Globalization.HijriCalendar();

            // Convert today's date to Hijri
            int hijriYear = hijriCalendar.GetYear(currentDate);
            int hijriMonth = hijriCalendar.GetMonth(currentDate);
            int hijriDay = hijriCalendar.GetDayOfMonth(currentDate);

            string jsonFilePath = "C:\\Users\\Muhammad\\Desktop\\PrayerTimesOfUzbekistan.json";

            // Read the JSON file as a string
            string jsonString = File.ReadAllText(jsonFilePath);

            // Parse the JSON string into a JObject
            JObject jsonObject = JObject.Parse(jsonString);

            // Build the path to access the desired element
            string path = $"$.{message.From.CityOfUser.First().ToString().ToUpper() + message.From.CityOfUser.Substring(1).ToLower()}[{month - 1}].monthData[{day - 1}]";

            // Select the token based on the path
            JToken value = jsonObject.SelectToken(path);
            var prayerTimes = new PrayerTimes();

            prayerTimes = new PrayerTimes
            {
                CityName = value[0].ToString(),
                Month = new NamesOfMonth
                {
                    Hijri = value[1][0].ToString(),
                    Qamari = value[1][1].ToString()
                },
                DayInHijri = value[2].ToString(),
                DayInQamari = value[3].ToString(),
                DayOfWeek = value[4].ToString(),
                Fajr = value[5].ToString(),
                Sunrise = value[6].ToString(),
                Zuhr = value[7].ToString(),
                Asr = value[8].ToString(),
                Magrib = value[9].ToString(),
                Isha = value[10].ToString(),
            };

            var payload = new
            {
                chat_id = message.From.UserID,
                message_id = message.MessageId,
                text = $"Bugun: {prayerTimes.DayInQamari}/{prayerTimes.Month.Qamari}/{year}\n" +
                       $"Hijriy: {prayerTimes.DayInHijri}/{prayerTimes.Month.Hijri}/{hijriYear}\n\n" +
                       $"{prayerTimes.CityName} namoz vaqtlari:\n\n" +
                       $"🏙 Bomdod: {prayerTimes.Fajr}\n" +
                       $"🌅 Quyosh: {prayerTimes.Sunrise}\n" +
                       $"🏞 Peshin: {prayerTimes.Zuhr}\n" +
                       $"🌆 Asr: {prayerTimes.Asr}\n" +
                       $"🌉 Shom: {prayerTimes.Magrib}\n" +
                       $"🌃 Xufton: {prayerTimes.Isha}"
,
                reply_markup = new { inline_keyboard = new object[0] } // Empty inline keyboard to disable it
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(telegramApiUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to edit message. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred in sendcityofuser while editing message: {ex.Message}");
                return false;
            }
        }
    }
}


