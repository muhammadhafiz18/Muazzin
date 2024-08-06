using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Models;

namespace WebAPI
{
    public class MuazzinStatistic
    {
        private readonly ILogger<MuazzinStatistic> _logger;
        private readonly IConfiguration _configuration;

        public MuazzinStatistic(ILogger<MuazzinStatistic> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Function("MuazzinStatistic")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            string requestBody;
            using (StreamReader reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }
            var userDetail = new UserDetails();

            var allUsers = await userDetail.UserDetailGetter();

            dynamic payload = JsonConvert.DeserializeObject(requestBody);

            if (payload?.message == null && payload?.callback_query == null)
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid payload" });
                _logger.LogInformation("Invalid payload...");
                return badResponse;
            }

            var message = new Message
            {
                MessageId = payload.message.message_id,
                Text = payload.message.text,
                From = new Chat
                {
                    UserID = payload.message.from.id,
                    FirstName = payload.message.from.first_name,
                    LastName = payload.message.from.last_name,
                    UserName = payload.message.from.username,
                },
            };

            var sender = new MessageSender(_logger, _configuration);
            bool success;
            if (message.Text == "/viewstatistic" || message.Text == "/start")
            {
                _logger.LogInformation("------------------------------------------------------------------------------");
                _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName}) wants to see the statistics");
                _logger.LogInformation("------------------------------------------------------------------------------");

                try
                {
                    var messageToSend = new StringBuilder();

                    int i = 0;

                    foreach (var user in allUsers)
                    {
                        i++;
                        messageToSend.AppendLine($"{i}. First name: {user.FirstName}");
                        messageToSend.AppendLine($"Last name: {user.LastName}");
                        messageToSend.AppendLine($"Username: {user.UserName}");
                        messageToSend.AppendLine($"UserId: {user.UserID}");
                        messageToSend.AppendLine($"User's city: {user.CallBackQuery}");
                        messageToSend.AppendLine($"-----------------------------");
                    }

                    success = await sender.SendMessageAsync(84285004, messageToSend.ToString(), "7337862823:AAH-ZOhYGJBU0uJOoX9BSl6AkBVp1RjMwYA");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while fetching statistics or sending the message.");
                    success = false;
                }
            }
            else
            {
                    success = await sender.SendMessageAsync(long.Parse(message.From.UserID), message.Text, "7337862823:AAH-ZOhYGJBU0uJOoX9BSl6AkBVp1RjMwYA");
            }

            var response = req.CreateResponse(success ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.InternalServerError);
            return response;
        }
    }
}
