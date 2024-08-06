/*using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WebAPI;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;
using System;
using Newtonsoft.Json;
using System.Text;

public class TelegramListener
{
    private readonly ILogger<TelegramListener> _logger;
    private readonly IConfiguration _configuration;
    private static readonly HttpClient _httpClient = new HttpClient();

    public TelegramListener(ILogger<TelegramListener> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function("Function1")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var (message, errorResponse) = await Utils.BuildMessageAsync(req);

        if (errorResponse != null)
        {
            _logger.LogError("Error in building message");
            return errorResponse;
        }
        var userDetails = new UserDetails();
        List<Chat> userDetailsFromJson = await userDetails.UserDetailGetter();

        var mainKeyboard = KeyboardBuilder.GetMainKeyboard();
        var currentUser = userDetailsFromJson.FirstOrDefault(c => c.UserID == message.From.UserID);
        bool success;
        if (message.From.CityOfUser != null && message.From.CityOfUser == "Ru" || message.From.CityOfUser == "Uz" && currentUser.CityOfUser == null)
        {
            string userIdToEdit = message.From.UserID;
            string language = message.From.CityOfUser;

            var chatToEdit = userDetailsFromJson.FirstOrDefault(c => c.UserID == userIdToEdit);
            await userDetails.UserDetailRemover(chatToEdit);

            chatToEdit.Language = language;
            await userDetails.UserDetailAdder(chatToEdit);
            var editMessageApiUrl = $"https://api.telegram.org/bot7263708391:AAEvRUGtiUcx2F1L1L0W0sjH-unyF__6OUA/deleteMessage";

            var editPayload = new
            {
                chat_id = message.From.UserID,
                message_id = message.MessageId
            };
            var editContent = new StringContent(JsonConvert.SerializeObject(editPayload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(editMessageApiUrl, editContent);

            var messageSender = new MessageSender(_logger, _configuration);
            success = await messageSender.SendMessageAsync(long.Parse(message.From.UserID), $"Til/Язык: {language}", "7263708391:AAEvRUGtiUcx2F1L1L0W0sjH-unyF__6OUA");

            await messageSender.SendMessageAsync(long.Parse(message.From.UserID), "Here the locations will be appeared", "7263708391:AAEvRUGtiUcx2F1L1L0W0sjH-unyF__6OUA");
            await userDetails.UserDetailAdder(message.From);
        }
        else if (message.Text == "/start")
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"'/start' is sent by {message.From.UserID} ({message.From.FirstName} {message.From.LastName}, ({message.From.UserName}))");
            _logger.LogInformation("------------------------------------------------------------------------------");
            bool userExists = userDetailsFromJson.Any(c => c.UserID == message.From.UserID);
            if (userExists == false)
            {
                var languageChooser = new LanguageChooser();
                success = await languageChooser.LanguageChooserAsync(message, _logger, _configuration, long.Parse(message.From.UserID));

            }
            else
            {
                var sender = new Sender();
                success = await sender.SendMessageAsync(message, _logger, _configuration, null, "Xush kelibsiz", null, null, mainKeyboard);
            }
        }
        else if (message.From.CityOfUser != null && message.From.CityOfUser.StartsWith("page_"))
        {
            var parts = message.From.CityOfUser.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[1], out int page))
            {
                var startTask = new StartTask();
                success = await startTask.ChoosingCityNameAsync(message, _logger, _configuration, page);
            }
            else
            {
                success = false;
            }
        }
        else if (message.From.CityOfUser != null)
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName})'s city is {message.From.CityOfUser}");
            _logger.LogInformation("------------------------------------------------------------------------------");
            bool hasCity = userDetailsFromJson.Any(c => c.UserID == message.From.UserID && !string.IsNullOrEmpty(c.CityOfUser));
            if (!hasCity)
            {
                string userIdToEdit = message.From.UserID;
                string cityName = message.From.CityOfUser;
                
                var chatToEdit = userDetailsFromJson.FirstOrDefault(c => c.UserID == userIdToEdit);

                await userDetails.UserDetailRemover(chatToEdit);

                chatToEdit.CityOfUser = cityName;
                await userDetails.UserDetailAdder(chatToEdit);

                var sender = new Sender();
                success = await sender.SendCityOfUserAsync(message, _logger, _configuration);
            }
            else
            {
                var sender = new Sender();

                string userIdToEdit = message.From.UserID;
                string cityName = message.From.CityOfUser;

                var chatToEdit = userDetailsFromJson.FirstOrDefault(c => c.UserID == userIdToEdit);
                await userDetails.UserDetailRemover(chatToEdit);

                chatToEdit.CityOfUser = cityName;
                await userDetails.UserDetailAdder(chatToEdit);

                success = await sender.SendCityOfUserAsync(message, _logger, _configuration);

                await sender.SendMessageAsync(message, _logger, _configuration, "Sizning joylashuvingiz o'zgartirildi", null, null, null, mainKeyboard);
            }
        }

        else if (message.Text == "/location" || message.Text == "📍 Joylashuvni o'zgartirish")
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName}) wants to change his/her location...");
            _logger.LogInformation("------------------------------------------------------------------------------");

            var detailOfUser = userDetailsFromJson.FirstOrDefault(c => c.UserID == message.From.UserID);

            string currentCityOfUser = detailOfUser!.CityOfUser!;

            message.MessageId = "";

            var location = new StartTask();
            success = await location.ChoosingCityNameAsync(message, _logger, _configuration, 1, currentCityOfUser);
            
        }
        else if (message.Text == "/feedback" || message.Text == "✍ Taklif va shikoyatlar uchun") {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName}) wants to send feedback, check the feedback bot...");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var sender = new Sender();
            success = await sender.SendMessageAsync(message, _logger, _configuration, null, null, "Taklif va shikoyatlaringizni @MuazzinFeedbacks_bot ga yuborishingiz mumkin", null, mainKeyboard);
        }
        else if (message.Text == "/statistic" || message.Text == "📊 Umumiy foydalanuvchilar soni")
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName}) wants to see the total number of users...");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var totalNumberOfUsers = userDetailsFromJson.Count;
            var sender = new Sender();
            success = await sender.SendMessageAsync(message, _logger, _configuration, null, null, null, $"Umumiy foydalanuvchilar soni: {totalNumberOfUsers}\n\nUshbu botni yaqinlaringizgaham ulashing", mainKeyboard);
        }
        else if (message.Text == "/language" || message.Text == "Tilni o'zgartirish" || message.Text == "изменение языка")
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName}) is changing the language");
            _logger.LogInformation("------------------------------------------------------------------------------");

            var languageChooser = new LanguageChooser();
            success = await languageChooser.LanguageChooserAsync(message, _logger, _configuration, long.Parse(message.From.UserID));
        }
        else
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.Text} is sent by {message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName})");
            _logger.LogInformation("------------------------------------------------------------------------------");
            success = true;
        }
        var response = req.CreateResponse(success ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.InternalServerError);
        return response;
    }
}
*/