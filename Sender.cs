using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using WebAPI.Models;

namespace WebAPI
{
    public class Sender
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        

        public async Task<bool> SendMessageAsync(Message message, ILogger _logger, IConfiguration configuration, string? InfoAboutChangedLocation, string? Welcome, string? FeedbackSection, string? Statistic, string? replyMarkup = null)
        {
            string telegramBotToken = "7263708391:AAEvRUGtiUcx2F1L1L0W0sjH-unyF__6OUA";


            var telegramApiUrl = $"https://api.telegram.org/bot{telegramBotToken}/sendMessage";

            string replyMessage = $"{InfoAboutChangedLocation}{Welcome}{FeedbackSection}{Statistic}";

            var payload = new
            {
                chat_id = message.From.UserID,
                text = replyMessage,
                reply_markup = replyMarkup != null ? JsonConvert.DeserializeObject(replyMarkup) : null
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
        public async Task<bool> SendCityOfUserAsync(Message message, ILogger _logger, IConfiguration configuration, string language)
        {
            string telegramBotToken = "7263708391:AAEvRUGtiUcx2F1L1L0W0sjH-unyF__6OUA";
            var telegramApiUrl = $"https://api.telegram.org/bot{telegramBotToken}/editMessageText";

            string timeZoneId = "Central Asia Standard Time";

            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            // Get the current UTC time
            DateTime utcNow = DateTime.UtcNow;

            // Convert UTC time to local time in Uzbekistan
            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);

            // Display the current date, month, and year in Uzbekistan
            int day = localTime.Day;
            int month = localTime.Month;
            int year = localTime.Year;

            string jsonUrl = "https://muazzinresources.blob.core.windows.net/timesofprayers/PrayerTimesOfUzbekistan.json.gz";

            // Fetching the compressed JSON file
            var response = await _httpClient.GetAsync(jsonUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Couldn't get the JSON, status code: {response.StatusCode}");
                
            }

            var compressedStream = await response.Content.ReadAsStreamAsync();

            using (var decompressedStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var streamReader = new StreamReader(decompressedStream))
            {
                var jsonString = await streamReader.ReadToEndAsync();

                // Parse the JSON string into a JObject
                JObject jsonObject = JObject.Parse(jsonString);

                string path = $"$.{message.From.CallBackQuery.First().ToString().ToUpper() + message.From.CallBackQuery.Substring(1).ToLower()}[{month - 1}].monthData[{day - 1}]";

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
                    chat_id = "",
                    message_id = message.MessageId,
                    text = "",
                    reply_markup = new { inline_keyboard = new object[0] } // Empty inline keyboard to disable it
                };

                if (language == "Ru")
                {
                    payload = new
                    {
                        chat_id = message.From.UserID,
                        message_id = message.MessageId,
                        text = $"Сегодня: {prayerTimes.DayInQamari}/{month}/{year}\n" +
                            $"{message.From.CallBackQuery} время молитвы:\n" +
                            $"🏙 Фаджр: {prayerTimes.Fajr}\n" +
                            $"🌅 Шурук: {prayerTimes.Sunrise}\n" +
                            $"🏞 Зухр: {prayerTimes.Zuhr}\n" +
                            $"🌆 Аср: {prayerTimes.Asr}\n" +
                            $"🌉 Магриб: {prayerTimes.Magrib}\n" +
                            $"🌃 Иша: {prayerTimes.Isha}"
    ,
                        reply_markup = new { inline_keyboard = new object[0] } // Empty inline keyboard to disable it
                    };
                }
                else
                {
                    string cityNameInLatin = CyrillicToLatinConverter.ConvertToLatin(prayerTimes.CityName);

                    payload = new
                    {
                        chat_id = message.From.UserID,
                        message_id = message.MessageId,
                        text = $"Bugun: {prayerTimes.DayInQamari}/{month}/{year}\n" +
                        $"{cityNameInLatin} namoz vaqtlari:\n" +
                        $"🏙 Bomdod: {prayerTimes.Fajr}\n" +
                        $"🌅 Quyosh: {prayerTimes.Sunrise}\n" +
                        $"🏞 Peshin: {prayerTimes.Zuhr}\n" +
                        $"🌆 Asr: {prayerTimes.Asr}\n" +
                        $"🌉 Shom: {prayerTimes.Magrib}\n" +
                        $"🌃 Xufton: {prayerTimes.Isha}"
,
                        reply_markup = new { inline_keyboard = new object[0] } // Empty inline keyboard to disable it
                    };
                }

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                try
                {
                    response = await _httpClient.PostAsync(telegramApiUrl, content);
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
}


