﻿using e6API;
using Fluff.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
            foreach(var post in PostNavigationArgs.PostsList)
            {
                PostsViewModel.Add(post);
            }
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
            var gridViewItem = PostFlipView.ContainerFromItem(PostFlipView.SelectedItem) as FlipViewItem;

            PostNavigationArgs.ClickedPost = PostFlipView.SelectedItem as Post;

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
                MenuFlyoutItem Item1 = new MenuFlyoutItem { Text = "Add tag to search" };
                MenuFlyoutItem Item2 = new MenuFlyoutItem { Text = "Filter tag from search" };
                MenuFlyoutItem Item3 = new MenuFlyoutItem { Text = "Favorite tag" };
                MenuFlyoutItem Item4 = new MenuFlyoutItem { Text = "View Wiki Entry" };
                Item1.Click += new RoutedEventHandler(AddTagToSearch);
                Item2.Click += new RoutedEventHandler(FliterTagFromSearch);
                Item3.Click += new RoutedEventHandler(FavoriteTag);
                Item4.Click += new RoutedEventHandler(ViewWikiEntry);
                myFlyout.Items.Add(Item1);
                myFlyout.Items.Add(Item2);
                myFlyout.Items.Add(Item3);
                myFlyout.Items.Add(Item4);
                var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;

                pointerPosition.X = pointerPosition.X - Window.Current.Bounds.X;
                pointerPosition.Y = pointerPosition.Y - Window.Current.Bounds.Y;
                pointerPosition.X -= 65;

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
            // TODO: Make this function
        }
        private void FliterTagFromSearch(object sender, RoutedEventArgs e)
        {
            // TODO: Make this function
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
    }
}