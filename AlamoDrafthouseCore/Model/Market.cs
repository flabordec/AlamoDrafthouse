﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.magusoft.drafthouse.Helpers;
using GalaSoft.MvvmLight;
using HtmlAgilityPack;
using com.magusoft.drafthouse.ExtensionMethods;
using GalaSoft.MvvmLight.Command;

namespace com.magusoft.drafthouse.Model
{
    public class Market : ObservableObject
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public string Url { get; }
        public string Name { get; }

        private bool _theatersLoaded;
        public bool TheatersLoaded
        {
            get { return _theatersLoaded; }
            private set { Set(ref _theatersLoaded, value); }
        }

        public ObservableCollection<Theater> Theaters { get; }

        public RelayCommand LoadTheatersCommand { get; }

        private bool mLoadingTheaters;
        public bool LoadingTheaters
        {
            get { return mLoadingTheaters; }
            private set { Set(ref mLoadingTheaters, value); }
        }

        public Market(string url, string name)
        {
            Url = url;
            Name = name;
            Theaters = new ObservableCollection<Theater>();
            LoadTheatersCommand = new RelayCommand(async () => await OnLoadTheatersAsync());

            _theatersLoaded = false;
        }

        public async Task OnLoadTheatersAsync()
        {
            try
            {
                LoadingTheaters = true;
                logger.Info("Reading theaters for {0}", Name);
                await InnerOnLoadTheatersAsync();
            }
            finally
            {
                LoadingTheaters = false;
            }
        }

        private async Task InnerOnLoadTheatersAsync()
        {
            lock (Theaters)
            {
                if (Theaters.Any())
                    return;
            }

            HtmlDocument marketsDocument = await InternetHelpers.GetPageHtmlDocumentAsync(Url);

            var theaters =
                from node in marketsDocument.DocumentNode.Descendants("a")
                where node.Attributes["class"] != null && node.Attributes["class"].Value == "button small secondary Showtimes-time"
                let theaterUrl = node.Attributes["href"].Value
                select new Theater(node.InnerText, theaterUrl);

            if (!Theaters.Any())
            {
                lock (Theaters)
                {
                    if (Theaters.Any())
                        return;

                    Theaters.AddRange(theaters);
                    TheatersLoaded = true;
                }
            }
        }
    }
}