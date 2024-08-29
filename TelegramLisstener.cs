using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WebAPI;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;
using System;
using Newtonsoft.Json;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class TelegramLisstener
{
    private readonly ILogger<TelegramLisstener> _logger;
    private readonly IConfiguration _configuration;
    private static readonly HttpClient _httpClient = new HttpClient();

    public TelegramLisstener(ILogger<TelegramLisstener> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function("Function1")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        List<string> places = new List<string>
            {
                "Тошкент", "Самарқанд", "Қўқон", "Андижон", "Фарғона", "Зомин", "Наманган", "Ангрен","Арнасой", "Ашхабод", "Бекобод", "Бишкек", "Бойсун", "Булоқбоши", "Бухоро", "Бурчмулла",
                "Газли", "Ғазалкент", "Ғаллаорол", "Ғузор", "Гулистон", "Денов", "Деҳқонобод", "Дўстлик", "Душанбе", "Жалолобод",
                "Жамбул", "Жиззах", "Жомбой", "Конибодом", "Конимех", "Қарши", "Қоровулбозор", "Қоракўл", "Қўнғирот",
                "Қоракўл", "Қизилтепа", "Қўрғонтепа", "Қумқўрғон", "Марғилон", "Мингбулоқ", "Мўйноқ", "Муборак", "Навоий",
                "Нукус", "Нурота", "Олот", "Олмаота", "Олтинкўл", "Олтиариқ", "Поп", "Пахтаобод", "Риштон", "Сайрам",
                "Таллимаржон", "Тахтакўпир", "Термиз", "Томди", "Туркистон", "Туркманобод", "Тошҳовуз", "Тўрткўл", "Учқўрғон",
                "Учқудуқ", "Учтепа", "Ўғиз", "Ўсмат", "Ўш", "Урганч", "Ургут", "Узунқудуқ", "Хазорасп", "Хива", "Хонқа",
                "Хонобод", "Хўжаобод", "Хўжанд", "Чимбой", "Чимкент", "Чортоқ", "Чуст", "Шаҳрихон", "Шеробод", "Шовот", "Шуманай",
                "Янгибозор", "Зарафшон"
            };
        List<string> availableLanguages = new List<string> { "Uz", "Ru" };
        var (message, errorResponse) = await Utils.BuildMessageAsync(req);

        if (errorResponse != null)
        {
            _logger.LogError("Error in building message");
            return errorResponse;
        }
        var userDetails = new UserDetails();
        List<Chat> userDetailsFromJson = await userDetails.UserDetailGetter();
        var currentUser = userDetailsFromJson.FirstOrDefault(c => c.UserID == message.From.UserID);


        string mainKeyboard;
        if (currentUser != null)
        {
            mainKeyboard = KeyboardBuilder.GetMainKeyboard(currentUser.Language);
        }
        else
        {
            mainKeyboard = KeyboardBuilder.GetMainKeyboard("Uz");
        }
        bool success;

        if (message.Text == "/start")
        {
            _logger.LogInformation($"'/start' is sent by {message.From.UserID} ({message.From.FirstName} {message.From.LastName}, ({message.From.UserName}))");

            if (currentUser == null)
            {
                var languageChooser = new LanguageChooser();
                success = await languageChooser.LanguageChooserAsync(message, _logger, _configuration, long.Parse(message.From.UserID));

                await userDetails.UserDetailAdder(message.From);
            }
            else
            {
                var sender = new Sender();
                if (currentUser.Language == "Ru")
                {
                    success = await sender.SendMessageAsync(message, _logger, _configuration, "Добро пожаловать", null, null, null, mainKeyboard);
                } else
                {
                    success = await sender.SendMessageAsync(message, _logger, _configuration, "Xush kelibsiz", null, null, null, mainKeyboard);
                }
            }
        }

        else if (availableLanguages.Contains(message.From.CallBackQuery) && !string.IsNullOrEmpty(currentUser.CallBackQuery))
        {
            string botToken = "YourBotTokenHere";
            string deleteMessageApiUrl = $"https://api.telegram.org/bot{botToken}/deleteMessage";

            var sender = new Sender();

            string userIdToEdit = message.From.UserID;
            string language = message.From.CallBackQuery;

            await userDetails.UserDetailRemover(currentUser);

            currentUser.Language = language;
            await userDetails.UserDetailAdder(currentUser);

            var editPayload = new
            {
                chat_id = message.From.UserID,
                message_id = message.MessageId
            };

            var editContent = new StringContent(JsonConvert.SerializeObject(editPayload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(deleteMessageApiUrl, editContent);

            if (language == "Ru")
            {
                mainKeyboard = KeyboardBuilder.GetMainKeyboard(currentUser.Language);
                success = await sender.SendMessageAsync(message, _logger, _configuration, "Вы выбрали русский язык", null, null, null, mainKeyboard);
            }
            else
            {
                mainKeyboard = KeyboardBuilder.GetMainKeyboard(currentUser.Language);

                success = await sender.SendMessageAsync(message, _logger, _configuration, "Siz o'zbek tilini tanladingiz", null, null, null, mainKeyboard);
            }
        }

        else if (availableLanguages.Contains(message.From.CallBackQuery) && string.IsNullOrEmpty(currentUser.CallBackQuery))
        {
            string botToken = "YourBotTokenHere";
            string deleteMessageApiUrl = $"https://api.telegram.org/bot{botToken}/deleteMessage";
            string userIdToEdit = message.From.UserID;
            string language = message.From.CallBackQuery;

            await userDetails.UserDetailRemover(currentUser);

            currentUser.Language = language;
            await userDetails.UserDetailAdder(currentUser);

            var editPayload = new
            {
                chat_id = message.From.UserID,
                message_id = message.MessageId
            };

            var editContent = new StringContent(JsonConvert.SerializeObject(editPayload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(deleteMessageApiUrl, editContent);

            var sender = new Sender();
            if (currentUser.Language == "Ru")
            {
                mainKeyboard = KeyboardBuilder.GetMainKeyboard(currentUser.Language);
                success = await sender.SendMessageAsync(message, _logger, _configuration, "Вы выбрали русский язык", null, null, null, mainKeyboard);
            }
            else
            {
                mainKeyboard = KeyboardBuilder.GetMainKeyboard(currentUser.Language);
                success = await sender.SendMessageAsync(message, _logger, _configuration, "Siz o'zbek tilini tanladingiz", null, null, null, mainKeyboard);
            }

            message.MessageId = "";
            var cityChooser = new StartTask();
            success = await cityChooser.ChoosingCityNameAsync(message, _logger, _configuration, language);
        }

        else if (message.From.CallBackQuery != null && message.From.CallBackQuery.StartsWith("page_"))
        {
            var parts = message.From.CallBackQuery.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[1], out int page))
            {
                var startTask = new StartTask();
                success = await startTask.ChoosingCityNameAsync(message, _logger, _configuration, message.From.Language, page);
            }
            else
            {
                success = false;
            }
        }

        else if (places.Contains(message.From.CallBackQuery))
        {
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName})'s city is {message.From.CallBackQuery}");
            if (currentUser.CallBackQuery == "")
            {
                string userIdToEdit = message.From.UserID;
                string cityName = message.From.CallBackQuery;

                await userDetails.UserDetailRemover(currentUser);

                currentUser.CallBackQuery = cityName;
                await userDetails.UserDetailAdder(currentUser);

                var sender = new Sender();
                success = await sender.SendCityOfUserAsync(message, _logger, _configuration, currentUser.Language);
            }
            else
            {
                var sender = new Sender();

                string userIdToEdit = message.From.UserID;
                string cityName = message.From.CallBackQuery;

                await userDetails.UserDetailRemover(currentUser);

                currentUser.CallBackQuery = cityName;
                await userDetails.UserDetailAdder(currentUser); 
                
                await sender.SendCityOfUserAsync(message, _logger, _configuration, currentUser.Language);
                if (currentUser.Language == "Ru")
                {
                    success = await sender.SendMessageAsync(message, _logger, _configuration, "Ваша местоположния изменено", null, null, null, mainKeyboard);
                }
                else
                {
                    success = await sender.SendMessageAsync(message, _logger, _configuration, "Sizning joylashuvingiz o'zgartirildi", null, null, null, mainKeyboard);
                }

            }

        }

        else if (message.Text == "/location" || message.Text == "📍 Joylashuvni o'zgartirish" || message.Text == "📍 Изменение местоположения")
        {
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName}) wants to change his/her location...");

            string currentCityOfUser = currentUser.CallBackQuery;

            var locationChanger = new StartTask();

            message.MessageId = "";
            success = await locationChanger.ChoosingCityNameAsync(message, _logger, _configuration, currentUser.Language, 1, currentCityOfUser);
        }

        else if (message.Text == "/feedback" || message.Text == "✍ Taklif va shikoyatlar uchun" || message.Text == "✍ Для отзывов и жалоб") {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName}) wants to send feedback, check the feedback bot...");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var sender = new Sender();
            if (currentUser.Language == "Ru")
            {
                success = await sender.SendMessageAsync(message, _logger, _configuration, "Вы можете отправлять отзывы или жалобы на @MuazzinFeedbacks_bot", null, null, null, mainKeyboard);
            }
            else
            {
                success = await sender.SendMessageAsync(message, _logger, _configuration, "Taklif yoki shikoyatlaringizni @MuazzinFeedbacks_bot ga yuborishingiz mumkin", null, null, null, mainKeyboard);
            }
        }

        else if (message.Text == "/statistic" || message.Text == "📊 Umumiy foydalanuvchilar soni" || message.Text == "📊 Общее количество пользователей")
        {
            _logger.LogInformation("------------------------------------------------------------------------------");
            _logger.LogInformation($"{message.From.FirstName} {message.From.LastName} {message.From.UserID} ({message.From.UserName}) wants to see the total number of users...");
            _logger.LogInformation("------------------------------------------------------------------------------");
            var totalNumberOfUsers = userDetailsFromJson.Count;
            var sender = new Sender();
            if (currentUser.Language == "Ru")
            {
                success = await sender.SendMessageAsync(message, _logger, _configuration, $"Общее количество пользователей {totalNumberOfUsers}\n\n Отправьте этот бот своим друзьям.\n@MuazzinUz_bot", null, null, null, mainKeyboard);
            }
            else
            {
                success = await sender.SendMessageAsync(message, _logger, _configuration, null, null, null, $"Umumiy foydalanuvchilar soni: {totalNumberOfUsers}\n\nUshbu botni yaqinlaringizgaham ulashing\n@MuazzinUz_bot", mainKeyboard);

            }
        }

        else if (message.Text == "/language" || message.Text == "🇺🇿 Tilni o'zgartirish" || message.Text == "🇷🇺 Изменение языка")
        {
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