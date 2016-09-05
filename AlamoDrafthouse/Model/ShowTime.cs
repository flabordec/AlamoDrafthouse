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
	public enum TicketsState
	{
		Unknown,
		OnSale,
		NotOnSale,
	}

	public class ShowTime : BindableBase
	{
		private static readonly ILog logger = LogManager.GetLogger(typeof(ShowTime));

		public DelegateCommand CheckTicketsOnSaleCommand { get; }
		public DateTime MyShowTime { get; }
		public DelegateCommand BuyTicketCommand { get; }

		private string InitialUrl { get; }

		private TicketsState mMyTicketsState;
		public TicketsState MyTicketsState
		{
			get { return mMyTicketsState; }
			set
			{
				SetProperty(ref mMyTicketsState, value);
				OnPropertyChanged(() => TicketsOnSaleLoaded);
			}
		}

		public bool TicketsOnSaleLoaded
		{
			get { return MyTicketsState != TicketsState.Unknown; }
		}

		private string mTicketsSaleStatus;
		public string TicketsSaleStatus
		{
			get { return mTicketsSaleStatus; }
			set { SetProperty(ref mTicketsSaleStatus, value); }
		}

		private string mTicketsUrl;
		public string TicketsUrl
		{
			get { return mTicketsUrl; }
			set { SetProperty(ref mTicketsUrl, value); }
		}

		private bool mCheckingTicketsOnSale;
		public bool CheckingTicketsOnSale
		{
			get { return mCheckingTicketsOnSale; }
			private set { SetProperty(ref mCheckingTicketsOnSale, value); }
		}

		public ShowTime(DateTime showTime, string url)
		{
			this.MyShowTime = showTime;
			this.InitialUrl = url;

			this.BuyTicketCommand = new DelegateCommand(OnBuyTicket, CanBuyTicket);
			this.BuyTicketCommand.ObservesProperty(() => MyTicketsState);

			this.CheckTicketsOnSaleCommand = DelegateCommand.FromAsyncHandler(OnCheckTicketsOnSaleAsync, CanCheckTicketsOnSaleAsync);
			this.CheckTicketsOnSaleCommand.ObservesProperty(() => MyTicketsState);
		}
		
		private bool CanBuyTicket()
		{
			// TODO Change when I'm more confident I'm really getting the tickets
			// return this.InitialUrl != null && (!this.TicketsOnSale.HasValue || this.TicketsOnSale.Value);

			return this.InitialUrl != null;
		}

		private void OnBuyTicket()
		{
			Process.Start(this.InitialUrl);
		}

		private bool CanCheckTicketsOnSaleAsync()
		{
			return this.InitialUrl != null && this.MyTicketsState == TicketsState.Unknown;
		}

		public async Task OnCheckTicketsOnSaleAsync()
		{
			try
			{
				CheckingTicketsOnSale = true;
				await InnerCheckTicketsOnSaleAsync();
			}
			finally
			{
				CheckingTicketsOnSale = false;
			}
		}

		private async Task InnerCheckTicketsOnSaleAsync()
		{
			lock (this)
			{
				if (this.MyTicketsState != TicketsState.Unknown)
					return;
			}

			HtmlDocument ticketsOnSaleDocument = await WebDriverHelper.GetPageHtmlDocumentAsync(this.InitialUrl);

			var saleNode = (
				from node in ticketsOnSaleDocument.DocumentNode.Descendants()
				where ClassMatches(node, "button", "expand", "u-noMarginBot")
				select node
				).SingleOrDefault();

			if (this.MyTicketsState == TicketsState.Unknown)
			{
				lock (this)
				{
					if (this.MyTicketsState != TicketsState.Unknown)
						return;

					if (saleNode != null)
					{
						this.MyTicketsState = (saleNode.Name == "a") ? TicketsState.OnSale : TicketsState.NotOnSale;
						this.TicketsSaleStatus = saleNode.InnerText;
						this.TicketsUrl = saleNode.Attributes["href"]?.Value;
					}
				}
			}
		}

		private static bool ClassMatches(HtmlNode node, params string[] classes)
		{
			if (node.Attributes["class"] == null)
				return false;

			bool allMatched = classes.All(c => node.Attributes["class"].Value.Contains(c));
			return allMatched;
		}
	}
}