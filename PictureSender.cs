using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WebAPI
{
    public class PictureSender
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        private static readonly HttpClient _httpClient = new HttpClient();
        public PictureSender(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

        }

        public async Task<bool> SendPictureAsync(long userId, string photoUrl, string photoCaption, string token)
        {
            string telegramBotToken = token;

            var url = $"https://api.telegram.org/bot{telegramBotToken}/sendPhoto";
            var payload = new
            {
                chat_id = userId,
                photo = photoUrl,
                caption = photoCaption // Optional caption
            };

            var content = JsonContent.Create(payload);

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
