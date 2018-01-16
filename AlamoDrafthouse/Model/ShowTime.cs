using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using com.magusoft.drafthouse.ViewModel;
using HtmlAgilityPack;
using log4net;

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

	public class ShowTime : BindableBase
	{
		private static readonly ILog logger = LogManager.GetLogger(typeof(ShowTime));

		public DateTime? MyShowTime { get; }
		public DelegateCommand BuyTicketCommand { get; }

		public TicketsStatus MyTicketsStatus { get; }
		public int? SeatsLeft { get; }

		public string TicketsUrl { get; }
		
		public ShowTime(DateTime? showTime, string url, string ticketsSaleStatusString, int? seatsLeft)
		{
			this.MyShowTime = showTime;
			this.TicketsUrl = url;
			this.MyTicketsStatus = StringToTicketsSaleStatus(ticketsSaleStatusString);
			this.SeatsLeft = seatsLeft;

			this.BuyTicketCommand = new DelegateCommand(OnBuyTicket, CanBuyTicket);
			this.BuyTicketCommand.ObservesProperty(() => MyTicketsStatus);
		}

		private TicketsStatus StringToTicketsSaleStatus(string ticketsSaleStatusString)
		{
			if (ticketsSaleStatusString == null)
			{
				Debug.Fail("Null tickets status");
				return TicketsStatus.Unknown;
			}

			switch (ticketsSaleStatusString) {
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
				this.TicketsUrl != null && 
				this.MyTicketsStatus == TicketsStatus.OnSale;
		}

		private void OnBuyTicket()
		{
			Process.Start(this.TicketsUrl);
		}
	}
}