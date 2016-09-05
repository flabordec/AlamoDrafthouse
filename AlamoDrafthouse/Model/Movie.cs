using HtmlAgilityPack;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.magusoft.drafthouse.Model
{
	public class Movie : BindableBase
	{
		public string Title { get; }
		
		public Theater Theater { get; }

		private readonly List<ShowTime> mShowTimes;
		public IEnumerable<ShowTime> ShowTimes
		{
			get { return mShowTimes; }
		}

		public Movie(Theater theater, string title, IEnumerable<ShowTime> showTimes)
		{
			this.Theater = theater;
			this.Title = title;
			this.mShowTimes = new List<ShowTime>(showTimes);
		}
	}
}
