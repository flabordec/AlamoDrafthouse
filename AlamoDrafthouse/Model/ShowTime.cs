using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;

namespace com.magusoft.drafthouse.Model
{
	public class ShowTime
	{
		private readonly string mMyShowTime;
		public string MyShowTime
		{
			get
			{
				return mMyShowTime;
			}
		}

		private readonly string mUrl;
		public string Url
		{
			get
			{
				return mUrl;
			}
		}

		private readonly DelegateCommand mBuyTicketCommand;
		public DelegateCommand BuyTicketCommand { get { return mBuyTicketCommand; } }

		public ShowTime(string showTime, string url)
		{
			this.mMyShowTime = showTime;
			this.mUrl = url;
			this.mBuyTicketCommand = new DelegateCommand(OnBuyTicket, CanBuyTicket);
		}

		private bool CanBuyTicket()
		{
			return this.Url != null;
		}

		private void OnBuyTicket()
		{
			Process.Start(this.Url);
		}
	}
}