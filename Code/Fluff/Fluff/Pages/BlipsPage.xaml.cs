using e6API;
using Fluff.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Fluff.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BlipsPage : Page
    {
        // TODO replying to posts

        ObservableCollection<Blip> BlipsViewModel;
        RequestHost host;
        public BlipsPage()
        {
            this.InitializeComponent();
            host = new RequestHost(SettingsHandler.UserAgent);
            host.Username = SettingsHandler.Username;
            host.ApiKey = SettingsHandler.ApiKey;
            BlipsViewModel = new ObservableCollection<Blip>();

            Thread t = new Thread(LoadBlips);
            t.Start();
        }

        // Api Functions
        private async void LoadBlips()
        {
            var blips = await host.ListBlips();

            foreach(var blip in blips)
            {
                blip.body = await DTextConverter.ToMarkdown(blip.body);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    BlipsViewModel.Add(blip);
                });
            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                LoadProgress.Visibility = Visibility.Collapsed;
            });
        }

        // Markdown functions
        private async void MarkdownTextBlock_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            try
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(e.Link));
            }
            catch(Exception ex)
            {

            }
        }

        // UI Interactions
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProgress.Visibility = Visibility.Visible;
            if (BlipEntryBox.Text != "")
            {
                if(SettingsHandler.Username == "")
                {
                    var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                    var page = (MainPage)grid.Parent;
                    page.ShowSystemMessage("You must log in to post.");
                }

                await host.CreateBlips(BlipEntryBox.Text.Replace("\r","\r\n"));

                var blips = await host.ListBlips();

                blips[0].body = await DTextConverter.ToMarkdown(blips[0].body);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    BlipsViewModel.Insert(0, blips[0]);
                });
            }
            LoadProgress.Visibility = Visibility.Collapsed;
        }
    }
}
