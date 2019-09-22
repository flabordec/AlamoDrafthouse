using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;

namespace com.magusoft.drafthouse.Model
{
    public enum TicketsStatus
    {
        Unknown,
        OnSale,
        NotOnSale,
        SoldOut,
        Past,
    }

    public class ShowTime : ObservableObject
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public DateTime? MyShowTime { get; }
        public RelayCommand BuyTicketCommand { get; }

        public TicketsStatus MyTicketsStatus { get; }
        public int? SeatsLeft { get; }

        public string TicketsUrl { get; }

        public ShowTime(DateTime? showTime, string url, string ticketsSaleStatusString, int? seatsLeft)
        {
            MyShowTime = showTime;
            TicketsUrl = url;
            MyTicketsStatus = StringToTicketsSaleStatus(ticketsSaleStatusString);
            SeatsLeft = seatsLeft;

            BuyTicketCommand = new RelayCommand(OnBuyTicket, CanBuyTicket);
        }

        private TicketsStatus StringToTicketsSaleStatus(string ticketsSaleStatusString)
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

        private bool CanBuyTicket()
        {
            return
                TicketsUrl != null &&
                MyTicketsStatus == TicketsStatus.OnSale;
        }

        private void OnBuyTicket()
        {
            Process.Start(TicketsUrl);
        }
    }
}