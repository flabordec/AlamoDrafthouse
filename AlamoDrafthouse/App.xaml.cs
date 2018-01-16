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
using log4net.Config;
using log4net;

namespace com.magusoft.drafthouse
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private static readonly ILog logger = LogManager.GetLogger(typeof(App));

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			XmlConfigurator.Configure();

			string marketName = null;
			string movieTitle = string.Empty;
			bool isService = false;
			string eMailAddress = string.Empty;
			string eMailPassword = string.Empty;
			string toAddress = string.Empty;
			var optionSet = new OptionSet()
				{
					{ "m|market=", m => marketName = m?.ToLowerInvariant() },
					{ "v|movie=", m => movieTitle = m?.ToLowerInvariant() },
					{ "s|service", s => isService = true },
					{ "a|eMailAddress=", m => eMailAddress = m },
					{ "p|eMailPassword=", p => eMailPassword = p },
				};
			optionSet.Parse(e.Args);
			
			if (isService)
			{
				toAddress = ConfigurationManager.AppSettings["toAddress"];

				if (string.IsNullOrEmpty(eMailAddress) || 
					string.IsNullOrEmpty(eMailPassword) || 
					string.IsNullOrEmpty(toAddress))
				{
					logger.Error(
						"The e-mail addresses and password must be specified if the application runs as " +
						"a service, running normally");
					isService = false;
				}
			}

			MainWindow window = new MainWindow();
			AlamoDrafthouseDataContext context = window.MainDockPanel.DataContext as AlamoDrafthouseDataContext;
			if (!isService)
				window.Show();
			context.InitializeAsync(marketName, movieTitle, eMailAddress, eMailPassword, toAddress, isService).ContinueWith(t => { });
		}
	}
}
