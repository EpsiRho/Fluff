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
using Windows.UI.Xaml.Navigation;

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
        bool CanSelect;
        bool CanUpadteAnchor;
        public UploadPage()
        {
            this.InitializeComponent();
            TagsViewModel = new ObservableCollection<Tag>();
            CanSelect = true;
            CanUpadteAnchor = true;
            host = new RequestHost("Fluff/0.7 (by EpsilonRho)"); // Initialize the api host
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
                    TagsViewModel.Add(t);
                });
            }

        }

        private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var tag = await host.SearchTags(args.QueryText, 1);
            if(tag == null)
            {
                InvalidGridView.Items.Add(new Tag { name = args.QueryText, category = 6, created_at = DateTime.Now });
                return;
            }
            if(tag.Count() > 0)
            {
                if (tag[0].name == args.QueryText)
                {
                    sender.Text = "";
                    switch (tag[0].category)
                    {
                        case 0:
                            GeneralGridView.Items.Add(tag[0]);
                            break;
                        case 1:
                            ArtistsGridView.Items.Add(tag[0]);
                            break;
                        case 3:
                            CopyrightsGridView.Items.Add(tag[0]);
                            break;
                        case 4:
                            CharactersGridView.Items.Add(tag[0]);
                            break;
                        case 5:
                            SpeciesGridView.Items.Add(tag[0]);
                            break;
                        case 6:
                            InvalidGridView.Items.Add(tag[0]);
                            break;
                        case 7:
                            MetaGridView.Items.Add(tag[0]);
                            break;
                        case 8:
                            LoreGridView.Items.Add(tag[0]);
                            break;
                    }
                    TagsViewModel.Clear();
                    return;
                }
            }

            InvalidGridView.Items.Add(new Tag { name = args.QueryText, category = 6, created_at = DateTime.Now});
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = (args.SelectedItem as Tag).name;
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

        private void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            

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
    }
}
