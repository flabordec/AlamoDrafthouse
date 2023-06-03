using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class SuperTitle
    {
        [Key, JsonPropertyName("slug"), Required(ErrorMessage = "You must specify a slug")]
        public string Slug { get; set; }
        [JsonPropertyName("superTitle")]
        public string Name { get; set; }
    }

    public class Presentation
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [Key, JsonPropertyName("slug"), Required(ErrorMessage = "You must specify a slug")]
        public string Slug { get; set; }
        [JsonPropertyName("show")]
        public Show Show { get; set; }
        [Required(ErrorMessage = "You must specify a market")]
        public Market Market { get; set; }

        public HashSet<Session> Sessions { get; } = new();
        [JsonPropertyName("superTitle")]
        public SuperTitle SuperTitle { get; set; }
    }

    public class Show
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [Key, JsonPropertyName("slug"), Required(ErrorMessage = "You must specify a slug")]
        public string Slug { get; set; }
        [JsonPropertyName("title"), Required(ErrorMessage = "You must specify a title")]
        public string Title { get; set; }
    }
}