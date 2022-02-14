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
    public sealed partial class PoolViewPage : Page
    {
        PostNavigationArgs args;
        ObservableCollection<Post> PostsViewModel;
        Pool CurrentPool;
        RequestHost host;

        public PoolViewPage()
        {
            this.InitializeComponent();
            host = new RequestHost(SettingsHandler.UserAgent);
            PostsViewModel = new ObservableCollection<Post>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            args = PagesStack.ArgsStack[(int)e.Parameter];

            CurrentPool = args.ClickedPool;
            CurrentPool.name = CurrentPool.name.Replace("_", " ");

            PageText.Text = args.Page.ToString();

            if (args.PostsList == null)
            {
                PostsViewModel.Clear();
                Thread t = new Thread(GetPosts);
                t.Start();
            }
            else
            {
                PostsViewModel = new ObservableCollection<Post>(args.PostsList);
                LoadProgress.Visibility = Visibility.Collapsed;
            }

        }

        public async void GetPosts(object CurList)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                LeftNav.IsEnabled = false;
                RightNav.IsEnabled = false;
                LoadProgress.Visibility = Visibility.Visible;
            });

            // Convert the args from object to usable args.
            var PostsList = CurList as ObservableCollection<Post>;

            // Get Tags from search
            string tags = $"pool:{CurrentPool.id} order:id";

            // Get max Rating
            switch (SettingsHandler.Rating)
            {
                case "safe":
                    tags += $" rating:safe";
                    break;
            }

            // Login if creds are available 
            if (SettingsHandler.Username != "" && SettingsHandler.ApiKey != "")
            {
                host.Username = SettingsHandler.Username;
                host.ApiKey = SettingsHandler.ApiKey;
            }

            // Get Posts
            var posts = await host.GetPosts(tags, 300, args.Page);
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
                    posts = new List<Post>(args.PostsList);
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
                LeftNav.IsEnabled = true;
                RightNav.IsEnabled = true;
                LoadProgress.Visibility = Visibility.Collapsed;
            });
            args.PostsList = new ObservableCollection<Post>(PostsViewModel);
        }

        private void PostsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Get the post and setup the post nav args. this will need to change eventually
            Post p = e.ClickedItem as Post;
            PostNavigationArgs NewArgs = new PostNavigationArgs();
            NewArgs.ClickedPost = p;
            NewArgs.Page = args.Page;
            NewArgs.Tags = args.Tags;
            args.PostsList = new ObservableCollection<Post>(PostsViewModel);
            NewArgs.PostsList = new ObservableCollection<Post>(PostsViewModel);
            PagesStack.ArgsStack.Add(NewArgs);

            // Start navigation
            this.Frame.Navigate(typeof(SinglePostView), PagesStack.ArgsStack.Count() - 1, new DrillInNavigationTransitionInfo());
        }

        private void LeftNav_Click(object sender, RoutedEventArgs e)
        {
            if (args.Page > 1)
            {
                args.Page--;
                PostsViewModel.Clear();
                Thread t = new Thread(GetPosts);
                t.Start();
            }
        }

        private void RightNav_Click(object sender, RoutedEventArgs e)
        {
            args.Page++;
            PostsViewModel.Clear();
            Thread t = new Thread(GetPosts);
            t.Start();
        }

        private void GridViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {

        }

        private void DownloadPoolButton_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(AddPostsToDownloadQueue);
            t.Start();
        }

        private async void AddPostsToDownloadQueue()
        {
            int count = 1;
            int pageCount = 1;
            while (true)
            {
                // Get Tags from search
                string tags = $"pool:{CurrentPool.id} order:id";

                // Get max Rating
                if(SettingsHandler.Rating == "safe")
                {
                    tags += $" rating:safe";
                }

                // Login if creds are available 
                if (SettingsHandler.Username != "" && SettingsHandler.ApiKey != "")
                {
                    host.Username = SettingsHandler.Username;
                    host.ApiKey = SettingsHandler.ApiKey;
                }

                // Get Posts
                var posts = await host.GetPosts(tags, 300, pageCount);
                if (posts == null)
                {
                    return;
                }

                // If there are no posts, try to load the last list of posts.
                // This is for when your navigating to the last page of a search, so it doesn't load an empty page.
                if (posts.Count == 0)
                {
                    return;
                }

                foreach (var post in PostsViewModel)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                        var page = (MainPage)grid.Parent;
                        page.AddItemToQueue(new DownloadQueueItem() { PostToDownload = post, FileName = count.ToString("D2"), FolderName = CurrentPool.name.Replace("_"," ")});
                    });
                    count++;
                }
                pageCount++;
            }
        }
    }
}
