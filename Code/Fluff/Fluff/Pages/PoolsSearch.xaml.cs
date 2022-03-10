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
        // Pages Vars
        ObservableCollection<Pool> PoolsViewModel;
        ObservableCollection<Set> SetsViewModel;
        RequestHost host;
        PostNavigationArgs args;

        // Loading Functions
        public PoolsSearch()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            args = PagesStack.ArgsStack[(int)e.Parameter];
            host = new RequestHost(SettingsHandler.UserAgent);
            PageText.Text = args.Page.ToString();
            PoolsViewModel = new ObservableCollection<Pool>();
            SetsViewModel = new ObservableCollection<Set>();

            if (args.IsSetSearch)
            {
                if (args.SetsList == null)
                {
                    SetsViewModel.Clear();
                    Thread t = new Thread(GetSets);
                    t.Start();
                }
                else
                {
                    SetsViewModel = new ObservableCollection<Set>(args.SetsList);
                    SearchProgress.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                SetSearchListView.Visibility = Visibility.Collapsed;
                if (args.PoolsList == null)
                {
                    PoolsViewModel.Clear();
                    Thread t = new Thread(GetPools);
                    t.Start();
                }
                else
                {
                    PoolsViewModel = new ObservableCollection<Pool>(args.PoolsList);
                    SearchProgress.Visibility = Visibility.Collapsed;
                }
            }
            
        }

        // Api Functions
        private async void GetPools()
        {
            try
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

                var pools = await host.SearchPoolNames(tags, 75, args.Page);

                foreach (var pool in pools)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        PoolsViewModel.Add(pool);
                    });
                }
                args.PoolsList = new ObservableCollection<Pool>(PoolsViewModel);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    SearchProgress.Visibility = Visibility.Collapsed;
                    SearchButtonPanel.Visibility = Visibility.Visible;
                });
            }
            catch (Exception)
            {

            }
        }
        private async void GetSets()
        {
            try
            {
                // Get Tags from search
                string tags = "";
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    tags = SearchBox.Text;
                    SetsViewModel.Clear();
                    SearchProgress.Visibility = Visibility.Visible;
                    SearchButtonPanel.Visibility = Visibility.Collapsed;
                });

                // Login if creds are available 
                if (SettingsHandler.Username != "" && SettingsHandler.ApiKey != "")
                {
                    host.Username = SettingsHandler.Username;
                    host.ApiKey = SettingsHandler.ApiKey;
                }

                var sets = await host.SearchSets(tags, 75, args.Page);

                foreach (var set in sets)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        SetsViewModel.Add(set);
                    });
                }
                args.SetsList = new ObservableCollection<Set>(SetsViewModel);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    SearchProgress.Visibility = Visibility.Collapsed;
                    SearchButtonPanel.Visibility = Visibility.Visible;
                });
            }
            catch (Exception)
            {

            }
        }

        // UI Interactions
        private void SearchListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            PostNavigationArgs NewArgs = new PostNavigationArgs();
            NewArgs.Page = 1;
            NewArgs.ClickedPool = (Pool)e.ClickedItem;
            NewArgs.Tags = args.Tags;
            args.ScrollPercent = ItemsScrollView.VerticalOffset;
            PagesStack.ArgsStack.Add(NewArgs);

            this.Frame.Navigate(typeof(PoolViewPage), PagesStack.ArgsStack.Count() - 1, new DrillInNavigationTransitionInfo());
        }
        private void LeftNav_Click(object sender, RoutedEventArgs e)
        {
            if (args.Page > 1)
            {
                args.Page--;
                PageText.Text = args.Page.ToString();
                if (args.IsSetSearch)
                {
                    Thread t = new Thread(GetSets);
                    t.Start();
                }
                else
                {
                    Thread t = new Thread(GetPools);
                    t.Start();
                }
            }
        }
        private void RightNav_Click(object sender, RoutedEventArgs e)
        {
            args.Page++;
            PageText.Text = args.Page.ToString();
            if (args.IsSetSearch)
            {
                Thread t = new Thread(GetSets);
                t.Start();
            }
            else
            {
                Thread t = new Thread(GetPools);
                t.Start();
            }
        }
        private void SearchButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            args.Page = 1;
            PageText.Text = args.Page.ToString();
            if (args.IsSetSearch)
            {
                Thread t = new Thread(GetSets);
                t.Start();
            }
            else
            {
                Thread t = new Thread(GetPools);
                t.Start();
            }
        }
        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                args.Page = 1;
                PageText.Text = args.Page.ToString();
                if (args.IsSetSearch)
                {
                    Thread t = new Thread(GetSets);
                    t.Start();
                }
                else
                {
                    Thread t = new Thread(GetPools);
                    t.Start();
                }
            }
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void SetSearchListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            PostNavigationArgs NewArgs = new PostNavigationArgs();
            NewArgs.Page = 1;
            NewArgs.ClickedSet = (Set)e.ClickedItem;
            NewArgs.Tags = args.Tags;
            NewArgs.IsSetSearch = true;
            args.ScrollPercent = ItemsScrollView.VerticalOffset;
            PagesStack.ArgsStack.Add(NewArgs);

            this.Frame.Navigate(typeof(PoolViewPage), PagesStack.ArgsStack.Count() - 1, new DrillInNavigationTransitionInfo());

        }

        private void ItemsScrollView_OnLoaded(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(ScrollThread);
            t.Start();
        }

        private async void ScrollThread()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                ItemsScrollView.ChangeView(0, args.ScrollPercent, 1);
            });
        }
    }
}
