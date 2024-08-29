using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebAPI.Models;

namespace WebAPI
{
    public class DailyPrayerTimesAndUserDetailsGetter
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<(List<JToken>, Dictionary<string, long[]>)> GetDailyPrayerTimes(int month, int day, List<Chat> allUsers)
        {
            List<JToken> dailyPrayerTimes = new List<JToken>();
            Dictionary<string, long[]> detailsOfUsers = new Dictionary<string, long[]>();
            bool shouldBreak = false;

            string jsonUrl = "YourJSONUrlOfPrayerTimesHere";


            // Fetching the compressed JSON file
            var response = await _httpClient.GetAsync(jsonUrl);


            var compressedStream = await response.Content.ReadAsStreamAsync();

            using (var decompressedStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var streamReader = new StreamReader(decompressedStream))
            {
                var jsonString = await streamReader.ReadToEndAsync();
                JObject jsonObject = JObject.Parse(jsonString);

                foreach (var user in allUsers)
                {
                    shouldBreak = false;
                    foreach (var item in dailyPrayerTimes)
                    {
                        if (item?[0].ToString() == user.CallBackQuery)
                        {
                            if (!detailsOfUsers.TryGetValue(user.CallBackQuery, out long[] userIds))
                            {
                                userIds = new long[] { };
                            }

                            long userId = long.Parse(user.UserID);
                            long[] newUserIds = new long[userIds.Length + 1];
                            Array.Copy(userIds, newUserIds, userIds.Length);
                            newUserIds[newUserIds.Length - 1] = userId;

                            detailsOfUsers[user.CallBackQuery] = newUserIds;
                            shouldBreak = true;
                            break;
                        }
                    }

                    if (shouldBreak)
                    {
                        continue;
                    }

                    string cityName = user.CallBackQuery?.First().ToString().ToUpper() + user.CallBackQuery?.Substring(1).ToLower();
                    string path = $"$.{cityName}[{month - 1}].monthData[{day - 1}]";
                    JToken value = jsonObject.SelectToken(path);

                    if (value == null)
                    {
                        continue;
                    }

                    detailsOfUsers[user.CallBackQuery] = new long[] { long.Parse(user.UserID) };
                    dailyPrayerTimes.Add(value);
                }
            }

            return (dailyPrayerTimes, detailsOfUsers);
        }
    }
}
