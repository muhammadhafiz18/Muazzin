using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using WebAPI.Models;

namespace WebAPI
{
    public class Sender
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<bool> SendMessageAsync(Message message, ILogger _logger, IConfiguration configuration)
        {
            string telegramBotToken = configuration["TelegramBotToken"];

            var telegramApiUrl = $"https://api.telegram.org/bot{telegramBotToken}/sendMessage";

            string replyMessage = $"Ho'sh, '{message.Text}' bu nima digani endi ???";

            var payload = new
            {
                chat_id = message.Chat.Id,
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

            var payload = new
            {
                chat_id = message.Chat.Id,
                message_id = message.MessageId,
                text = $"Your city is {message.From.CityOfUser}",
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


