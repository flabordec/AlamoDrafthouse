using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public enum TicketsStatus
    {
        Unknown,
        OnSale,
        NotOnSale,
        SoldOut,
        Past,
    }

    public class ShowTimeComparer
    {
        public static IEqualityComparer<ShowTime> Url => new ShowTimeUrlComparer();
    }

    class ShowTimeUrlComparer : IEqualityComparer<ShowTime>
    {
        private readonly StringComparer _comparer = StringComparer.OrdinalIgnoreCase;

        public ShowTimeUrlComparer()
        {
        }

        public bool Equals([AllowNull] ShowTime x, [AllowNull] ShowTime y)
        {
            if (ReferenceEquals(null, x) && ReferenceEquals(null, y))
                return true;
            if (ReferenceEquals(null, x))
                return false;
            if (ReferenceEquals(null, y))
                return false;
            if (ReferenceEquals(x, y))
                return true;
            return _comparer.Equals(x.TicketsUrl, y.TicketsUrl);
        }

        public int GetHashCode([DisallowNull] ShowTime obj)
        {
            return _comparer.GetHashCode(obj.TicketsUrl);
        }
    }

    public class ShowTime
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [Required(ErrorMessage = "You must specify a URL"), Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string TicketsUrl { get; set; }

        public DateTime? Date { get; set; }

        public TicketsStatus TicketsStatus { get; set; }

        public int? SeatsLeft { get; set; }

        public string TheaterUrl { get; set; }
        [ForeignKey(nameof(TheaterUrl))]
        public Theater Theater { get; set; }

        public string MovieTitle { get; set; }
        [ForeignKey(nameof(MovieTitle))]
        public Movie Movie { get; set; }

        public HashSet<ShowTimeNotification> UsersUpdated { get; set; }

        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }

        public ShowTime(Theater theater, string ticketsUrl, DateTime? date, string ticketsSaleStatusString, int? seatsLeft)
        {
            TheaterUrl = theater.Url;
            Theater = theater;
            Date = date;
            TicketsUrl = ticketsUrl;
            TicketsStatus = StringToTicketsSaleStatus(ticketsSaleStatusString);
            SeatsLeft = seatsLeft;

            UsersUpdated = new HashSet<ShowTimeNotification>();
        }

        public ShowTime()
        {
        }

        private static TicketsStatus StringToTicketsSaleStatus(string ticketsSaleStatusString)
        {
            if (ticketsSaleStatusString == null)
            {
                Debug.Fail("Null tickets status");
                return TicketsStatus.Unknown;
            }

            switch (ticketsSaleStatusString)
            {
                case "onsale":
                    return TicketsStatus.OnSale;
                case "notonsale":
                    return TicketsStatus.NotOnSale;
                case "soldout":
                    return TicketsStatus.SoldOut;
                case "past":
                    return TicketsStatus.Past;
                default:
                    Debug.Fail($"Unexpected value: {ticketsSaleStatusString}");
                    return TicketsStatus.Unknown;
            }
        }
    }
}