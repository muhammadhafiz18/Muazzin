using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Models;

namespace WebAPI
{
    public class LanguageChooser
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        public async Task<bool> LanguageChooserAsync(Message message, ILogger _logger, IConfiguration configuration, long chatId)
        {
            string telegramBotToken = "7263708391:AAEvRUGtiUcx2F1L1L0W0sjH-unyF__6OUA";
            var telegramApiUrl = $"https://api.telegram.org/bot{telegramBotToken}/sendMessage";

            var messageData = new
            {
                chat_id = chatId,
                text = "Til/Язык:",
                reply_markup = new
                {
                    inline_keyboard = new[]
                {
                    new[]
                    {
                        new { text = "🇺🇿 O'zbekcha", callback_data = "Uz" },
                        new { text = "🇷🇺 Русский", callback_data = "Ru" }
                    }
                }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(messageData), Encoding.UTF8, "application/json");

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
