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
        ObservableCollection<Post> ParentViewModel;
        ObservableCollection<Post> ChildrenViewModel;
        ObservableCollection<Pool> PoolsSource;
        ObservableCollection<Comment> CommentSource;
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
            if (SettingsHandler.Username != "")
            {
                host.Username = SettingsHandler.Username;
                host.ApiKey = SettingsHandler.ApiKey;
            }
            PostsViewModel = new ObservableCollection<Post>();
            PostHandler = new CurrentPostNotif();
            PoolsSource = new ObservableCollection<Pool>();
            CommentSource = new ObservableCollection<Comment>();
            ParentViewModel = new ObservableCollection<Post>();
            ChildrenViewModel = new ObservableCollection<Post>();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            args = PagesStack.ArgsStack[(int)e.Parameter];
            PostsViewModel = new ObservableCollection<Post>(args.PostsList);
            PostFlipView.SelectedIndex = PostsViewModel.IndexOf(args.ClickedPost);
            PostHandler.CurrentPost = args.ClickedPost;
            PostHandler.ShowHighQuality = Visibility.Collapsed;

            Thread t = new Thread(SetVoteColors);
            t.Start();
        }

        private void SinglePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (PostHandler.CurrentPost.file.ext == "webm")
            {
                ShowToolBarVideo.Begin();
            }
            else
            {
                ShowToolBar.Begin();
            }
            ThumbnailGridView.ScrollIntoView(ThumbnailGridView.Items[PostFlipView.SelectedIndex]);
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
        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            var x = await host.VotePost(PostHandler.CurrentPost.id, 1);
            if (x == null)
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
            PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].score.up = x.up;
            PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].score.down = x.down;
            PostHandler.CurrentPost.score.down = x.down;
            Bindings.Update();
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
            PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].score.up = x.up;
            PostsViewModel[PostsViewModel.IndexOf(PostHandler.CurrentPost)].score.down = x.down;
            PostHandler.CurrentPost.score.down = x.down;
            Bindings.Update();
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
            if (ToolBarTrans.Y == 0) 
            { 
                if (PostHandler.CurrentPost.file.ext == "webm")
                {
                    ShowToolBarVideo.Begin();
                }
                else
                {
                    ShowToolBar.Begin();
                }
            }

            if (DescTrans.X == 0)
            {
                DescText.Text = DTextConverter.ToMarkdown(PostHandler.CurrentPost.description);
                CommentSource.Clear();
                CommentsProgress.Visibility = Visibility.Visible;

                Thread b = new Thread(LoadComments);
                b.Start();

                ParentViewModel.Clear();
                ChildrenViewModel.Clear();
                Thread d = new Thread(LoadRelationships);
                d.Start();

                SourcesTextBox.Text = "";
                foreach (var item in PostHandler.CurrentPost.sources)
                {
                    SourcesTextBox.Text += $"{item}\n\n\n";
                }
            }

            ThumbnailGridView.ScrollIntoView(ThumbnailGridView.Items[PostFlipView.SelectedIndex]);
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
                NewArgs.Tags = NewArgs.Tags.Replace($"-{ClickedTag}", "");
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
            SettingsHandler.FavoriteTags.Add(ClickedTag);
            SettingsHandler.SaveFavsToFile();
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
            page.AddItemToQueue(new DownloadQueueItem() { PostToDownload = PostHandler.CurrentPost, FileName = PostHandler.CurrentPost.file.md5, FolderName = "" });
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
            if (SettingsHandler.Username == "")
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
            args.ClickedPost = PostHandler.CurrentPost;
            PagesStack.ArgsStack.Add(NewArgs);

            this.Frame.Navigate(typeof(PoolViewPage), PagesStack.ArgsStack.Count() - 1, new DrillInNavigationTransitionInfo());
        }

        private void CommentsButton_Click(object sender, RoutedEventArgs e)
        {
            CommentSource.Clear();
            CommentsProgress.Visibility = Visibility.Visible;
            ShowCommentsPane.Begin();

            Thread t = new Thread(LoadComments);
            t.Start();
        }

        private async void LoadComments()
        {
            var comments = await host.GetPostComments(PostHandler.CurrentPost.id);

            if (comments == null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    CommentsProgress.Visibility = Visibility.Collapsed;
                });
                return;
            }

            foreach (var comment in comments)
            {
                comment.body = DTextConverter.ToMarkdown(comment.body);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    CommentSource.Add(comment);
                });
            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                CommentsProgress.Visibility = Visibility.Collapsed;
            });
        }

        private async void LoadRelationships()
        {
            var parent = await host.GetPosts($"id:{PostHandler.CurrentPost.relationships.parent_id}", 1);
            if (parent.Count > 0)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ParentNoText.Text = "";
                    ParentViewModel.Add(parent[0]);
                });
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ParentNoText.Text = "No parent found for this post.";
                });
            }
            if (PostHandler.CurrentPost.relationships.children.Count() > 0)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ChildrenNoText.Text = "";
                });
                foreach (var item in PostHandler.CurrentPost.relationships.children)
                {
                    var post = await host.GetPosts($"id:{item}", 1);
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        ChildrenViewModel.Add(post[0]);
                    });
                }
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ChildrenNoText.Text = "No children found for this post.";
                });
            }
        }

        private void CloseCommentsButton_Click(object sender, RoutedEventArgs e)
        {
            HideCommentsPane.Begin();
        }

        private void PostCommentButton_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(PostComment);
            t.Start(CommentTextBox.Text);
        }

        private async void PostComment(object s)
        {
            string body = s as string;

            var check = await host.CommentOnPost(PostHandler.CurrentPost.id, body);

            if (!check)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                    var page = (MainPage)grid.Parent;
                    page.ShowSystemMessage("Failed to comment, are you logged in?");
                });
                return;
            }

            var comments = await host.GetPostComments(PostHandler.CurrentPost.id, 1);

            comments[0].body = DTextConverter.ToMarkdown(comments[0].body);
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                CommentSource.Insert(0, comments[0]);
                CommentTextBox.Text = "";
                CommentsProgress.Visibility = Visibility.Collapsed;
            });
        }


        private void DescButton_Click(object sender, RoutedEventArgs e)
        {
            if (DescTrans.X == 0)
            {
                HideDesc.Begin();
            }
            else
            {
                DescText.Text = DTextConverter.ToMarkdown(PostHandler.CurrentPost.description);

                CommentSource.Clear();
                CommentsProgress.Visibility = Visibility.Visible;
                Thread t = new Thread(LoadComments);
                t.Start();

                ParentViewModel.Clear();
                ChildrenViewModel.Clear();
                Thread b = new Thread(LoadRelationships);
                b.Start();

                RightSideBarDef.MinWidth = 400;
                RightSideBarDef.Width = new GridLength(400);
                ShowDesc.Begin();

                SourcesTextBox.Text = "";
                foreach (var item in PostHandler.CurrentPost.sources)
                {
                    SourcesTextBox.Text += $"{item}\n\n\n";
                }
            }
        }

        private void CloseDescButton_Click(object sender, RoutedEventArgs e)
        {
            HideDesc.Begin();
        }

        private void PostFlipView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ToolBarTrans.Y == 0)
            {
                HideToolBar.Begin();
            }
            else
            {
                if (PostHandler.CurrentPost.file.ext == "webm")
                {
                    ShowToolBarVideo.Begin();
                }
                else {
                    ShowToolBar.Begin();
                }
            }
        }

        private void TagsButton_Click(object sender, RoutedEventArgs e)
        {
            if (TagsTrans.X == 0)
            {
                HideTags.Begin();
            }
            else
            {
                LeftSideBarDef.MinWidth = 250;
                LeftSideBarDef.Width = new GridLength(250);
                ShowTags.Begin();
            }
        }

        private void HideTags_Completed(object sender, object e)
        {
            LeftSideBarDef.MinWidth = 0;
            LeftSideBarDef.Width = new GridLength(0);
        }

        private void AppBarToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (((AppBarToggleButton)sender).IsChecked.Value)
            {
                PostHandler.ShowHighQuality = Visibility.Visible;
            }
            else
            {
                PostHandler.ShowHighQuality = Visibility.Collapsed;
            }
            Bindings.Update();
        }

        private void CurrentImage_ImageExFailed(object sender, Microsoft.Toolkit.Uwp.UI.Controls.ImageExFailedEventArgs e)
        {
            var grid = ((Grid)this.Frame.Parent).Parent as Grid;
            var page = (MainPage)grid.Parent;
            page.ShowSystemMessage($"Image Failed to load: {e.ErrorMessage}");
        }

        private void HideDesc_Completed(object sender, object e)
        {
            RightSideBarDef.MinWidth = 0;
            RightSideBarDef.Width = new GridLength(0);
        }

        private async void SourcesTextBox_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(e.Link));
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ObservableCollection<Post> posts = ((GridView)sender).ItemsSource as ObservableCollection<Post>;
            Post p = e.ClickedItem as Post;
            PostNavigationArgs NewArgs = new PostNavigationArgs();
            NewArgs.ClickedPost = p;
            NewArgs.Page = args.Page;
            NewArgs.Tags = args.Tags;
            args.PostsList =  new ObservableCollection<Post>(PostsViewModel);
            NewArgs.PostsList = new ObservableCollection<Post>(posts);
            PagesStack.ArgsStack.Add(NewArgs);

            this.Frame.Navigate(typeof(SinglePostView), PagesStack.ArgsStack.Count() - 1, new DrillInNavigationTransitionInfo());
        }
    }
}
