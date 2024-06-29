using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WebAPI;
using WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using Microsoft.IdentityModel.Tokens;
using System.Configuration;
using System.Reflection;

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
        var context = new MyAppDbContext();
        if (errorResponse != null)
        {
            _logger.LogError("Error in building message");
            return errorResponse;
        }

        bool success;
        if (message.Text == "/start")
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"'/start' is sent by {message.From.UserID} ({message.From.FirstName} {message.From.LastName}, ({message.From.UserName}))");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var chat = await context.Chats.FirstOrDefaultAsync(c => c.UserID == message.From.UserID);
            if (chat == null)
            {
                context.Chats.Add(message.From);
                context.SaveChanges();
                var startTask = new StartTask();
                success = await startTask.ChoosingCityNameAsync(message, _logger, _configuration, req);
            }
            else
            {
                var sender = new Sender();
                success = await sender.SendMessageAsync(message, _logger, _configuration, null, "Xush kelibsiz", null, null, null);
            }   

        }
        else if (message.From.CityOfUser != null)
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName})'s city is {message.From.CityOfUser}");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var chat = await context.Chats.FirstOrDefaultAsync(c => c.UserID == message.From.UserID);
            if (chat.CityOfUser == null)
            {
                chat = await context.Chats.FirstOrDefaultAsync(c => c.UserID == message.From.UserID);
                chat.CityOfUser = message.From.CityOfUser;
                await context.SaveChangesAsync();
                var sender = new Sender();
                success = await sender.SendCityOfUserAsync(message, _logger, _configuration);
            }
            else
            {
                var sender = new Sender();
                chat = await context.Chats.FirstOrDefaultAsync(c => c.UserID == message.From.UserID);
                chat.CityOfUser = message.From.CityOfUser;
                await context.SaveChangesAsync();
                success = await sender.SendCityOfUserAsync(message, _logger, _configuration);
                await sender.SendMessageAsync(message, _logger, _configuration, "Sizning joylashuvingiz o'zgartirildi", null, null, null, null);
            }
        }
        else if (message.Text == "/location")
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName}) wants to change his/her location...");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var currentCityOfUser = await context.Chats
                                 .Where(c => c.UserID == message.From.UserID)
                                 .Select(c => c.CityOfUser)
                                 .FirstOrDefaultAsync();
            var location = new LocationChanger();
            success = await location.ChangingLocation(message, _logger, _configuration, currentCityOfUser);
            
        }
        else if (message.Text == "/feedback") {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName}) wants to send feedback, check the feedback bot...");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var sender = new Sender();
            success = await sender.SendMessageAsync(message, _logger, _configuration, null, null, "Taklif va shikoyatlaringizni @MuazzinFeedbacks_bot ga yuborishingiz mumkin", null, null);
        }
        else if (message.Text == "/statistic")
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName}) wants to see the total number of users...");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var totalNumberOfUsers = context.Chats.Count();
            var sender = new Sender();
            success = await sender.SendMessageAsync(message, _logger, _configuration, null, null, null, null, $"Umumiy foydalanuvchilar soni: {totalNumberOfUsers}\n\nUshbu botni yaqinlaringizgaham ulashing");

        }
        else
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"'{message.Text}' is sent by {message.From.UserID} ({message.From.FirstName} {message.From.LastName}, ({message.From.UserName}))");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var sender = new Sender();
            success = await sender.SendMessageAsync(message, _logger, _configuration, null, null, null, message.Text, null);
        }

        var response = req.CreateResponse(success ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.InternalServerError);
        return response;
    }
}
