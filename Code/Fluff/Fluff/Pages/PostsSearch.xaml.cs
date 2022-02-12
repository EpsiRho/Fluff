using System;
using System.Collections.Generic;
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
using e6API;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media.Animation;
using Fluff.Classes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Fluff.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PostsSearch : Page
    {
        #region Global Page Vars
        ObservableCollection<Post> PostsViewModel;
        RequestHost host;
        bool CanSearch { get; set; }
        public double timeTillSearch;
        Thread TagsThread;
        string TagToSearch;
        bool CanGetTags;
        #endregion Global Page Vars

        #region Page Loading Functions
        public PostsSearch()
        {
            this.InitializeComponent();
            host = new RequestHost("Fluff/0.2(by EpsilonRho)");
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            PostsViewModel = new ObservableCollection<Post>();
            CanGetTags = true;
            SearchButtonPanel.Visibility = Visibility.Collapsed;
            SearchProgress.Visibility = Visibility.Visible;
            SearchButton.IsEnabled = false;
            PostsViewModel.Clear();
            PageText.Text = "1";

            Thread t = new Thread(GetPosts);
            t.Start();

            // TODO: Get Settings
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var gridViewItem = PostsView.ContainerFromItem(PostNavigationArgs.ClickedPost) as GridViewItem;

            ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("forwardAnimation");
            if (animation != null)
            {
               var fuck = animation.TryStart(gridViewItem);
            }
        }
        #endregion Page Loading Functions

        #region Post Grid Interactions
        private void PostsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Post p = e.ClickedItem as Post;
            PostNavigationArgs.ClickedPost = p;
            PostNavigationArgs.PostsList = PostsViewModel;

            var gridViewItem = PostsView.ContainerFromItem(e.ClickedItem) as GridViewItem;

            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("forwardAnimation", gridViewItem);
            this.Frame.Navigate(typeof(SinglePostView), null, new SuppressNavigationTransitionInfo());
        }
        private void GridViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //var item = PostsView.ContainerFromItem(e.ClickedItem) as GridViewItem;
            Storyboard sb = ((GridViewItem)sender).Resources["ShowImageBar"] as Storyboard;
            sb.Begin();
        }
        private void GridViewItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Storyboard sb = ((GridViewItem)sender).Resources["HideImageBar"] as Storyboard;
            sb.Begin();
        }
        #endregion Post Grid Interactions

        #region Thread Run Functions
        public async void GetPosts()
        {
            CanSearch = false;
            // Get Tags from search
            string tags = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                tags = SearchBox.Text;
            });

            // Get Modifiers from combobox
            string modifier = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                modifier = ((ComboBoxItem)SortSelection.SelectedItem).Content as string;
            });
            switch (modifier)
            {
                case "Oldest":
                    tags += " order:id";
                    break;
                case "Highest Score":
                    tags += " order:score";
                    break;
                case "Lowest Score":
                    tags += " order:score_asc";
                    break;
                case "Random":
                    tags += " order:random";
                    break;
            }

            // Get Post Count from slider
            int count = 75;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                count = (int)PostCountSelection.Value;
            });

            // Get Posts
            var posts = await host.GetPosts(tags, count, PostNavigationArgs.Page);

            if(posts.Count == 0)
            {
                posts = PostNavigationArgs.PostsList.ToList();
                if (PostNavigationArgs.Page > 1)
                {
                    PostNavigationArgs.Page--;
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    PageText.Text = PostNavigationArgs.Page.ToString();
                });
            }

            // Add posts to gridview
            foreach(var post in posts)
            {
                if (post.preview.url == null)
                {
                    //post.preview.url = "https://i.imgur.com/xH2ojWU.png";
                    continue;
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    PostsViewModel.Add(post);
                });
            }

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                SearchProgress.Visibility = Visibility.Collapsed;
                SearchButtonPanel.Visibility = Visibility.Visible;
                SearchButton.IsEnabled = true;
                LeftNav.IsEnabled = true;
                SortSelection.IsEnabled = true;
                PostCountSelection.IsEnabled = true;
                RightNav.IsEnabled = true;
            });
            CanSearch = true;
        }
        private void TimerThread()
        {
            timeTillSearch = 0.2;
            while (timeTillSearch > 0)
            {
                timeTillSearch -= 0.1;
                Thread.Sleep(100);
            }

            if(TagToSearch.Length > 3)
            {
                Thread TagsThread = new Thread(GetAutocompleteTags);
                TagsThread.Start(TagToSearch);

            }
            TagsThread = null;

        }
        private async void GetAutocompleteTags(object tag)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                SearchTagAutoComplete.Items.Clear();
            });
            var res = await host.SearchTags(tag as string, 8);

            foreach (Tag t in res)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    SearchTagAutoComplete.Items.Add(t.name);
                });
            }
        }
        #endregion

        #region Left Bar Interactions
        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SearchTagAutoComplete.Items.Clear();
                PostNavigationArgs.Page = 1;
                StartSearch();
            }
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            timeTillSearch = 1.5;
            string[] tags = SearchBox.Text.Split(" ");
            int index = 0;
            int count = 0;
            int pos = SearchBox.SelectionStart;
            for (int i = 0; i < tags.Count(); i++)
            {
                count += tags[i].Count();
                if (pos == count)
                {
                    index = i;
                    break;
                }
                count++;
            }
            if (tags[index].Count() >= 3)
            {
                if (TagsThread == null && CanGetTags)
                {
                    TagsThread = new Thread(TimerThread);
                    TagsThread.Start();
                }
                TagToSearch = tags[index];
            }
            else
            {
                SearchTagAutoComplete.Items.Clear();
            }
        }
        private void SearchTagAutoComplete_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                CanGetTags = false;
                string clickedTag = (string)e.ClickedItem;
                string[] tags = SearchBox.Text.Split(" ");
                int pos = SearchBox.SelectionStart;
                SearchBox.Text = "";
                int count = 0;
                for (int i = 0; i < tags.Count(); i++)
                {
                    count += tags[i].Count();
                    if (pos == count)
                    {
                        pos = (pos - tags[i].Count()) + clickedTag.Count() + 1;
                        if (tags[i][0] == '-')
                        {
                            tags[i] = "-" + clickedTag;
                        }
                        else
                        {
                            tags[i] = clickedTag;
                        }
                        SearchBox.Focus(FocusState.Programmatic);
                    }
                    SearchBox.Text += tags[i] + " ";
                    count++;
                }
                SearchTagAutoComplete.Items.Clear();
                SearchBox.SelectionStart = pos;
                CanGetTags = true;
            }
            catch (Exception)
            {

            }
        }
        private void SearchButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PostNavigationArgs.Page = 1;
            StartSearch();
        }
        private void SortSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PostNavigationArgs.Page = 1;
            StartSearch();
        }
        private void PostCountSelection_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            PostCountSelection.Value = 75;
        }
        private void LeftNav_Click(object sender, RoutedEventArgs e)
        {
            if (!CanSearch)
            {
                return;
            }
            if (PostNavigationArgs.Page > 1)
            {
                PostNavigationArgs.Page--;
                StartSearch();
            }
        }
        private void RightNav_Click(object sender, RoutedEventArgs e)
        {
            PostNavigationArgs.Page++;
            StartSearch();
        }
        #endregion Left Bar Interactions

        #region Helper & Common Code Functions
        public void StartSearch()
        {
            if (!CanSearch)
            {
                return;
            }
            SearchButtonPanel.Visibility = Visibility.Collapsed;
            SearchProgress.Visibility = Visibility.Visible;
            SearchButton.IsEnabled = false;
            SortSelection.IsEnabled = false;
            PostCountSelection.IsEnabled = false;
            LeftNav.IsEnabled = false;
            RightNav.IsEnabled = false;
            PostNavigationArgs.PostsList = new ObservableCollection<Post>(PostsViewModel);
            PageText.Text = PostNavigationArgs.Page.ToString();
            PostsViewModel.Clear();
            Thread t = new Thread(GetPosts);
            t.Start();
        }
        #endregion Helper & Common Code Functions















    }
}
