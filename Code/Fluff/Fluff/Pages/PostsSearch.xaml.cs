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
        // The collection of posts bound to the gridview
        ObservableCollection<Post> PostsViewModel;
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
        PostNavigationArgs args;
        #endregion Global Page Vars

        #region Page Loading Functions
        public PostsSearch()
        {
            this.InitializeComponent();
            host = new RequestHost("Fluff/0.5 (by EpsilonRho)"); // Initialize the api host
            PostsViewModel = new ObservableCollection<Post>(); // Initialize the post holder
            CanGetTags = true; // set this to start as true
            CanSearch = true;
            //StartSearch();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            args = PagesStack.ArgsStack[(int)e.Parameter];
            SearchBox.Text = args.Tags;
            PageText.Text = args.Page.ToString();
            if (args.PostsList == null)
            {
                StartSearch();
            }
            else
            {
                PostsViewModel = new ObservableCollection<Post>(args.PostsList);
            }
        }
        

        #endregion Page Loading Functions

        #region Post Grid Interactions
        private void PostsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Get the post and setup the post nav args. this will need to change eventually
            Post p = e.ClickedItem as Post;
            PostNavigationArgs NewArgs = new PostNavigationArgs();
            NewArgs.ClickedPost = p;
            NewArgs.Page = args.Page;
            NewArgs.Tags = SearchBox.Text;
            args.PostsList = new ObservableCollection<Post>(PostsViewModel);
            NewArgs.PostsList = new ObservableCollection<Post>(PostsViewModel);
            PagesStack.ArgsStack.Add(NewArgs);

            // Start navigation
            this.Frame.Navigate(typeof(SinglePostView), PagesStack.ArgsStack.Count()-1, new DrillInNavigationTransitionInfo());
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
        #endregion Post Grid Interactions

        #region Thread Run Functions
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
            tags += $" rating:{SettingsHandler.Rating}";

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
            int count = (int)SettingsHandler.PostCount;

            // Login if creds are available 
            if(SettingsHandler.Username != "" && SettingsHandler.ApiKey != "")
            {
                host.Username = SettingsHandler.Username;
                host.ApiKey = SettingsHandler.ApiKey;
            }

            // Get Posts
            var posts = await host.GetPosts(tags, count, args.Page);
            if(posts == null)
            {
                return;
            }

            // If there are no posts, try to load the last list of posts.
            // This is for when your navigating to the last page of a search, so it doesn't load an empty page.
            if(posts.Count == 0)
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
            foreach(var post in posts)
            {
                if (post.preview.url == null)
                {
                    //post.preview.url = "https://i.imgur.com/xH2ojWU.png";
                    continue;
                }

                bool vote = false;
                if(SettingsHandler.VotedPosts.TryGetValue(post.id, out vote))
                {
                    if(vote)
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
                SortSelection.IsEnabled = true;
                RightNav.IsEnabled = true;
                SearchTagAutoComplete.Items.Clear();
            });
            CanSearch = true;
        }
        private void TimerThread()
        {
            timeTillSearch = 0.3;
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
            var res = await host.GetAutocompletetags(tag as string);

            if(res == null)
            {
                return;
            }

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
                args.Page = 1;
                StartSearch();
            }
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            timeTillSearch = 0.3;
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
            if (!CanSearch)
            {
                return;
            }
            args.Page = 1;
            SearchBox.Text += " ";
            SearchTagAutoComplete.Items.Clear();
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

                foreach(var item in list)
                {
                    var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                    var page = (MainPage)grid.Parent;
                    page.AddItemToQueue(new DownloadQueueItem() { PostToDownload = item as Post });
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
            LeftNav.IsEnabled = false;
            RightNav.IsEnabled = false;
            PageText.Text = args.Page.ToString();
            Thread t = new Thread(GetPosts);
            t.Start(new ObservableCollection<Post>(PostsViewModel));
            PostsViewModel.Clear();
        }

        #endregion Helper & Common Code Functions

        #region Touch Interactions
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
        #endregion

    }
}
