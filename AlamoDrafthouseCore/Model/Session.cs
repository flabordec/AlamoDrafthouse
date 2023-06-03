using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

    public class Session
    {
        [Key, Required(ErrorMessage = "You must specify an ID"), JsonPropertyName("sessionId")]
        public string Id { get; set; }
        [JsonPropertyName("status")]
        public string TicketStatus { get; set; }
        [JsonPropertyName("cinemaId")]
        public string CinemaId { get; set; }
        [JsonPropertyName("presentationSlug")]
        public string PresentationSlug { get; set; }
        [JsonPropertyName("showTimeUtc")]
        public DateTime ShowTimeUtc { get; set; }
        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; }
        [JsonPropertyName("ticketTypesNormalCount")]
        public int Tickets { get; set; }

        [NotMapped]
        public Cinema Cinema { get; set; }
        [NotMapped]
        public Presentation Presentation { get; set; }

        public string TicketsUrl => $"https://drafthouse.com/austin/show/{PresentationSlug}?cinemaId={CinemaId}&sessionId={Id}";


        public static TicketsStatus StringToTicketsSaleStatus(string ticketsSaleStatusString)
        {
            if (ticketsSaleStatusString == null)
            {
                Debug.Fail("Null tickets status");
                return TicketsStatus.Unknown;
            }

            switch (ticketsSaleStatusString)
            {
                case "onsale":
                case "ONSALE":
                    return TicketsStatus.OnSale;
                case "notonsale":
                case "NOTONSALE":
                    return TicketsStatus.NotOnSale;
                case "soldout":
                case "SOLDOUT":
                    return TicketsStatus.SoldOut;
                case "past":
                case "PAST":
                    return TicketsStatus.Past;
                default:
                    Debug.Fail($"Unexpected value: {ticketsSaleStatusString}");
                    return TicketsStatus.Unknown;
            }
        }
    }
}
