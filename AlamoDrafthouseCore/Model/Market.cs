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

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class Market
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [Required(ErrorMessage = "You must specify a URL"), Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Url { get; set; }
        [Required(ErrorMessage = "You must specify a Name")]
        public string Name { get; set; }

        public List<Theater> Theaters { get; }

        public Market(string url, string name)
        {
            Url = url;
            Name = name;
            Theaters = new List<Theater>();
        }
    }
}
