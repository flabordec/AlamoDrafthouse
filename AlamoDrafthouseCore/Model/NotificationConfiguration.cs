using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    //"Notifications": [
    //    {
    //        "Markets": {
    //            "Austin": {
    //                "Cinemas": [ "All" ]
    //            }
    //        },
    //        "SuperTitles": [ "pancake", "cyop" ]
    //    },
    //    {
    //        "Markets": {
    //            "Austin": {
    //                "Cinemas": [ "Lakeline" ]
    //            }
    //        },
    //        "Shows": [ "dungeons & dragons", "dungeons and dragons" ],
    //        "After":  "2023-04-15",
    //        "Before": "2023-04-17",
    //        "DayOfWeek": [ "Saturday", "Sunday" ]
    //    }
    //],

    public class NotificationConfiguration
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public List<MarketNotificationConfiguration> Markets { get; } = new();
        public List<string> SuperTitles { get; } = new();
        public List<string> Shows { get; } = new();
        public DateOnly? After { get; set; }
        public DateOnly? Before { get; set; }
        public List<DayOfWeek> DayOfWeek { get; set; } = new();
    }

    public class MarketNotificationConfiguration
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; }
        public List<string> Cinemas { get; } = new();
    }
}
