using e6API;
using Fluff.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Fluff.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SinglePostView : Page
    {

        #region Global Page Vars
        ObservableCollection<Post> PostsViewModel;
        ObservableCollection<Pool> PoolsSource;
        CurrentPostNotif PostHandler;
        RequestHost host;
        PostNavigationArgs args;
        string ClickedTag;
        #endregion Global Page Vars

        #region Page Loading Functions
        public SinglePostView()
        {
            this.InitializeComponent();
            host = new RequestHost(SettingsHandler.UserAgent); // Initialize the api host
            if(SettingsHandler.Username != "")
            {
                host.Username = SettingsHandler.Username;
                host.ApiKey = SettingsHandler.ApiKey;
            }
            PostsViewModel = new ObservableCollection<Post>();
            PostHandler = new CurrentPostNotif();
            PoolsSource = new ObservableCollection<Pool>();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            args = PagesStack.ArgsStack[(int)e.Parameter];
            PostsViewModel = new ObservableCollection<Post>(args.PostsList);
            PostFlipView.SelectedIndex = PostsViewModel.IndexOf(args.ClickedPost);
            PostHandler.CurrentPost = args.ClickedPost;

            Thread t = new Thread(SetVoteColors);
            t.Start();
        }

        private async void SetVoteColors()
        {
            if (PostHandler.CurrentPost.voted_up)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    LikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    DislikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                });
            }
            else if (PostHandler.CurrentPost.voted_down)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    LikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    DislikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                });
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    LikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    DislikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                });
            }

            if (PostHandler.CurrentPost.is_favorited)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    FavoriteButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 94, 255, 255));
                });
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    FavoriteButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                });
            }
        }
        #endregion Page Loading Functions

        #region ToolBar Interactions
        private void MouseHoverHandler_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ShowControls.Begin();
        }
        private void MouseHoverHandler_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            HideControls.Begin();
        }
        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            var x = await host.VotePost(PostHandler.CurrentPost.id, 1);
            if(x == null)
            {
                var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                var page = (MainPage)grid.Parent;
                page.ShowSystemMessage("You must be logged in to vote!");
                return;
            }
            if (x.our_score == 1)
            {
                LikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                DislikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                bool vote;
                if (SettingsHandler.VotedPosts.TryGetValue(PostHandler.CurrentPost.id, out vote))
                {
                    SettingsHandler.VotedPosts[PostHandler.CurrentPost.id] = true;
                }
                else
                {
                    SettingsHandler.VotedPosts.Add(PostHandler.CurrentPost.id, true);
                }
                PostHandler.CurrentPost.voted_up = true;
                PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].voted_up = true;
                PostHandler.CurrentPost.voted_down = false;
                PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].voted_down = false;
            }
            else
            {
                LikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                SettingsHandler.VotedPosts.Remove(PostHandler.CurrentPost.id);
                PostHandler.CurrentPost.voted_up = false;
                PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].voted_up = false;
            }
            PostHandler.CurrentPost.score.up = x.up;
            PostHandler.CurrentPost.score.down = x.down;
        }
        private async void DislikeButton_Click(object sender, RoutedEventArgs e)
        {
            var x = await host.VotePost(PostHandler.CurrentPost.id, -1);
            if (x == null)
            {
                var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                var page = (MainPage)grid.Parent;
                page.ShowSystemMessage("You must be logged in to vote!");
                return;
            }
            if (x.our_score == -1)
            {
                LikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                DislikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                bool vote;
                if (SettingsHandler.VotedPosts.TryGetValue(PostHandler.CurrentPost.id, out vote))
                {
                    SettingsHandler.VotedPosts[PostHandler.CurrentPost.id] = false;
                }
                else
                {
                    SettingsHandler.VotedPosts.Add(PostHandler.CurrentPost.id, false);
                }
                PostHandler.CurrentPost.voted_up = false;
                PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].voted_up = false;
                PostHandler.CurrentPost.voted_down = true;
                PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].voted_down = true;
            }
            else
            {
                DislikeButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                SettingsHandler.VotedPosts.Remove(PostHandler.CurrentPost.id);
                PostHandler.CurrentPost.voted_down = false;
                PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].voted_down = false;
            }
            PostHandler.CurrentPost.score.up = x.up;
            PostHandler.CurrentPost.score.down = x.down;
        }
        #endregion ToolBar Functions

        #region Image and FlipView Interactions
        private void PostFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PostHandler.CurrentPost = e.AddedItems[0] as Post;
            Thread t = new Thread(SetVoteColors);
            t.Start();

            Thread g = new Thread(GetPools);
            g.Start();

            ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("forwardAnimation");
            if (animation != null)
            {
                animation.TryStart(PostFlipView);
            }
        }
        private async void GetPools()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                PoolsSource.Clear();
            });
            foreach (var pool in PostHandler.CurrentPost.pools) 
            {
                var info = await host.GetPoolInfo(pool);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    PoolsSource.Add(info);
                });
            }
        }
        #endregion Image and FlipView Interactions

        #region Tags Interactions 
        private void TagList_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                ClickedTag = e.ClickedItem as string;
                MenuFlyout myFlyout = new MenuFlyout();
                MenuFlyoutItem Item5 = new MenuFlyoutItem { Text = "Search Tag" };
                MenuFlyoutItem Item1 = new MenuFlyoutItem { Text = "Add tag to search" };
                MenuFlyoutItem Item2 = new MenuFlyoutItem { Text = "Filter tag from search" };
                MenuFlyoutItem Item3 = new MenuFlyoutItem { Text = "Favorite tag" };
                MenuFlyoutItem Item4 = new MenuFlyoutItem { Text = "View Wiki Entry" };
                Item5.Click += new RoutedEventHandler(SearchTag);
                Item1.Click += new RoutedEventHandler(AddTagToSearch);
                Item2.Click += new RoutedEventHandler(FliterTagFromSearch);
                Item3.Click += new RoutedEventHandler(FavoriteTag);
                Item4.Click += new RoutedEventHandler(ViewWikiEntry);
                myFlyout.Items.Add(Item5);
                myFlyout.Items.Add(Item1);
                myFlyout.Items.Add(Item2);
                myFlyout.Items.Add(Item3);
                myFlyout.Items.Add(Item4);
                var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;

                pointerPosition.X = pointerPosition.X - Window.Current.Bounds.X;
                pointerPosition.Y = pointerPosition.Y - Window.Current.Bounds.Y;
                pointerPosition.X -= 65;
                pointerPosition.Y += 15;

                myFlyout.ShowAt(((Grid)this.Frame.Parent).Parent as UIElement, pointerPosition);
            }
            catch (Exception)
            {

            }
        }
        private void Title_Click(object sender, RoutedEventArgs e)
        {
            HyperlinkButton ClickedItem = e.OriginalSource as HyperlinkButton;
            switch (ClickedItem.Content)
            {
                case "Artists:":
                    if (ArtistsTags.Visibility == Visibility.Collapsed)
                    {
                        ArtistsTags.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ArtistsTags.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "Copyrights:":
                    if (CopyrightsTags.Visibility == Visibility.Collapsed)
                    {
                        CopyrightsTags.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        CopyrightsTags.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "Characters:":
                    if (CharactersTags.Visibility == Visibility.Collapsed)
                    {
                        CharactersTags.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        CharactersTags.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "Species:":
                    if (SpeciesTags.Visibility == Visibility.Collapsed)
                    {
                        SpeciesTags.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        SpeciesTags.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "General:":
                    if (GeneralTags.Visibility == Visibility.Collapsed)
                    {
                        GeneralTags.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        GeneralTags.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "Meta:":
                    if (MetaTags.Visibility == Visibility.Collapsed)
                    {
                        MetaTags.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MetaTags.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "Lore:":
                    if (LoreTags.Visibility == Visibility.Collapsed)
                    {
                        LoreTags.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        LoreTags.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "Invalid:":
                    if (InvalidTags.Visibility == Visibility.Collapsed)
                    {
                        InvalidTags.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        InvalidTags.Visibility = Visibility.Collapsed;
                    }
                    break;
            }
        }
        private void AddTagToSearch(object sender, RoutedEventArgs e)
        {
            PostNavigationArgs NewArgs = new PostNavigationArgs();
            NewArgs.Tags = args.Tags;
            if (NewArgs.Tags.Contains($"-{ClickedTag}"))
            {
                NewArgs.Tags = NewArgs.Tags.Replace($"-{ClickedTag}","");
            }

            if (NewArgs.Tags.Contains(ClickedTag))
            {
                return;
            }
            NewArgs.Tags += $" {ClickedTag}";
            NewArgs.Page = 1;
            args.ClickedPost = PostHandler.CurrentPost;
            PagesStack.ArgsStack.Add(NewArgs);
            this.Frame.Navigate(typeof(PostsSearch), PagesStack.ArgsStack.Count() - 1, new EntranceNavigationTransitionInfo());
        }
        private void SearchTag(object sender, RoutedEventArgs e)
        {
            PostNavigationArgs NewArgs = new PostNavigationArgs();
            NewArgs.Tags = ClickedTag;
            NewArgs.Page = 1;
            args.ClickedPost = PostHandler.CurrentPost;
            PagesStack.ArgsStack.Add(NewArgs);

            this.Frame.Navigate(typeof(PostsSearch), PagesStack.ArgsStack.Count() - 1, new EntranceNavigationTransitionInfo());
        }
        private void FliterTagFromSearch(object sender, RoutedEventArgs e)
        {
            PostNavigationArgs NewArgs = new PostNavigationArgs();
            NewArgs.Tags = args.Tags;
            if (NewArgs.Tags.Contains($"{ClickedTag}"))
            {
                NewArgs.Tags = NewArgs.Tags.Replace($"{ClickedTag}", "");
            }

            if (NewArgs.Tags.Contains($"-{ClickedTag}"))
            {
                return;
            }
            NewArgs.Tags += $" -{ClickedTag}";
            NewArgs.Page = 1;
            args.ClickedPost = PostHandler.CurrentPost;
            PagesStack.ArgsStack.Add(NewArgs);
            this.Frame.Navigate(typeof(PostsSearch), PagesStack.ArgsStack.Count() - 1, new EntranceNavigationTransitionInfo());
        }
        private void FavoriteTag(object sender, RoutedEventArgs e)
        {
            // TODO: Make this function
        }
        private void ViewWikiEntry(object sender, RoutedEventArgs e)
        {
            // TODO: Make this function
        }
        #endregion Tags Interactions

        #region Image Share Functions
        private void CopyImage_Click(object sender, RoutedEventArgs e)
        {
            Thread saveThread = new Thread(new ThreadStart(CopyImage));
            saveThread.Start();
        }
        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            var grid = ((Grid)this.Frame.Parent).Parent as Grid;
            var page = (MainPage)grid.Parent;
            page.AddItemToQueue(new DownloadQueueItem() { PostToDownload = PostHandler.CurrentPost, FileName = PostHandler.CurrentPost.file.md5, FolderName = ""});
        }
        private void CopyPostLink_Click(object sender, RoutedEventArgs e)
        {
            Thread saveThread = new Thread(new ThreadStart(CopyPostLink));
            saveThread.Start();
        }
        private void CopyDirectLink_Click(object sender, RoutedEventArgs e)
        {
            Thread saveThread = new Thread(new ThreadStart(CopyContentLink));
            saveThread.Start();
        }
        private async void CopyImage()
        {
            try
            {
                HttpClient client = new HttpClient();
                var dataPackage = new DataPackage();
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromUri(new Uri(PostHandler.CurrentPost.file.url)));
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                    var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                    var page = (MainPage)grid.Parent;
                    page.ShowSystemMessage("Image Copied!");
                });
            }
            catch (Exception e)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                    var page = (MainPage)grid.Parent;
                    page.ShowSystemMessage("Copy Error");
                });
            }
        }
        private async void CopyContentLink()
        {
            try
            {
                var dataPackage = new DataPackage();
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    dataPackage.SetText(PostHandler.CurrentPost.file.url);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                });
                var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                var page = (MainPage)grid.Parent;
                page.ShowSystemMessage("Copied!");
            }
            catch (Exception err)
            {
                var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                var page = (MainPage)grid.Parent;
                page.ShowSystemMessage("Copy Error");
            }
        }
        private async void CopyPostLink()
        {
            try
            {
                var dataPackage = new DataPackage();
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    dataPackage.SetText("https://e621.net/posts/" + PostHandler.CurrentPost.id);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                    var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                    var page = (MainPage)grid.Parent;
                    page.ShowSystemMessage("Copied");
                });
            }
            catch (Exception err)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                    var page = (MainPage)grid.Parent;
                    page.ShowSystemMessage("Copy Error");
                });
            }
        }
        #endregion Image Share Functions

        #region Videoplayer Functions
        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            var player = sender as MediaElement;
            player.Visibility = Visibility.Visible;
        }
        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            var player = sender as MediaElement;
            player.Visibility = Visibility.Collapsed;
        }




        #endregion Videoplayer Functions

        private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if(SettingsHandler.Username == "")
            {
                var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                var page = (MainPage)grid.Parent;
                page.ShowSystemMessage("You must be logged in to favorite!");
                return;
            }
            var x = await host.FavoritePost(PostHandler.CurrentPost.id);
            if (x)
            {
                FavoriteButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 94, 255, 255));
                PostHandler.CurrentPost.is_favorited = true;
                PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].is_favorited = true;
            }
            else
            {
                await host.UnfavoritePost(PostHandler.CurrentPost.id);
                FavoriteButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                PostHandler.CurrentPost.is_favorited = false;
                PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].is_favorited = false;
            }
        }

        private void PoolsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            PostNavigationArgs NewArgs = new PostNavigationArgs();
            NewArgs.Page = 1;
            NewArgs.ClickedPool = (Pool)e.ClickedItem;
            NewArgs.Tags = args.Tags;
            PagesStack.ArgsStack.Add(NewArgs);

            this.Frame.Navigate(typeof(PoolViewPage), PagesStack.ArgsStack.Count()-1, new DrillInNavigationTransitionInfo());
        }
    }
}
