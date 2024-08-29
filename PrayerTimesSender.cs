using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Globalization;
using WebAPI.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebAPI
{
    public class PrayerTimesSender
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string blobConnectionString = "YourBlobConnectionStringHere";
        private readonly string containerName = "picsofprayers";
        private readonly IConfiguration _configuration;
        private readonly ILogger<PrayerTimesSender> _logger;

        public PrayerTimesSender(ILogger<PrayerTimesSender> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _blobServiceClient = new BlobServiceClient(blobConnectionString);
        }

        [Function("PrayerTimesSender")]
        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer)
        {

            string mainLogoPictureUrl = await GetBlobUrlAsync(containerName, "Muazzin Main Logo.jpg");
            string mainLogoPictureUrlRu = await GetBlobUrlAsync(containerName, "Muazzin Main Logo Ru.jpg");

            var userDetails = new UserDetails();
            var allUsers = await userDetails.UserDetailGetter();
            
            if (allUsers.Count == 0)
            {
                _logger.LogInformation("No users found.");
                return;
            }

            string timeZoneId = "Central Asia Standard Time";

            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            // Get the current UTC time
            DateTime utcNow = DateTime.UtcNow;

            // Convert UTC time to local time in Uzbekistan
            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
            DateTime oneHourBefore = localTime.AddHours(-1);
            string timeFormat = "HH:mm:ss";
            string formattedTime = oneHourBefore.ToString(timeFormat);
            string lastTwoChars = formattedTime.Length >= 2 ? formattedTime.Substring(formattedTime.Length - 2) : formattedTime;
            if (lastTwoChars == "59")
            {
                DateTime oneSecondBefore = utcNow.AddSeconds(+1);
                formattedTime = oneSecondBefore.ToString(timeFormat);
            }

            _logger.LogInformation($"now it is {formattedTime} in Tashkent");
 
            // Display the current date, month, and year in Uzbekistan
            int day = localTime.Day;
            int month = localTime.Month;

            HijriCalendar hijriCalendar = new HijriCalendar();
            int hijriDay = hijriCalendar.GetDayOfMonth(utcNow);
            int hijriMonth = hijriCalendar.GetMonth(utcNow);
            int hijriYear = hijriCalendar.GetYear(utcNow);
            var dailyPrayerTimesGetter = new DailyPrayerTimesAndUserDetailsGetter();
            var specificTimesGetter = new AllSpecificTimesGetter();
            var userDetailsFromBlob = new UserDetails();
            List<Chat> userDetailsFromJson = await userDetailsFromBlob.UserDetailGetter();

            var (dailyPrayerTimes, detailsOfUsers) = await dailyPrayerTimesGetter.GetDailyPrayerTimes(month, day, allUsers);
            if (!dailyPrayerTimes.Any() || !detailsOfUsers.Any())
            {
                _logger.LogInformation("Daily prayer times or user details are empty.");
                return;
            }

            var allSpecificTimes = specificTimesGetter.GetAllSpecificTimes(dailyPrayerTimes);
            var pictureSender = new PictureSender(_logger, _configuration);

            if (formattedTime == "01:00:00")
            {
                await SendDailyPrayerTimesMessageAtNight(detailsOfUsers, dailyPrayerTimes, month, day, pictureSender, mainLogoPictureUrl, userDetailsFromJson, mainLogoPictureUrlRu);
                return;
            } else if (formattedTime == "13:00:00")
            {
                await SendDailyPrayerTimesMessageAtNoon(detailsOfUsers, dailyPrayerTimes, month, day, hijriDay, hijriMonth, hijriYear, pictureSender, mainLogoPictureUrl, userDetailsFromJson, mainLogoPictureUrlRu);
                return;
            }

            await CheckAndSendSpecificTimeNotifications(formattedTime, allSpecificTimes, dailyPrayerTimes, detailsOfUsers, pictureSender, userDetailsFromJson);
        }

        private async Task SendDailyPrayerTimesMessageAtNight(Dictionary<string, long[]> detailsOfUsers, List<JToken> dailyPrayerTimes, int month, int day, PictureSender pictureSender, string photoUrl, List<Chat> userDetailsFromBLob, string photoUrlRu)
        {
            foreach (var kvp in detailsOfUsers)
            {
                string city = kvp.Key;
                long[] userIds = kvp.Value;
                var dailyPrayerTime = dailyPrayerTimes.FirstOrDefault(d => d[0]?.ToString() == city);
                if (dailyPrayerTime == null) continue;

                string cityNameInLatin = CyrillicToLatinConverter.ConvertToLatin(dailyPrayerTime[0].ToString());
                foreach (var userId in userIds)
                {
                    string language = userDetailsFromBLob
                                .Where(u => long.Parse(u.UserID) == userId)
                                .Select(u => u.Language)
                                .FirstOrDefault();

                    bool success;
                    if (language == "Ru")
                    {
                        var message = new StringBuilder();
                        message.AppendLine($"Ассаляму алейкум. Сегодняшнее время молитвы {dailyPrayerTime[0].ToString()}а\n");
                        message.AppendLine($"Сегодня: {day}.{month}.2024\n");
                        message.AppendLine($"Время молитвы:");
                        message.AppendLine($"🏙 Фаджр: {dailyPrayerTime[5]}");
                        message.AppendLine($"🌅 Шурук: {dailyPrayerTime[6]}");
                        message.AppendLine($"🏞 Зухр: {dailyPrayerTime[7]}");
                        message.AppendLine($"🌆 Аср: {dailyPrayerTime[8]}");
                        message.AppendLine($"🌉 Магриб: {dailyPrayerTime[9]}");
                        message.AppendLine($"🌃 Иша: {dailyPrayerTime[10]}\n\n@MuazzinUz_bot");
                        success = await pictureSender.SendPictureAsync(userId, photoUrlRu, message.ToString(), "YourBotTokenHere");
                        _logger.LogInformation(success ? $"Daily prayer times list sent successfully to {userId}" : $"Message wasn't sent to {userId}");
                    } 
                    else
                    {
                        var message = new StringBuilder();
                        message.AppendLine($"Assalomu alaykum. {cityNameInLatin}da bugungi namoz vaqtlari\n");
                        message.AppendLine($"Bugun: {day}.{month}.2024\n");
                        message.AppendLine($"Namoz vaqtlari:");
                        message.AppendLine($"🏙 Bomdod: {dailyPrayerTime[5]}");
                        message.AppendLine($"🌅 Quyosh: {dailyPrayerTime[6]}");
                        message.AppendLine($"🏞 Peshin: {dailyPrayerTime[7]}");
                        message.AppendLine($"🌆 Asr: {dailyPrayerTime[8]}");
                        message.AppendLine($"🌉 Shom: {dailyPrayerTime[9]}");
                        message.AppendLine($"🌃 Xufton: {dailyPrayerTime[10]}\n\n@MuazzinUz_bot");
                        success = await pictureSender.SendPictureAsync(userId, photoUrl, message.ToString(), "YourBotTokenHere");
                        _logger.LogInformation(success ? $"Daily prayer times list sent successfully to {userId}" : $"Message wasn't sent to {userId}");

                    }
                }
            }
        }

        private async Task SendDailyPrayerTimesMessageAtNoon(Dictionary<string, long[]> detailsOfUsers, List<JToken> dailyPrayerTimes, int month, int day, int hijriDay, int hijriMonth, int hijriYear, PictureSender pictureSender, string photoUrl, List<Chat> userDetailsFromBLob, string photoUrlRu)
        {

            foreach (var kvp in detailsOfUsers)
            {
                string city = kvp.Key;
                long[] userIds = kvp.Value;
                var dailyPrayerTime = dailyPrayerTimes.FirstOrDefault(d => d[0]?.ToString() == city);
                if (dailyPrayerTime == null) continue;

                string cityNameInLatin = CyrillicToLatinConverter.ConvertToLatin(dailyPrayerTime[0].ToString());
                foreach (var userId in userIds)
                {
                    string language = userDetailsFromBLob
                                .Where(u => long.Parse(u.UserID) == userId)
                                .Select(u => u.Language)
                                .FirstOrDefault();

                    bool success;
                    if (language == "Ru")
                    {
                        var message = new StringBuilder();
                        message.AppendLine($"Ассаляму алейкум. Сегодняшнее время молитвы {dailyPrayerTime[0].ToString()}а\n");
                        message.AppendLine($"Сегодня: {day}.{month}.2024\n");
                        message.AppendLine($"Время молитвы:");
                        message.AppendLine($"🏙 Фаджр: {dailyPrayerTime[5]}");
                        message.AppendLine($"🌅 Шурук: {dailyPrayerTime[6]}");
                        message.AppendLine($"🏞 Зухр: {dailyPrayerTime[7]}");
                        message.AppendLine($"🌆 Аср: {dailyPrayerTime[8]}");
                        message.AppendLine($"🌉 Магриб: {dailyPrayerTime[9]}");
                        message.AppendLine($"🌃 Иша: {dailyPrayerTime[10]}\n\n@MuazzinUz_bot");
                        success = await pictureSender.SendPictureAsync(userId, photoUrlRu, message.ToString(), "YourBotTokenHere");
                        _logger.LogInformation(success ? $"Daily prayer times list sent successfully to {userId}" : $"Message wasn't sent to {userId}");
                    }
                    else
                    {
                        var message = new StringBuilder();
                        message.AppendLine($"Assalomu alaykum. {cityNameInLatin}da bugungi namoz vaqtlari\n");
                        message.AppendLine($"Bugun: {day}.{month}.2024\n");
                        message.AppendLine($"Namoz vaqtlari:");
                        message.AppendLine($"🏙 Bomdod: {dailyPrayerTime[5]}");
                        message.AppendLine($"🌅 Quyosh: {dailyPrayerTime[6]}");
                        message.AppendLine($"🏞 Peshin: {dailyPrayerTime[7]}");
                        message.AppendLine($"🌆 Asr: {dailyPrayerTime[8]}");
                        message.AppendLine($"🌉 Shom: {dailyPrayerTime[9]}");
                        message.AppendLine($"🌃 Xufton: {dailyPrayerTime[10]}\n\n@MuazzinUz_bot");
                        success = await pictureSender.SendPictureAsync(userId, photoUrl, message.ToString(), "YourBotTokenHere");
                        _logger.LogInformation(success ? $"Daily prayer times list sent successfully to {userId}" : $"Message wasn't sent to {userId}");

                    }
                }
            }
        }

        private async Task CheckAndSendSpecificTimeNotifications(string formattedTime, (int hour, int minute)[] allSpecificTimes, List<JToken> dailyPrayerTimes, Dictionary<string, long[]> detailsOfUsers, PictureSender pictureSender, List<Chat> userDetailsFromBLob)
        {
            string bomdodPictureUrl = await GetBlobUrlAsync(containerName, "Muazzin_bomdod.jpg");
            string quyoshPictureUrl = await GetBlobUrlAsync(containerName, "Muazzin_quyosh.jpg");
            string peshinPictureUrl = await GetBlobUrlAsync(containerName, "Muazzin_peshin.jpg");
            string asrPictureUrl = await GetBlobUrlAsync(containerName, "Muazzin_asr.jpg");
            string shomPictureUrl = await GetBlobUrlAsync(containerName, "Muazzin_shom.jpg");
            string xuftonPictureUrl = await GetBlobUrlAsync(containerName, "Muazzin_xufton.jpg");

            string bomdodPictureUrlRu = await GetBlobUrlAsync(containerName, "Muazzin_bomdod_ru.jpg");
            string quyoshPictureUrlRu = await GetBlobUrlAsync(containerName, "Muazzin_quyosh_ru.jpg");
            string peshinPictureUrlRu = await GetBlobUrlAsync(containerName, "Muazzin_peshin_ru.jpg");
            string asrPictureUrlRu = await GetBlobUrlAsync(containerName, "Muazzin_asr_ru.jpg");
            string shomPictureUrlRu = await GetBlobUrlAsync(containerName, "Muazzin_shom_ru.jpg");
            string xuftonPictureUrlRu = await GetBlobUrlAsync(containerName, "Muazzin_xufton_ru.jpg");
            foreach (var time in allSpecificTimes)
            {
                var specificTime = new TimeSpan(time.hour, time.minute, 0);

                if (formattedTime != specificTime.ToString(@"hh\:mm\:ss")) continue;

                foreach (var dailyPrayerTime in dailyPrayerTimes)
                {
                    string cityNameInLatin = CyrillicToLatinConverter.ConvertToLatin(dailyPrayerTime[0].ToString());
                    for (int i = 5; i <= 10; i++)
                    {
                        if (dailyPrayerTime[i]?.ToString() != specificTime.ToString(@"hh\:mm")) continue;

                        long[] userIds = detailsOfUsers[dailyPrayerTime[0].ToString()];
                        string message = i switch
                        {
                            5 => $"🏙 {cityNameInLatin}da bomdod vaqti bo'ldi\n\n@MuazzinUz_bot",
                            6 => $"🌅 {cityNameInLatin}da bomdod vaqti o'tib ketti\n\n@MuazzinUz_bot",
                            7 => $"🏞 {cityNameInLatin}da peshin vaqti bo'ldi\n\n@MuazzinUz_bot",
                            8 => $"🌆 {cityNameInLatin}da asr vaqti bo'ldi\n\n@MuazzinUz_bot",
                            9 => $"🌉 {cityNameInLatin}da shom vaqti bo'ldi\n\n@MuazzinUz_bot",
                            10 => $"🌃 {cityNameInLatin}da xufton vaqti bo'ldi\n\n@MuazzinUz_bot",
                            _ => string.Empty
                        };
                        string messageInRussian = i switch
                        {
                            5 => $"🏙 В {dailyPrayerTime[0].ToString()}е фаджр.\n\n@MuazzinUz_bot",
                            6 => $"🌅 В {dailyPrayerTime[0].ToString()}е рассвет\n\n@MuazzinUz_bot",
                            7 => $"🏞 В {dailyPrayerTime[0].ToString()}е зухр\n\n@MuazzinUz_bot",
                            8 => $"🌆 В {dailyPrayerTime[0].ToString()}е аср\n\n@MuazzinUz_bot",
                            9 => $"🌉 В {dailyPrayerTime[0].ToString()}е магриб\n\n@MuazzinUz_bot",
                            10 => $"🌃 В {dailyPrayerTime[0].ToString()}е иша\n\n@MuazzinUz_bot",
                            _ => string.Empty
                        };

                        string photoUrl;

                        if (string.IsNullOrEmpty(message)) continue;
                        bool success;
                        foreach (var userId in userIds)
                        {
                            string language = userDetailsFromBLob
                                .Where(u => long.Parse(u.UserID) == userId)
                                .Select(u => u.Language)
                                .FirstOrDefault();

                            if (language == "Ru")
                            {
                                if (i == 5)
                                {
                                    photoUrl = bomdodPictureUrlRu;
                                }
                                else if (i == 6)
                                {
                                    photoUrl = quyoshPictureUrlRu;
                                }
                                else if (i == 7)
                                {
                                    photoUrl = peshinPictureUrlRu;
                                }
                                else if (i == 8)
                                {
                                    photoUrl = asrPictureUrlRu;
                                }
                                else if (i == 9)
                                {
                                    photoUrl = shomPictureUrlRu;
                                }
                                else
                                {
                                    photoUrl = xuftonPictureUrlRu;
                                }
                                success = await pictureSender.SendPictureAsync(userId, photoUrl, messageInRussian, "YourBotTokenHere");
                                _logger.LogInformation(success ? $"Notification sent successfully to {userId} [{i}]" : $"Notification wasn't sent to {userId} [{i}] Russain");

                            } 
                            else
                            {
                                if (i == 5)
                                {
                                    photoUrl = bomdodPictureUrl;
                                }
                                else if (i == 6)
                                {
                                    photoUrl = quyoshPictureUrl;
                                }
                                else if (i == 7)
                                {
                                    photoUrl = peshinPictureUrl;
                                }
                                else if (i == 8)
                                {
                                    photoUrl = asrPictureUrl;
                                }
                                else if (i == 9)
                                {
                                    photoUrl = shomPictureUrl;
                                }
                                else
                                {
                                    photoUrl = xuftonPictureUrl;
                                }
                                success = await pictureSender.SendPictureAsync(userId, photoUrl, message, "YourBotTokenHere");
                                _logger.LogInformation(success ? $"Notification sent successfully to {userId} [{i}]" : $"Notification wasn't sent to {userId} [{i}] Uzbek");

                            }
                        }

                        _logger.LogInformation($"Notifications sent for {dailyPrayerTime[0]}");
                        return;
                    }
                }
            }
        }
        private async Task<string> GetBlobUrlAsync(string containerName, string blobName)
        {
            try
            {

                // Create a BlobServiceClient
                BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);

                // Get a reference to the container
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                // Get a reference to the blob
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                // Generate the URI for the blob
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving blob URL: {ex.Message}");
                return null;
            }
        }

    }
}
