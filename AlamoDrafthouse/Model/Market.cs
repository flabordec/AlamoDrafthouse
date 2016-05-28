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

namespace com.magusoft.drafthouse.Model
{
	public class Market : BindableBase
	{
		private readonly string mUrl;
		public string Url
		{
			get { return mUrl; }
		}

		private readonly string mName;
		public string Name
		{
			get { return mName; }
		}

		private bool mTheatersLoaded;
		public bool TheatersLoaded
		{
			get { return mTheatersLoaded; }
			private set { SetProperty(ref mTheatersLoaded, value); }
		}

		private readonly ObservableCollection<Theater> mTheaters;
		public ObservableCollection<Theater> Theaters
		{
			get
			{
				return mTheaters;
			}
		}

		private readonly DelegateCommand mLoadTheatersCommand;
		public DelegateCommand LoadTheatersCommand
		{
			get { return mLoadTheatersCommand; }
		}

		private bool mLoadingTheaters;
		public bool LoadingTheaters
		{
			get { return mLoadingTheaters; }
			private set { SetProperty(ref mLoadingTheaters, value); }
		}

		public Market(string url, string name)
		{
			this.mUrl = url;
			this.mName = name;
			this.mTheaters = new ObservableCollection<Theater>();
			this.mLoadTheatersCommand = DelegateCommand.FromAsyncHandler(OnLoadTheatersAsync);
		}

		public async Task OnLoadTheatersAsync()
		{
			try
			{
				LoadingTheaters = true;
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

			HtmlDocument marketsDocument = await WebDriverHelper.GetPageHtmlDocumentAsync(this.Url);

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
