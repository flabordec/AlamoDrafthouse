using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;
using MaguSoft.ComeAndTicket.Core.ExtensionMethods;
using MaguSoft.ComeAndTicket.Core.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class Theater
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [Required(ErrorMessage = "You must specify a URL"), Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Url { get; set; }
        public string CalendarUrl
        {
            get { return Url.Replace("theater", "calendar"); }
        }

        [Required(ErrorMessage = "You must specify a name")]
        public string Name { get; set; }

        public Market Market { get; set; }

        public HashSet<ShowTime> ShowTimes { get; set; }

        public Theater(Market market, string url, string name)
        {
            Market = market;
            Name = name;
            Url = url;
            ShowTimes = new HashSet<ShowTime>();
        }

        public Theater() { }
    }
}