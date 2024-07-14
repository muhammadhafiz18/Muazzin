using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WebAPI
{
    public class MessageSender
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        private static readonly HttpClient _httpClient = new HttpClient();
        public MessageSender(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

        }

        public async Task<bool> SendMessageAsync(long userId, string messageText, string token)
        {
            string telegramBotToken = token;

            var url = $"https://api.telegram.org/bot{telegramBotToken}/sendMessage";
            var payload = new
            {
                chat_id = userId,
                text = messageText
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
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
