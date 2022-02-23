using e6API;
using Fluff.Classes;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Fluff.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UploadPage : Page
    {
        ObservableCollection<Tag> TagsViewModel;
        RequestHost host;
        // Used to timeout tag autocomplete until the user stops typing.
        public double timeTillSearch;
        // The thread that waits for the user to stop typing
        Thread TagsThread;
        // The tag that will be searched when timeTillSearch = 0
        string TagToSearch;
        string Stop;
        StorageFile ChosenFile;
        bool CanSelect;
        bool CanUpadteAnchor;
        List<Tag> CurrentSuggestions;
        public UploadPage()
        {
            this.InitializeComponent();
            TagsViewModel = new ObservableCollection<Tag>();
            CanSelect = true;
            CanUpadteAnchor = true;
            CurrentSuggestions = new List<Tag>();
            host = new RequestHost("Fluff/0.7 (by EpsilonRho)"); // Initialize the api host
            host.Username = SettingsHandler.Username; // Initialize the api host
            host.ApiKey = SettingsHandler.ApiKey; // Initialize the api host
        }

        private void TimerThread()
        {
            while (timeTillSearch > 0)
            {
                timeTillSearch -= 0.1;
                Thread.Sleep(100);
            }

            Thread t = new Thread(GetAutocompleteTags);
            t.Start(TagToSearch);
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
                    CurrentSuggestions.Add(t);
                    TagsViewModel.Add(t);
                });
            }

        }

        private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if(args.QueryText == "e6API_Tag")
            {
                return;
            }
            //var tag = await host.SearchTags(args.QueryText, 1);
            Tag tag = null;
            try
            {
                tag = CurrentSuggestions.First(o => o.name == args.QueryText);
            }
            catch (Exception ex)
            {
                var temp = await host.SearchTags(args.QueryText, 1);
                if (temp != null)
                {
                    tag = temp[0];
                }
            }

            if (tag == null)
            {
                InvalidGridView.Items.Add(new Tag { name = args.QueryText, category = 6, created_at = DateTime.Now });
                return;
            }
            if (tag.name == args.QueryText)
            {
                sender.Text = "";
                switch (tag.category)
                {
                    case 0:
                        GeneralGridView.Items.Add(tag);
                        break;
                    case 1:
                        ArtistsGridView.Items.Add(tag);
                        break;
                    case 3:
                        CopyrightsGridView.Items.Add(tag);
                        break;
                    case 4:
                        CharactersGridView.Items.Add(tag);
                        break;
                    case 5:
                        SpeciesGridView.Items.Add(tag);
                        break;
                    case 6:
                        InvalidGridView.Items.Add(tag);
                        break;
                    case 7:
                        MetaGridView.Items.Add(tag);
                        break;
                    case 8:
                        LoreGridView.Items.Add(tag);
                        break;
                }
                TagsViewModel.Clear();
                return;
            }

            //InvalidGridView.Items.Add(new Tag { name = args.QueryText, category = 6, created_at = DateTime.Now});
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem == "e6API_Tag")
            {
                return;
            }
            string x = (args.SelectedItem as Tag).name;
            if(x == "e6API_Tag")
            {
                return;
            }
            sender.Text = x;
        }

        private void RemoveItemFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource != null)
            {
                Tag item = ((MenuFlyoutItem)e.OriginalSource).Tag as Tag;
                switch (item.category)
                {
                    case 0:
                        GeneralGridView.Items.Remove(item);
                        break;
                    case 1:
                        ArtistsGridView.Items.Remove(item);
                        break;
                    case 3:
                        CopyrightsGridView.Items.Remove(item);
                        break;
                    case 4:
                        CharactersGridView.Items.Remove(item);
                        break;
                    case 5:
                        SpeciesGridView.Items.Remove(item);
                        break;
                    case 6:
                        InvalidGridView.Items.Remove(item);
                        break;
                    case 7:
                        MetaGridView.Items.Remove(item);
                        break;
                    case 8:
                        LoreGridView.Items.Remove(item);
                        break;
                }
                Bindings.Update();
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!CanSelect)
            {
                CanSelect = true;
                return;
            }
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                timeTillSearch = 0.3;
                if (TagsThread == null)
                {
                    TagsThread = new Thread(TimerThread);
                    TagsThread.Start();
                }
                string[] split = sender.Text.Split(' ');
                TagToSearch = split[split.Length - 1];
            }
            else 
            { 

            }
        }

        private void SearchBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Up || e.Key == Windows.System.VirtualKey.Down)
            {
                CanSelect = false;
            }
        }

        private void SafeButton_Click(object sender, RoutedEventArgs e)
        {
            QuestionableButton.IsChecked = false;
            ExplicitButton.IsChecked = false;
        }

        private void QuestionableButton_Click(object sender, RoutedEventArgs e)
        {
            SafeButton.IsChecked = false;
            ExplicitButton.IsChecked = false;
        }

        private void ExplicitButton_Click(object sender, RoutedEventArgs e)
        {
            SafeButton.IsChecked = false;
            QuestionableButton.IsChecked = false;
        }


        private void NavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            CanUpadteAnchor = false;
            string item = args.InvokedItem as string;
            switch (item)
            {
                case "File":
                    FileBorder.StartBringIntoView(new BringIntoViewOptions() { VerticalOffset = 0, AnimationDesired = true, VerticalAlignmentRatio = 0.0 });
                    break;
                case "Sources":
                    SourcesBorder.StartBringIntoView(new BringIntoViewOptions() { VerticalOffset = 0, AnimationDesired = true, VerticalAlignmentRatio = 0.0 });
                    break;
                case "Tags":
                    TagsBorder.StartBringIntoView(new BringIntoViewOptions() { VerticalOffset = 0, AnimationDesired = true, VerticalAlignmentRatio = 0.0 });
                    break;
                case "Rating":
                    RatingBorder.StartBringIntoView(new BringIntoViewOptions() { VerticalOffset = 0, AnimationDesired = true, VerticalAlignmentRatio = 0.0 });
                    break;
                case "Relationships":
                    RelationshipsBorder.StartBringIntoView(new BringIntoViewOptions() { VerticalOffset = 0, AnimationDesired = true, VerticalAlignmentRatio = 0.0 });
                    break;
                case "Description":
                    DescriptionBorder.StartBringIntoView(new BringIntoViewOptions() { VerticalOffset = 0, AnimationDesired = true, VerticalAlignmentRatio = 0.0 });
                    break;
                case "Upload":
                    UploadBorder.StartBringIntoView(new BringIntoViewOptions() { VerticalOffset = 0, AnimationDesired = true, VerticalAlignmentRatio = 0.0 });
                    break;
            }
        }

        private void MainScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (!CanUpadteAnchor)
            {
                if (!e.IsIntermediate)
                {
                    CanUpadteAnchor = true;
                }
                return;
            }
            double x = MainScrollViewer.VerticalOffset;
            if (x >= UploadBorder.ActualOffset.Y)
            {
                if (AnchorNav.SelectedItem != UploadNav)
                {
                    AnchorNav.SelectedItem = UploadNav;
                }
            }
            else if (x >= DescriptionBorder.ActualOffset.Y)
            {
                if (AnchorNav.SelectedItem != DescriptionNav)
                {
                    AnchorNav.SelectedItem = DescriptionNav;
                }
            }
            else if (x >= RelationshipsBorder.ActualOffset.Y)
            {
                if (AnchorNav.SelectedItem != RelationshipsNav)
                {
                    AnchorNav.SelectedItem = RelationshipsNav;
                }
            }
            else if (x >= RatingBorder.ActualOffset.Y)
            {
                    
                if (AnchorNav.SelectedItem != RatingNav)
                {
                    AnchorNav.SelectedItem = RatingNav;
                }
            }
            else if (x >= TagsBorder.ActualOffset.Y)
            {
                if (AnchorNav.SelectedItem != TagsNav)
                {
                    AnchorNav.SelectedItem = TagsNav;
                }
            }
            else if (x >= SourcesBorder.ActualOffset.Y)
            {
                if (AnchorNav.SelectedItem != SourcesNav)
                {
                    AnchorNav.SelectedItem = SourcesNav;
                }

            }
            else if (x >= FileBorder.ActualOffset.Y)
            {
                if (AnchorNav.SelectedItem != FileNav)
                {
                    AnchorNav.SelectedItem = FileNav;
                }
            }
        }

        private void SourcesAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (SourcesList.Items.Count() == 10)
            {
                return;
            }
            SourcesList.Items.Add(SourcesTextBox.Text);
            SourcesTextBox.Text = "";
        }

        private void SourcesMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            string item = ((MenuFlyoutItem)e.OriginalSource).Tag as string;
            if (item != null)
            {
                SourcesList.Items.Remove(item);
            }
            Bindings.Update();
        }

        private void SourcesTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                if(SourcesList.Items.Count() == 10)
                {
                    return;
                }
                SourcesList.Items.Add(SourcesTextBox.Text);
                SourcesTextBox.Text = "";
            }
        }

        private async void AddFileGridViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".webm");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                ChosenFile = file;
                BitmapImage bitmapImage = new BitmapImage();
                FileRandomAccessStream stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);

                bitmapImage.SetSource(stream);
                //FileImage.Source = bitmapImage;
            }
        }

        private void AddLinkTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                LinkImage.ImageSource = new BitmapImage(new Uri(AddLinkTextBox.Text));
            }
            catch (Exception ex)
            {

            }
        }

        private void Uploadbutton_Click(object sender, RoutedEventArgs e)
        {
            bool check = false;

            // Check Tags
            int count = ArtistsGridView.Items.Count();
            count += CopyrightsGridView.Items.Count();
            count += CharactersGridView.Items.Count();
            count += SpeciesGridView.Items.Count();
            count += GeneralGridView.Items.Count();
            count += MetaGridView.Items.Count();
            count += LoreGridView.Items.Count();

            if(count < 4)
            {
                check = true;
                TagsError.Visibility = Visibility.Visible;
            }
            else
            {
                TagsError.Visibility = Visibility.Collapsed;
            }

            if(!(SafeButton.IsChecked.Value || QuestionableButton.IsChecked.Value || ExplicitButton.IsChecked.Value))
            {
                check = true;
                RatingError.Visibility = Visibility.Visible;
            }
            else
            {
                RatingError.Visibility = Visibility.Collapsed;
            }

            if (AddLinkTextBox.Text == "")
            {
                UploadError.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                UploadError.Visibility = Visibility.Collapsed;
            }

            if (check)
            {
                UploadError.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                UploadError.Visibility = Visibility.Collapsed;
            }


            var anim = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", UploadBorder);

            UploadingPaneBG.Visibility = Visibility.Visible;
            UploadingPane.Visibility = Visibility.Visible;

            if (anim != null)
            {
                anim.TryStart(UploadingPane);
            }

            Thread t = new Thread(UploadFile);
            t.Start();
        }

        private async void UploadFile()
        {
            string url = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                url = AddLinkTextBox.Text;
            });

            string md5Hash = "";
            byte[] FileArr;
            if (url != "")
            {
                WebClient client = new WebClient();
                FileArr = client.DownloadData(new Uri(url));

                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(FileArr);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                md5Hash = sb.ToString();
            }
            else
            {
                MemoryStream ms = new MemoryStream();
                Stream stream = await ChosenFile.OpenStreamForReadAsync();
                stream.CopyTo(ms);
                FileArr = ms.ToArray();

                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(FileArr);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                md5Hash = sb.ToString();
            }

            string tags = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                foreach (Tag t in ArtistsGridView.Items)
                {
                    tags += $"{t.name} ";
                }
                foreach (Tag t in CopyrightsGridView.Items)
                {
                    tags += $"{t.name} ";
                }
                foreach (Tag t in CharactersGridView.Items)
                {
                    tags += $"{t.name} ";
                }
                foreach (Tag t in SpeciesGridView.Items)
                {
                    tags += $"{t.name} ";
                }
                foreach (Tag t in GeneralGridView.Items)
                {
                    tags += $"{t.name} ";
                }
                foreach (Tag t in MetaGridView.Items)
                {
                    tags += $"{t.name} ";
                }
                foreach (Tag t in LoreGridView.Items)
                {
                    tags += $"{t.name} ";
                }
                foreach (Tag t in InvalidGridView.Items)
                {
                    tags += $"{t.name} ";
                }
            });

            string rating = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                if (SafeButton.IsChecked.Value)
                {
                    rating = "s";
                }
                else if (QuestionableButton.IsChecked.Value)
                {
                    rating = "q";
                }
                else
                {
                    rating = "e";
                }
            });

            string sources = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                foreach (string s in SourcesList.Items)
                {
                    sources += $"{s}%0A";
                }
            });

            string description = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                description = DescriptionBox.Text;
            });

            string parent_id = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                parent_id = ParentIDBox.Text;
            });

            int PostID = -1;
            if (url != "")
            {
                PostID = await host.CreateNewPost(tags, rating, sources, md5Hash, url, description, parent_id);
            }
            else
            {
                
            }

            if(PostID == -1)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                    var page = (MainPage)grid.Parent;
                    page.ShowSystemMessage("Couldn't Upload File!");
                    PagesStack.ArgsStack.Add(new PostNavigationArgs()
                    {
                        ClickedPost = null,
                        Page = 1,
                        PostsList = null,
                        Tags = ""
                    });
                    this.Frame.Navigate(typeof(PostsSearch), PagesStack.ArgsStack.Count() - 1, new DrillInNavigationTransitionInfo());
                });
            }
            else
            {
                var post = await host.GetPosts($"id:{PostID}", 1);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    var grid = ((Grid)this.Frame.Parent).Parent as Grid;
                    var page = (MainPage)grid.Parent;
                    page.ShowSystemMessage("File Uploaded!");
                    PagesStack.ArgsStack.Add(new PostNavigationArgs()
                    {
                        ClickedPost = post[0],
                        Page = 1,
                        PostsList = new ObservableCollection<Post>(post),
                        Tags = ""
                    });
                    this.Frame.Navigate(typeof(SinglePostView), PagesStack.ArgsStack.Count() - 1, new DrillInNavigationTransitionInfo());
                });
            }
        }
    }
}
