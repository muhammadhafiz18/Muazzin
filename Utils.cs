using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using WebAPI.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebAPI
{
    public static class Utils
    {
        public static async Task<(Message message, HttpResponseData errorResponse)> BuildMessageAsync(HttpRequestData req)
        {
            string requestBody;
            using (StreamReader reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            dynamic payload = JsonConvert.DeserializeObject(requestBody);

            if (payload?.message == null && payload?.callback_query == null)
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid payload" });
                return (null, badResponse);
            }

            var message = new Message();

            if (payload?.message != null)
            {
                message = new Message
                {
                    MessageId = payload.message.message_id,
                    Text = payload.message.text,
                    From = new Chat
                    {
                        Id = payload.message.from.id,
                        FirstName = payload.message.from.first_name,
                        LastName = payload.message.from.last_name,
                        UserName = payload.message.from.username,
                    },
                    Chat = new Chat
                    {
                        Id = payload.message.chat.id // Assuming the chat ID should be filled similarly
                    }
                };
            }
            else if (payload?.callback_query != null)
            {
                message = new Message
                {
                    MessageId = payload.callback_query.message.message_id,
                    Text = payload.callback_query.message.text,
                    From = new Chat
                    {
                        Id = payload.callback_query.from.id,
                        FirstName = payload.callback_query.from.first_name,
                        LastName = payload.callback_query.from.last_name,
                        UserName = payload.callback_query.from.username,
                    },
                    Chat = new Chat
                    {
                        Id = payload.callback_query.message.chat.id // Assuming the chat ID should be filled similarly
                    },
                    CityOfUser = payload.callback_query.data
                };
            }

            return (message, null);
        }

    }
}
