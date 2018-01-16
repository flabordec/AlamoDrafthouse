using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.magusoft.drafthouse.ViewModel;
using HtmlAgilityPack;
using Prism.Commands;
using Prism.Mvvm;
using log4net;

namespace com.magusoft.drafthouse.Model
{
	public class Market : BindableBase
	{
		private static readonly ILog logger = LogManager.GetLogger(typeof(Theater));

		public string Url { get; }
		public string Name { get; }

		private bool mTheatersLoaded;
		public bool TheatersLoaded
		{
			get { return mTheatersLoaded; }
			private set { SetProperty(ref mTheatersLoaded, value); }
		}

		public ObservableCollection<Theater> Theaters { get; }

		public DelegateCommand LoadTheatersCommand { get; }

		private bool mLoadingTheaters;
		public bool LoadingTheaters
		{
			get { return mLoadingTheaters; }
			private set { SetProperty(ref mLoadingTheaters, value); }
		}

		public Market(string url, string name)
		{
			this.Url = url;
			this.Name = name;
			this.Theaters = new ObservableCollection<Theater>();
			this.LoadTheatersCommand = DelegateCommand.FromAsyncHandler(OnLoadTheatersAsync);

			this.mTheatersLoaded = false;
		}

		public async Task OnLoadTheatersAsync()
		{
			try
			{
				LoadingTheaters = true;
				logger.InfoFormat("Reading theaters for {0}", this.Name);
				await InnerOnLoadTheatersAsync();
			}
			finally
			{
				LoadingTheaters = false;
			}
		}

		private async Task InnerOnLoadTheatersAsync()
		{
			lock (this.Theaters)
			{
				if (this.Theaters.Any())
					return;
			}

			HtmlDocument marketsDocument = await InternetHelpers.GetPageHtmlDocumentAsync(this.Url);

			var theaters =
				from node in marketsDocument.DocumentNode.Descendants("a")
				where node.Attributes["class"] != null && node.Attributes["class"].Value == "button small secondary Showtimes-time"
				let theaterUrl = node.Attributes["href"].Value
				select new Theater(node.InnerText, theaterUrl);

			if (!this.Theaters.Any())
			{
				lock (this.Theaters)
				{
					if (this.Theaters.Any())
						return;

					this.Theaters.AddRange(theaters);
					this.TheatersLoaded = true;
				}
			}
		}
	}
}
