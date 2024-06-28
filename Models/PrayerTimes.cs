using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAPI.Models
{
    public class PrayerTimes
    {
        public string CityName { get; set; }
        public NamesOfMonth Month { get; set; }
        public string DayInHijri { get; set; }
        public string DayInQamari { get;set; }
        public string DayOfWeek { get; set; }
        public string Fajr { get; set; }
        public string Sunrise { get; set; }
        public string Zuhr { get; set; }
        public string Asr { get; set; }
        public string Magrib { get; set; }
        public string Isha { get; set; }
    }
}
