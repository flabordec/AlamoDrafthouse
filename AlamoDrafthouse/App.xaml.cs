using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using com.magusoft.drafthouse.Model;
using com.magusoft.drafthouse.ViewModel;
using NDesk.Options;

namespace com.magusoft.drafthouse
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			string marketName = null;
			string movieTitle = string.Empty;
			bool isService = false;
			var optionSet = new OptionSet()
				{
					{ "m|market=", m => marketName = m?.ToLowerInvariant() },
					{ "v|movie=", m => movieTitle = m?.ToLowerInvariant() },
					{ "s|service", s => isService = true }
				};
			optionSet.Parse(e.Args);

			if (isService)
			{

			}
			else
			{
				MainWindow window = new MainWindow();
				AlamoDrafthouseDataContext context = window.MainDockPanel.DataContext as AlamoDrafthouseDataContext;
				window.Show();
				context.InitializeAsync(marketName, movieTitle).ContinueWith(t => { });
			}
		}
	}
}
