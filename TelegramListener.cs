using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WebAPI;
using WebAPI.Models;

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
            var startTask = new StartTask();
            success = await startTask.ChoosingCityNameAsync(message, _logger, _configuration, req);
        }
        else if (message.From.CityOfUser != null)
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName})'s city is {message.From.CityOfUser}");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var dataToDb = message.From;
            context.Chats.Add(dataToDb);
            context.SaveChanges();
            var sender = new Sender();
            success = await sender.SendCityOfUserAsync(message, _logger, _configuration);
        }
        else
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"'{message.Text}' is sent by {message.From.UserID} ({message.From.FirstName} {message.From.LastName}, ({message.From.UserName}))");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var sender = new Sender();
            success = await sender.SendMessageAsync(message, _logger, _configuration);
        }

        var response = req.CreateResponse(success ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.InternalServerError);
        return response;
    }
}
