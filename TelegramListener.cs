using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WebAPI;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;
using System;

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
        bool success;
        if (message.Text == "/start")
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"'/start' is sent by {message.From.UserID} ({message.From.FirstName} {message.From.LastName}, ({message.From.UserName}))");
            _logger.LogInformation("------------------------------------------------------------------------------");
            bool userExists = userDetailsFromJson.Any(c => c.UserID == message.From.UserID);
            if (userExists == false)
            {
                await userDetails.UserDetailAdder(message.From);
                var startTask = new StartTask();
                success = await startTask.ChoosingCityNameAsync(message, _logger, _configuration, req);
            }
            else
            {
                var sender = new Sender();
                success = await sender.SendMessageAsync(message, _logger, _configuration, null, "Xush kelibsiz", null, null, mainKeyboard);
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

            var location = new LocationChanger();
            success = await location.ChangingLocation(message, _logger, _configuration, currentCityOfUser);
            
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
