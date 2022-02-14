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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Fluff.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PoolsSearch : Page
    {
        ObservableCollection<Pool> PoolsViewModel;
        RequestHost host;
        PostNavigationArgs args;
        public PoolsSearch()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            args = PagesStack.ArgsStack[(int)e.Parameter];
            host = new RequestHost("Fluff/0.5 (by EpsilonRho)");
            PoolsViewModel = new ObservableCollection<Pool>();

            Thread t = new Thread(GetPools);
            t.Start();
        }

        private async void GetPools()
        {
            // Get Tags from search
            string tags = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                tags = SearchBox.Text;
                PoolsViewModel.Clear();
                SearchProgress.Visibility = Visibility.Visible;
                SearchButtonPanel.Visibility = Visibility.Collapsed;
            });

            // Login if creds are available 
            if (SettingsHandler.Username != "" && SettingsHandler.ApiKey != "")
            {
                host.Username = SettingsHandler.Username;
                host.ApiKey = SettingsHandler.ApiKey;
            }

            var pools = await host.SearchPoolNames(tags);

            foreach(var pool in pools)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    PoolsViewModel.Add(pool);
                });
            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                SearchProgress.Visibility = Visibility.Collapsed;
                SearchButtonPanel.Visibility = Visibility.Visible;
            });
        }

        private void SearchListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            PostNavigationArgs NewArgs = new PostNavigationArgs();
            NewArgs.Page = 1;
            NewArgs.ClickedPool = (Pool)e.ClickedItem;
            NewArgs.Tags = args.Tags;
            PagesStack.ArgsStack.Add(NewArgs);

            this.Frame.Navigate(typeof(PoolViewPage), PagesStack.ArgsStack.Count() - 1, new DrillInNavigationTransitionInfo());
        }

        private void RightNav_Click(object sender, RoutedEventArgs e)
        {
            args.Page--;
            Thread t = new Thread(GetPools);
            t.Start();
        }

        private void LeftNav_Click(object sender, RoutedEventArgs e)
        {
            if (args.Page > 1)
            {
                args.Page--;
                Thread t = new Thread(GetPools);
                t.Start();
            }
        }

        private void SearchButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            args.Page = 1;
            Thread t = new Thread(GetPools);
            t.Start();
        }

        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                args.Page = 1;
                Thread t = new Thread(GetPools);
                t.Start();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void SearchTagAutoComplete_ItemClick(object sender, ItemClickEventArgs e)
        {

        }
    }
}
