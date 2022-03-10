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
using System.Reflection;
using Microsoft.AppCenter.Analytics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Fluff.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PostsSearch : Page
    {
        // Global Page Vars
        // The collection of posts bound to the gridview
        ObservableCollection<Post> PostsViewModel;
        ObservableCollection<Tag> TagsViewModel;
        // The Api host
        RequestHost host;
        // If searching is allowed, used to keep the filters from starting more searches on page load.
        bool CanSearch;
        // Used for grid swiping
        private bool _isGridSwiped;
        // Used to timeout tag autocomplete until the user stops typing.
        public double timeTillSearch;
        // The thread that waits for the user to stop typing
        Thread TagsThread;
        // The tag that will be searched when timeTillSearch = 0
        string TagToSearch;
        // If tags can be searched for
        bool CanGetTags;
        Post RightTappedPost;
        PostNavigationArgs args;

        // Page Loading Functions
        public PostsSearch()
        {
            this.InitializeComponent();
            host = new RequestHost(SettingsHandler.UserAgent); // Initialize the api host
            PostsViewModel = new ObservableCollection<Post>(); // Initialize the post holder
            TagsViewModel = new ObservableCollection<Tag>(); // Initialize the post holder
            CanGetTags = true; // set this to start as true
            CanSearch = true;
            //StartSearch();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                args = PagesStack.ArgsStack[(int) e.Parameter];
                SearchBox.Text = args.Tags;
                PageText.Text = args.Page.ToString();
                if (args.IsPopDay)
                {
                    PopDayButton.IsChecked = true;
                }
                else if (args.IsPopWeek)
                {
                    PopWeekButton.IsChecked = true;
                }
                else if (args.IsPopMonth)
                {
                    PopMonthButton.IsChecked = true;
                }

                if (args.PostsList != null)
                {
                    SortSelection.SelectedIndex = args.SortOrder;
                }
            }
            catch (Exception ex)
            {

            }
        }

        // Post Grid Interactions
        private void PostsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Get the post and setup the post nav args. this will need to change eventually
            Post p = e.ClickedItem as Post;
            PostNavigationArgs NewArgs = new PostNavigationArgs();
            NewArgs.ClickedPost = p;
            NewArgs.Page = args.Page;
            NewArgs.Tags = SearchBox.Text;
            args.Tags = SearchBox.Text;
            args.PostsList = new ObservableCollection<Post>(PostsViewModel);
            args.ClickedPost = p;
            args.SortOrder = SortSelection.SelectedIndex;
            args.IsPopDay = PopDayButton.IsChecked.Value;
            args.IsPopWeek = PopWeekButton.IsChecked.Value;
            args.IsPopMonth = PopMonthButton.IsChecked.Value;
            args.ScrollPercent = PostsScrollView.VerticalOffset;
            NewArgs.PostsList = new ObservableCollection<Post>(PostsViewModel);
            PagesStack.ArgsStack.Add(NewArgs);
            ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", (UIElement)PostsView.ContainerFromItem(p));
            animation.Configuration = new BasicConnectedAnimationConfiguration();


            // Start navigation
            this.Frame.Navigate(typeof(SinglePostView), PagesStack.ArgsStack.Count() - 1, new DrillInNavigationTransitionInfo());
        }
        private void GridViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Start the animation to show the like / fav bar
            Storyboard sb = ((GridViewItem)sender).Resources["ShowImageBar"] as Storyboard;
            sb.Begin();
        }
        private void GridViewItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Start the animation to hide the like / fav bar
            Storyboard sb = ((GridViewItem)sender).Resources["HideImageBar"] as Storyboard;
            sb.Begin();
        }
        private void PostsView_Loaded(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(ScrollThread);
            t.Start();
        }

        private async void ScrollThread()
        {
            if (args.PostsList == null)
            {
                StartSearch();
            }
            else
            {
                foreach (var post in args.PostsList)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                    {
                        PostsViewModel.Add(post);
                    });
                }

                bool check = false;
                while (!check)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                    {
                        check = PostsScrollView.ChangeView(0, args.ScrollPercent, 1);
                    });
                }
            }
        }

        private void PostsView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            RightTappedPost = (sender as GridViewItem).DataContext as Post;

            if (RightTappedPost != null)
            {
                MenuFlyout flyout = new MenuFlyout();
                MenuFlyoutItem item1 = new MenuFlyoutItem();
                item1.Text = "Like Post";
                item1.Click += LikeTapped;
                flyout.Items.Add(item1);
                MenuFlyoutItem item2 = new MenuFlyoutItem();
                item2.Text = "Dislike Post";
                item2.Click += DislikeTapped;
                flyout.Items.Add(item2);
                MenuFlyoutItem item3 = new MenuFlyoutItem();
                item3.Text = "Favorite Post";
                item3.Click += FavTapped;
                flyout.Items.Add(item3);
                MenuFlyoutItem item4 = new MenuFlyoutItem();
                item4.Text = "Download Post";
                item4.Click += DownloadTapped;
                flyout.Items.Add(item4);
                flyout.ShowAt(PostsView, e.GetPosition(PostsView));
            }
        }
        private void LikeTapped(object sender, RoutedEventArgs e)
        {
            host.VotePost(RightTappedPost.id, 1);
        }
        private void DislikeTapped(object sender, RoutedEventArgs e)
        {
            host.VotePost(RightTappedPost.id, -1);
        }
        private void FavTapped(object sender, RoutedEventArgs e)
        {
            host.FavoritePost(RightTappedPost.id);
        }
        private void DownloadTapped(object sender, RoutedEventArgs e)
        {
            var grid = ((Grid)this.Frame.Parent).Parent as Grid;
            var page = (MainPage)grid.Parent;
            page.AddItemToQueue(new DownloadQueueItem() { PostToDownload = RightTappedPost, FileName = RightTappedPost.file.md5, FolderName = "" });
        }

        // Thread Run Functions
        public async void GetPosts(object CurList)
        {
            // Convert the args from object to usable args.
            var PostsList = CurList as ObservableCollection<Post>;

            // Disable extra searches
            CanSearch = false;

            // Get Tags from search
            string tags = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                tags = SearchBox.Text;
            });

            // Get max Rating
            if (SettingsHandler.Rating == "safe")
            {
                tags += $" rating:safe";
            }

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

            // Get Modifiers from combobox
            string minScore = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                minScore = MinScoreBox.Text;
            });
            if (minScore != "")
            {
                tags += $" score:>{minScore}";
            }

            // Get blacklist
            host.Username = SettingsHandler.Username;
            host.ApiKey = SettingsHandler.ApiKey;
            var users = await host.SearchUsers(SettingsHandler.Username, 1);
            string[] blacklist = users[0].blacklisted_tags.Replace("\r", "").Split("\n");
            if (blacklist != null)
            {
                foreach (string tag in blacklist)
                {
                    tags += $" -{tag}";
                }
            }

            // Get Post Count from slider
            int count = (int)SettingsHandler.PostCount;

            // Login if creds are available 
            if (SettingsHandler.Username != "" && SettingsHandler.ApiKey != "")
            {
                host.Username = SettingsHandler.Username;
                host.ApiKey = SettingsHandler.ApiKey;
            }

            // Get Posts
            List<Post> posts = null;
            bool Day = false;
            bool Week = false;
            bool Month = false;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Day = PopDayButton.IsChecked.Value;
                Week = PopWeekButton.IsChecked.Value;
                Month = PopMonthButton.IsChecked.Value;
            });
            if (Day)
            {
                posts = await host.GetPopularPosts(PopularPosts.Day, count, args.Page);
            }
            else if (Week)
            {
                posts = await host.GetPopularPosts(PopularPosts.Week, count, args.Page);
            }
            else if (Month)
            {
                posts = await host.GetPopularPosts(PopularPosts.Month, count, args.Page);
            }
            else
            {
                posts = await host.GetPosts(tags, count, args.Page);
            }

            if (posts == null)
            {
                return;
            }

            // If there are no posts, try to load the last list of posts.
            // This is for when your navigating to the last page of a search, so it doesn't load an empty page.
            if (posts.Count == 0)
            {
                if (args.Page > 1)
                {
                    posts = PostsList.ToList();
                    args.Page--;
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    PageText.Text = args.Page.ToString();
                });
            }

            // Add posts to gridview
            foreach (var post in posts)
            {
                if (post.preview.url == null)
                {
                    //post.preview.url = "https://i.imgur.com/xH2ojWU.png";
                    continue;
                }

                bool vote = false;
                if (SettingsHandler.VotedPosts.TryGetValue(post.id, out vote))
                {
                    if (vote)
                    {
                        post.voted_up = true;
                    }
                    else
                    {
                        post.voted_down = true;
                    }
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    PostsViewModel.Add(post);
                });
            }

            // Set controls to be enabled again.
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                SearchProgress.Visibility = Visibility.Collapsed;
                SearchButtonPanel.Visibility = Visibility.Visible;
                SearchButton.IsEnabled = true;
                LeftNav.IsEnabled = true;
                if (!PopDayButton.IsChecked.Value && !PopMonthButton.IsChecked.Value && !PopWeekButton.IsChecked.Value)
                {
                    SearchBox.IsEnabled = true;
                }

                SortSelection.IsEnabled = true;
                RightNav.IsEnabled = true;
            });
            CanSearch = true;
        }

        private void TimerThread()
        {
            while (timeTillSearch > 0)
            {
                timeTillSearch -= 0.1;
                Thread.Sleep(100);
            }

            if (TagToSearch.Length > 3)
            {
                Thread TagsThread = new Thread(GetAutocompleteTags);
                TagsThread.Start(TagToSearch);
            }
            TagsThread = null;

        }
        private async void GetAutocompleteTags(object tag)
        {
            var res = await host.GetAutocompletetags(tag as string);
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                TagsViewModel.Clear();
            });

            if (res == null)
            {
                return;
            }

            foreach (Tag t in res)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    TagsViewModel.Add(t);
                });
            }
            
        }
        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            string[] split = sender.Text.Split(' ');
            sender.Text = sender.Text.Replace(split[split.Length - 1], "");
            sender.Text += ((Tag)args.SelectedItem).name;
        }
        private async void GetRecommended()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                SearchBox.IsEnabled = false;
                SearchButtonPanel.Visibility = Visibility.Collapsed;
                SearchProgress.Visibility = Visibility.Visible;
                SearchButton.IsEnabled = false;
                SortSelection.IsEnabled = false;
                LeftNav.IsEnabled = false;
                RightNav.IsEnabled = false;
                PageText.Text = "R";
                PostsViewModel.Clear();
            });
            var assembly = this.GetType().GetTypeInfo().Assembly;
            var resource = assembly.GetManifestResourceStream("Fluff.Assets.BannedTags.txt");

            byte[] buffer = new byte[resource.Length];
            resource.Read(buffer, 0, (int)resource.Length);

            string response = System.Text.Encoding.Default.GetString(buffer);

            List<string> bannedtags = response.Split("\n").ToList();

            host.Username = SettingsHandler.Username;
            host.ApiKey = SettingsHandler.ApiKey;
            var posts = await host.GetPosts($"votedup:EpsilonRho", 300);

            Dictionary<string, int> Counts = new Dictionary<string, int>();
            foreach (var post in posts)
            {
                foreach (var tag in post.tags.general)
                {
                    var check = Counts.TryAdd(tag, 1);
                    if (!check)
                    {
                        Counts[tag]++;
                    }
                }
                foreach (var tag in post.tags.character)
                {
                    var check = Counts.TryAdd(tag, 1);
                    if (!check)
                    {
                        Counts[tag]++;
                    }
                }
                foreach (var tag in post.tags.copyright)
                {
                    var check = Counts.TryAdd(tag, 1);
                    if (!check)
                    {
                        Counts[tag]++;
                    }
                }
                foreach (var tag in post.tags.artist)
                {
                    var check = Counts.TryAdd(tag, 1);
                    if (!check)
                    {
                        Counts[tag]++;
                    }
                }
            }


            var sortedDict = from entry in Counts orderby entry.Value descending select entry;

            var sortedList = sortedDict.ToList();

            var FinalList = new List<KeyValuePair<string, int>>();
            foreach (var item in sortedList)
            {
                if (!bannedtags.Contains(item.Key))
                {
                    FinalList.Add(item);
                }
            }

            string tags = "";
            Random rand = new Random();
            for (int i = 0; i < 10; i++)
            {
                int idx = rand.Next(FinalList.Count / 4);
                tags += $"~{FinalList[idx].Key} ";
            }

            tags += "order:random score:>150";

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                SearchBox.Text = tags;
                args.Tags = tags;
                PageText.Text = "R";
                PostsViewModel.Clear();
                StartSearch();
            });
        }
        
        // Left Panel Interactions
        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                timeTillSearch = 0.3;
                if(TagsThread == null)
                {
                    TagsThread = new Thread(TimerThread);
                    TagsThread.Start();
                }
                string[] split = sender.Text.Split(' ');
                TagToSearch = split[split.Length - 1];
            }
        }
        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                args.Page = 1;
                StartSearch();
            }
        }
        private void SearchButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!CanSearch)
            {
                return;
            }
            args.Page = 1;
            SearchBox.Text += "";
            StartSearch();
        }
        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            this.args.Page = 1;
            StartSearch();
        }
        private void SortSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!CanSearch)
            {
                return;
            }
            args.Page = 1;
            StartSearch();
        }
        private void DownloadMultiple_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadMultiple.IsChecked.Value)
            {
                PostsView.IsItemClickEnabled = false;
                PostsView.SelectionMode = ListViewSelectionMode.Multiple;

            }
            else
            {
                var list = new List<object>(PostsView.SelectedItems);
                PostsView.IsItemClickEnabled = true;
                PostsView.SelectionMode = ListViewSelectionMode.None;

                foreach (var item in list)
                {
                    var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                    var page = (MainPage)grid.Parent;
                    page.AddItemToQueue(new DownloadQueueItem() { PostToDownload = item as Post, FileName = (item as Post).file.md5, FolderName = "" });
                }
            }

        }
        private void LeftNav_Click(object sender, RoutedEventArgs e)
        {
            if (!CanSearch)
            {
                return;
            }
            if (args.Page > 1)
            {
                args.Page--;
                StartSearch();
            }
        }
        private void RightNav_Click(object sender, RoutedEventArgs e)
        {
            args.Page++;
            StartSearch();
        }
        private void RecommendButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsHandler.Username == "")
            {
                var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                var page = (MainPage)grid.Parent;
                page.ShowSystemMessage("You need to be logged in to use this feature");
                return;
            }

            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var str = (string)localSettings.Values["SeenRecommendInfo"];
            if (str == null)
            {
                localSettings.Values["SeenRecommendInfo"] = "Seen";
                RecommendedPanel.Visibility = Visibility.Visible;
            }
            else
            {
                PopDayButton.IsChecked = false;
                PopWeekButton.IsChecked = false;
                PopMonthButton.IsChecked = false;
                Thread t = new Thread(GetRecommended);
                t.Start();

            }
        }
        private void TagsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            string str = e.ClickedItem as string;
            if (SearchBox.Text.Contains(str))
            {
                SearchBox.Text = SearchBox.Text.Replace($" {str}","");
                SearchBox.Text = SearchBox.Text.Replace(str,"");
                return;
            }

            if(SearchBox.Text.Count() == 0)
            {
                SearchBox.Text += $"{str}";
            }
            else
            {
                SearchBox.Text += $" {str}";
            }
        }
        private void AddTagFavButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsHandler.FavoriteTags.Add(AddTagFavBox.Text);
            SettingsHandler.SaveFavsToFile();
            Bindings.Update();
            AddTagFavBox.Text = "";
            AddNewTagFlyout.Hide();
        }
        private void AddTagFavBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            SettingsHandler.FavoriteTags.Add(sender.Text);
            SettingsHandler.SaveFavsToFile();
            Bindings.Update();
            sender.Text = "";
            AddNewTagFlyout.Hide();
        }
        private void AddNewTagOnTapped(object sender, TappedRoutedEventArgs e)
        {
            (sender as UIElement).ContextFlyout.ShowAt((FrameworkElement)sender);
        }
        private void FavTagMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if(e.OriginalSource != null)
            {
                string tag = ((MenuFlyoutItem)e.OriginalSource).Tag.ToString();
                SettingsHandler.FavoriteTags.Remove(tag);
                SettingsHandler.SaveFavsToFile();
                Bindings.Update();
            }
        }

        // Helper & Common Code Functions
        public async void StartSearch()
        {
            if (!CanSearch)
            {
                return;
            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                SearchBox.IsEnabled = false;
                SearchProgress.Visibility = Visibility.Visible;
                SearchButton.IsEnabled = false;
                SortSelection.IsEnabled = false;
                LeftNav.IsEnabled = false;
                RightNav.IsEnabled = false;
                PageText.Text = args.Page.ToString();
                PostsViewModel.Clear();
            });
            Thread t = new Thread(GetPosts);
            t.Start(new ObservableCollection<Post>(PostsViewModel));
        }

        // Swipe Interactions
        private void PostsView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e.IsInertial && !_isGridSwiped)
            {
                var swipedDistance = e.Cumulative.Translation.X;

                if (Math.Abs(swipedDistance) <= 200) return;

                if (swipedDistance < 0)
                {
                    try
                    {
                        if (PostsViewModel.Count > 0)
                        {
                            args.Page++;
                            StartSearch();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
                else
                {
                    try
                    {
                        if (args.Page != 1)
                        {
                            args.Page--;
                            StartSearch();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
                _isGridSwiped = true;
            }
        }
        private void PostsView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            _isGridSwiped = false;
        }
        
        // Feature Introductions Function
        private void RecommendPanelOkay_Click(object sender, RoutedEventArgs e)
        {
            RecommendedPanel.Visibility = Visibility.Collapsed;
        }

        private void PopDayButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsHandler.Rating == "safe")
            {
                var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                var page = (MainPage)grid.Parent;
                page.ShowSystemMessage("Can't view in safe mode!");
                PopDayButton.IsChecked = false;
                PopWeekButton.IsChecked = false;
                PopMonthButton.IsChecked = false;

                return;
            }
            if (PopDayButton.IsChecked.Value)
            {
                PopWeekButton.IsChecked = false;
                PopMonthButton.IsChecked = false;
                SearchBox.IsEnabled = false;
            }
            else
            {
                SearchBox.IsEnabled = true;
            }
            StartSearch();
        }
        private void PopWeekButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsHandler.Rating == "safe")
            {
                var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                var page = (MainPage)grid.Parent;
                page.ShowSystemMessage("Can't view in safe mode!");
                PopDayButton.IsChecked = false;
                PopWeekButton.IsChecked = false;
                PopMonthButton.IsChecked = false;

                return;
            }
            if (PopWeekButton.IsChecked.Value)
            {
                PopDayButton.IsChecked = false;
                PopMonthButton.IsChecked = false;
                SearchBox.IsEnabled = false;
            }
            else
            {
                SearchBox.IsEnabled = true;
            }
            StartSearch();
        }
        private void PopMonthButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsHandler.Rating == "safe")
            {
                var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                var page = (MainPage)grid.Parent;
                page.ShowSystemMessage("Can't view in safe mode!");
                PopDayButton.IsChecked = false;
                PopWeekButton.IsChecked = false;
                PopMonthButton.IsChecked = false;

                return;
            }
            if (PopMonthButton.IsChecked.Value)
            {
                PopDayButton.IsChecked = false;
                PopWeekButton.IsChecked = false;
                SearchBox.IsEnabled = false;
            }
            else
            {
                SearchBox.IsEnabled = true;
            }
            StartSearch();
        }

        
    }
}
