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
        CurrentPostNotif PostHandler;
        string ClickedTag;
        #endregion Global Page Vars

        #region Page Loading Functions
        public SinglePostView()
        {
            this.InitializeComponent();
            PostsViewModel = new ObservableCollection<Post>();
            PostHandler = new CurrentPostNotif();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            PostsViewModel = new ObservableCollection<Post>(PostNavigationArgs.PostsList);
            PostFlipView.SelectedIndex = PostsViewModel.IndexOf(PostNavigationArgs.ClickedPost);
            PostHandler.CurrentPost = PostNavigationArgs.ClickedPost;

            ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("forwardAnimation");
            if (animation != null)
            {
                animation.TryStart(PostFlipView);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            PostNavigationArgs.ClickedPost = PostFlipView.SelectedItem as Post;
            if (PostNavigationArgs.NeedsRefersh)
            {
                return;
            }
            var gridViewItem = PostFlipView.ContainerFromItem(PostFlipView.SelectedItem) as FlipViewItem;

            ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("forwardAnimation", gridViewItem);
           
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
        #endregion ToolBar Functions

        #region Image and FlipView Interactions
        private void PostFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PostHandler.CurrentPost = e.AddedItems[0] as Post;
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
            if (PostNavigationArgs.Tags.Contains($"-{ClickedTag}"))
            {
                PostNavigationArgs.Tags.Replace($"-{ClickedTag}","");
            }

            if (PostNavigationArgs.Tags.Contains(ClickedTag))
            {
                return;
            }
            PostNavigationArgs.Tags += $" {ClickedTag}";
            PostNavigationArgs.NeedsRefersh = true;
            this.Frame.Navigate(typeof(PostsSearch), null, new EntranceNavigationTransitionInfo());
        }
        private void SearchTag(object sender, RoutedEventArgs e)
        {
            PostNavigationArgs.Tags = ClickedTag;
            PostNavigationArgs.NeedsRefersh = true;
            this.Frame.Navigate(typeof(PostsSearch), null, new EntranceNavigationTransitionInfo());
        }
        private void FliterTagFromSearch(object sender, RoutedEventArgs e)
        {
            if (PostNavigationArgs.Tags.Contains($"{ClickedTag}"))
            {
                PostNavigationArgs.Tags.Replace($"{ClickedTag}", "");
            }

            if (PostNavigationArgs.Tags.Contains($"-{ClickedTag}"))
            {
                return;
            }
            PostNavigationArgs.Tags += $" -{ClickedTag}";
            PostNavigationArgs.NeedsRefersh = true;
            this.Frame.Navigate(typeof(PostsSearch), null, new EntranceNavigationTransitionInfo());
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

        private void CopyImage_Click(object sender, RoutedEventArgs e)
        {
            Thread saveThread = new Thread(new ThreadStart(CopyImage));
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

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            var grid = ((Grid)this.Frame.Parent).Parent as Grid;
            var page = (MainPage)grid.Parent;
            page.AddItemToQueue(new DownloadQueueItem() { PostToDownload = PostHandler.CurrentPost});
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

        private void LikeButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
