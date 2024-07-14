using Newtonsoft.Json.Linq;

namespace WebAPI
{
    public class AllSpecificTimesGetter
    {
        public (int, int)[] GetAllSpecificTimes(List<JToken> dailyPrayerTimes)
        {
            (int hour, int minute)[] allSpecificTimes = new (int, int)[0];

            foreach (var dailyPrayerTime in dailyPrayerTimes)
            {
                // Create a new array for each iteration
                (int hour, int minute)[] newSpecificTimes = new (int, int)[6];

                // Parse and assign values to newSpecificTimes
                string xufton = dailyPrayerTime[10].ToString();
                string[] xuftonParts = xufton.Split(':');
                newSpecificTimes[0] = (int.Parse(xuftonParts[0]), int.Parse(xuftonParts[1]));

                string shom = dailyPrayerTime[9].ToString();
                string[] shomParts = shom.Split(':');
                newSpecificTimes[1] = (int.Parse(shomParts[0]), int.Parse(shomParts[1]));

                string asr = dailyPrayerTime[8].ToString();
                string[] asrParts = asr.Split(':');
                newSpecificTimes[2] = (int.Parse(asrParts[0]), int.Parse(asrParts[1]));

                string peshin = dailyPrayerTime[7].ToString();
                string[] peshinParts = peshin.Split(':');
                newSpecificTimes[3] = (int.Parse(peshinParts[0]), int.Parse(peshinParts[1]));

                string quyosh = dailyPrayerTime[6].ToString();
                string[] quyoshParts = quyosh.Split(':');
                newSpecificTimes[4] = (int.Parse(quyoshParts[0]), int.Parse(quyoshParts[1]));

                string bomdod = dailyPrayerTime[5].ToString();
                string[] bomdodParts = bomdod.Split(':');
                newSpecificTimes[5] = (int.Parse(bomdodParts[0]), int.Parse(bomdodParts[1]));

                allSpecificTimes = ConcatArrays(allSpecificTimes, newSpecificTimes);
            }
            static T[] ConcatArrays<T>(T[] array1, T[] array2)
            {
                T[] newArray = new T[array1.Length + array2.Length];
                Array.Copy(array1, 0, newArray, 0, array1.Length);
                Array.Copy(array2, 0, newArray, array1.Length, array2.Length);
                return newArray;
            }
            return allSpecificTimes;
        }
    }
}

