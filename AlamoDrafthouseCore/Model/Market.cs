using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using GalaSoft.MvvmLight.Command;
using MaguSoft.ComeAndTicket.Core.Helpers;
using MaguSoft.ComeAndTicket.Core.ExtensionMethods;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class Market
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [JsonPropertyName("id")]
        public string Id { get; set; }
        [Required(ErrorMessage = "You must specify a Slug"), JsonPropertyName("slug")]
        public string Slug { get; set; }
        [Required(ErrorMessage = "You must specify a Name"), JsonPropertyName("name")]
        public string Name { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string MarketUrl => $"https://drafthouse.com/s/mother/v2/schedule/market/{Slug}";


        public HashSet<Cinema> Cinemas { get; } = new ();
        public HashSet<Presentation> Presentations { get; } = new ();
    }
}
