using e6API;
using Fluff.Classes;
using Fluff.Pages;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace Fluff
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        int SelectedIndex;
        ObservableCollection<DownloadQueueItem> DownloadQueueModel;
        Thread DownloadQueueThread;
        RequestHost host;
        User CurrentUser;
        bool SysMessIsOpen;

        public MainPage()
        {
            this.InitializeComponent();
            PagesStack.ArgsStack = new List<PostNavigationArgs>();
            PagesStack.ArgsStack.Add(new PostNavigationArgs()
            {
                ClickedPost = null,
                Page = 1,
                PostsList = null,
                Tags = ""
            });
            contentFrame.Navigate(typeof(PostsSearch), PagesStack.ArgsStack.Count()-1, new DrillInNavigationTransitionInfo());
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            DownloadQueueModel = new ObservableCollection<DownloadQueueItem>();
            SelectedIndex = 0;
            host = new RequestHost(SettingsHandler.UserAgent);
            GetSettings();
            SetSettingsUI();
            //CheckForUpdate();

            SettingsHandler.VotedPosts = new Dictionary<int, bool>();
            if (SettingsHandler.Username != "")
            {
                Thread t = new Thread(GetLikedPosts);
                t.Start();
            }
        }

        #region Settings Functions
        private void GetSettings()
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            SettingsHandler.Username = (string)localSettings.Values["username"];
            if (SettingsHandler.Username == null)
            {
                SettingsHandler.Username = "";
                localSettings.Values["username"] = "";
            }
            SettingsHandler.ApiKey = (string)localSettings.Values["apikey"];
            if (SettingsHandler.ApiKey == null)
            {
                SettingsHandler.ApiKey = "";
                localSettings.Values["apikey"] = "";
            }
            SettingsHandler.Rating = (string)localSettings.Values["rating"];
            if (SettingsHandler.Rating == null)
            {
                SettingsHandler.Rating = "safe";
                localSettings.Values["rating"] = "safe";
            }
            try
            {
                SettingsHandler.ShowComments = (bool)localSettings.Values["comments"];
            }
            catch (Exception)
            {
                SettingsHandler.ShowComments = true;
                localSettings.Values["comments"] = true;
            }
            try
            {
                SettingsHandler.MuteVolume = (bool)localSettings.Values["volume"];
            }
            catch (Exception)
            {
                SettingsHandler.MuteVolume = true;
                localSettings.Values["volume"] = true;
            }
            try
            {
                SettingsHandler.PostCount = (double)localSettings.Values["postcount"];
            }
            catch (Exception)
            {
                SettingsHandler.PostCount = 75;
                localSettings.Values["postcount"] = 75;
            }
        }
        private void SetSettings()
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["rating"] = RatingSelection.SelectedItem as string;
            SettingsHandler.Rating = RatingSelection.SelectedItem as string;
            localSettings.Values["postcount"] = PostCountSlider.Value;
            SettingsHandler.PostCount = (double)PostCountSlider.Value;
            localSettings.Values["comments"] = CommentSwitch.IsOn;
            SettingsHandler.ShowComments = CommentSwitch.IsOn;
            localSettings.Values["volume"] = VolumeSwitch.IsOn;
            SettingsHandler.MuteVolume = VolumeSwitch.IsOn;
        }
        private async void SetSettingsUI()
        {
            RatingSelection.SelectedItem = SettingsHandler.Rating;
            PostCountSlider.Value = SettingsHandler.PostCount;
            CommentSwitch.IsOn = SettingsHandler.ShowComments;
            VolumeSwitch.IsOn = SettingsHandler.MuteVolume;
            if(SettingsHandler.Username != "")
            {
                var check = await host.TryAuthenticate(SettingsHandler.Username, SettingsHandler.ApiKey);
                if (!check)
                {
                    ShowSystemMessage("Login Invalid, Please re-login");
                    SettingsHandler.Username = "";
                    SettingsHandler.ApiKey = "";
                    Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values["username"] = "";
                    localSettings.Values["apikey"] = "";
                    return;
                }

                host.Username = SettingsHandler.Username;
                host.ApiKey = SettingsHandler.ApiKey;
                LoginPanel.Visibility = Visibility.Collapsed;
                LoggedInPanel.Visibility = Visibility.Visible;
                UsernameSet.Text = SettingsHandler.Username;
                BlacklistProgress.Visibility = Visibility.Visible;
                Thread t = new Thread(GetBlacklistTags);
                t.Start();
            }
        }
        private void SaveBlacklistButton_Click(object sender, RoutedEventArgs e)
        {
            BlacklistProgress.Visibility = Visibility.Visible;
            Thread t = new Thread(SetBlacklistTags);
            t.Start();
        }
        private async void GetBlacklistTags()
        {
            host.Username = SettingsHandler.Username;
            host.ApiKey = SettingsHandler.ApiKey;
            var users = await host.SearchUsers(SettingsHandler.Username, 1);
            string[] tags = users[0].blacklisted_tags.Split(" ");
            CurrentUser = users[0];
            if (tags != null)
            {
                foreach (string tag in tags)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        BlacklistBox.Text += $"{tag}\r\n";
                        BlacklistProgress.Visibility = Visibility.Collapsed;
                    });
                }
            }

        }
        private async void SetBlacklistTags()
        {
            string tags = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                BlacklistProgress.Visibility = Visibility.Visible;
                tags = BlacklistBox.Text;
            });

            string[] splt = tags.Split("\r\n");
            tags = "";

            foreach (string str in splt)
            {
                tags += $"{str}\\n";
            }
            tags = tags.Substring(0, tags.Length - 3);

            string id = CurrentUser.id.ToString();

            var check = await host.UpdateUserBlacklist(id, SettingsHandler.Username, SettingsHandler.ApiKey, tags.Replace("\n", "\\n"));
            if (!check)
            {
                ShowSystemMessage("Couldn't Update Blacklist");
            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                BlacklistProgress.Visibility = Visibility.Collapsed;
            });
        }
        private void SettingsDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SetSettings();
        }
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginProgress.Visibility = Visibility.Visible;
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            string u = "";
            string a = "";
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                u = UsernameEntry.Text;
                a = ApiKeyEntry.Text;
            });

            var check = await host.TryAuthenticate(u, a);

            if (check)
            {
                LoginPanel.Visibility = Visibility.Collapsed;
                LoggedInPanel.Visibility = Visibility.Visible;
                SettingsHandler.Username = u;
                localSettings.Values["username"] = u;
                SettingsHandler.ApiKey = a;
                localSettings.Values["apikey"] = a;
                UsernameSet.Text = u;
                Thread th = new Thread(GetLikedPosts);
                th.Start();
                Thread t = new Thread(GetBlacklistTags);
                t.Start();
            }
            else
            {
                ShowSystemMessage("Invalid Login!");
            }
            LoginProgress.Visibility = Visibility.Collapsed;
            BlacklistProgress.Visibility = Visibility.Visible;
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            LoginPanel.Visibility = Visibility.Visible;
            LoggedInPanel.Visibility = Visibility.Collapsed;
            localSettings.Values["username"] = "";
            localSettings.Values["apikey"] = "";
            UsernameSet.Text = "";
        }
        #endregion Settings Functions

        #region Topbar Functions
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var MyFrame = contentFrame;
                if (MyFrame.CanGoBack)
                {
                    PagesStack.ArgsStack.Remove(PagesStack.ArgsStack.Last());
                    MyFrame.GoBack(new DrillInNavigationTransitionInfo());

                    var postpage = MyFrame.Content as PostsSearch;
                    if(postpage != null)
                    {
                        OpenPostsFull.Begin();
                        ClosePoolsFull.Begin();
                        CloseWikiFull.Begin();
                        CloseUploadFull.Begin();
                    }
                    var poolpage = MyFrame.Content as PoolsSearch;
                    if(poolpage != null)
                    {
                        ClosePostsFull.Begin();
                        OpenPoolsFull.Begin();
                        CloseWikiFull.Begin();
                        CloseUploadFull.Begin();
                    }
                }
            }
            catch (Exception)
            {

            }
        }
        private void PostsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenPostsFull.Begin();
            ClosePoolsFull.Begin();
            CloseWikiFull.Begin();
            CloseUploadFull.Begin();
            PagesStack.ArgsStack.Add(new PostNavigationArgs()
            {
                ClickedPost = null,
                Page = 1,
                PostsList = null,
                Tags = ""
            });
            contentFrame.Navigate(typeof(PostsSearch), PagesStack.ArgsStack.Count() - 1, new DrillInNavigationTransitionInfo());
        }
        private void PostsButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (PostUnderline.ScaleX != 1)
            {
                OpenPostsHalf.Begin();
            }
        }
        private void PostsButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (PostUnderline.ScaleX != 1)
            {
                ClosePostsHalf.Begin();
            }
        }
        private void PoolsButton_Click(object sender, RoutedEventArgs e)
        {
            ClosePostsFull.Begin();
            OpenPoolsFull.Begin();
            CloseWikiFull.Begin();
            CloseUploadFull.Begin();
            PagesStack.ArgsStack.Add(new PostNavigationArgs()
            {
                ClickedPost = null,
                Page = 1,
                PostsList = null,
                Tags = ""
            });
            contentFrame.Navigate(typeof(PoolsSearch), PagesStack.ArgsStack.Count() - 1, new DrillInNavigationTransitionInfo());
        }
        private void PoolsButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (PoolsUnderline.ScaleX != 1)
            {
                OpenPoolsHalf.Begin();
            }
        }
        private void PoolsButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (PoolsUnderline.ScaleX != 1)
            {
                ClosePoolsHalf.Begin();
            }
        }
        private void WikiButton_Click(object sender, RoutedEventArgs e)
        {
            ClosePostsFull.Begin();
            ClosePoolsFull.Begin();
            OpenWikiFull.Begin();
            CloseUploadFull.Begin();
        }
        private void WikiButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (WikiUnderline.ScaleX != 1)
            {
                OpenWikiHalf.Begin();
            }
        }
        private void WikiButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (WikiUnderline.ScaleX != 1)
            {
                CloseWikiHalf.Begin();
            }
        }
        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            ClosePostsFull.Begin();
            ClosePoolsFull.Begin();
            CloseWikiFull.Begin();
            OpenUploadFull.Begin();
        }
        private void UploadButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (UploadUnderline.ScaleX != 1)
            {
                OpenUploadHalf.Begin();
            }
        }
        private void UploadButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (UploadUnderline.ScaleX != 1)
            {
                CloseUploadHalf.Begin();
            }
        }
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog.ShowAsync();
        }
        #endregion Topbar Functions

        #region Crosspage Helpers
        public async void ShowSystemMessage(string message)
        {
            if (SysMessIsOpen)
            {
                return;
            }
            SysMessIsOpen = true;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                SystemMessagetext.Text = message;
                OpenSystemMessage.Begin();
            });
            Thread t = new Thread(SysMessageTimer);
            t.Start();
        }
        private async void SysMessageTimer()
        {
            Thread.Sleep(1500);
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                CloseSystemMessage.Begin();
            });
            SysMessIsOpen = false;
        }
        public void AddItemToQueue(DownloadQueueItem p)
        {
            DownloadQueueModel.Add(p);
            if(DownloadQueueThread == null)
            {
                DownloadQueueThread = new Thread(DownloadThread);
                DownloadQueueThread.Start();
            }
        }
        #endregion Crosspage Helpers

        #region Image Saving
        private async void DownloadThread()
        {
            while(DownloadQueueModel.Count > 0)
            {
                DownloadQueueItem item = null;

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    item = DownloadQueueModel[0];
                });

                await SaveImage(item);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    DownloadQueueModel.RemoveAt(0);
                });
            }
            DownloadQueueThread = null;
        }
        private async Task SaveImage(DownloadQueueItem PostToSave)
        {
            try
            {
                List<string> tags = new List<string>();

                foreach (string tag in PostToSave.PostToDownload.tags.artist)
                {
                    tags.Add(tag);
                }
                foreach (string tag in PostToSave.PostToDownload.tags.copyright)
                {
                    tags.Add(tag);
                }
                foreach (string tag in PostToSave.PostToDownload.tags.character)
                {
                    tags.Add(tag);
                }
                foreach (string tag in PostToSave.PostToDownload.tags.species)
                {
                    tags.Add(tag);
                }
                foreach (string tag in PostToSave.PostToDownload.tags.general)
                {
                    tags.Add(tag);
                }
                foreach (string tag in PostToSave.PostToDownload.tags.lore)
                {
                    tags.Add(tag);
                }
                foreach (string tag in PostToSave.PostToDownload.tags.meta)
                {
                    tags.Add(tag);
                }
                foreach (string tag in PostToSave.PostToDownload.tags.invalid)
                {
                    tags.Add(tag);
                }

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    PostToSave.ProgressColor = new SolidColorBrush(Colors.White);
                    PostToSave.Value = 0;
                });
                var filter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter()
                {
                    CookieUsageBehavior = Windows.Web.Http.Filters.HttpCookieUsageBehavior.Default
                };
                HttpClient client = new HttpClient(filter); // Create HttpClient
                Uri requestUri = new Uri(PostToSave.PostToDownload.file.url);
                HttpResponseMessage httpResponse = new HttpResponseMessage();
                string httpResponseBody = "";

                try
                {
                    //Send the GET request
                    var pb = client.GetAsync(requestUri);
                    pb.Progress = async (res, progressInfo) =>
                    {
                        try
                        {
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                            {
                                PostToSave.Value = (double)(100.0 * progressInfo.BytesReceived / PostToSave.PostToDownload.file.size);
                            });
                        }
                        catch (Exception)
                        {

                        }
                        Thread.Sleep(100);
                    };
                    httpResponse = await pb;
                    httpResponse.EnsureSuccessStatusCode();
                }
                catch (Exception)
                {
                    ShowSystemMessage("Save Error: File Missing");
                }
                //byte[] ByteArray = await client.GetByteArrayAsync(PostToSave.file.url); // Download file
                //IBuffer buffer = ByteArray.AsBuffer();

                StorageFile file = null;
                try
                {
                    if (PostToSave.FolderName != "")
                    {
                        string foldername = PostToSave.FolderName;
                        foldername = foldername.Replace(":", "");
                        foldername = foldername.Replace("<", "");
                        foldername = foldername.Replace(">", "");
                        foldername = foldername.Replace("\"", "");
                        foldername = foldername.Replace("\\", "");
                        foldername = foldername.Replace("/", "");
                        foldername = foldername.Replace("|", "");
                        foldername = foldername.Replace("?", "");
                        foldername = foldername.Replace("*", "");
                        foldername = foldername.Replace("_", " ");
                        file = await DownloadsFolder.CreateFileAsync($"{foldername}_{PostToSave.FileName}.{PostToSave.PostToDownload.file.ext}");
                    }
                    else
                    {
                        file = await DownloadsFolder.CreateFileAsync(PostToSave.FileName + "." + PostToSave.PostToDownload.file.ext);
                    }
                }
                catch (Exception)
                {
                    ShowSystemMessage("File Already Exists!");
                    return;
                }

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    PostToSave.ProgressColor = new SolidColorBrush(Colors.Blue);
                });

                if (PostToSave.PostToDownload.file.ext == "webm" || PostToSave.PostToDownload.file.ext == "gif")
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        IInputStream inputStream = await httpResponse.Content.ReadAsInputStreamAsync();
                        ulong totalBytesRead = 0;
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            PostToSave.ProgressColor = new SolidColorBrush(Colors.Magenta);
                            PostToSave.Value = 0;
                        });
                        while (true)
                        {
                            // Read from the web.
                            IBuffer buffer = new Windows.Storage.Streams.Buffer(1024);
                            buffer = await inputStream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.None);
                            if (buffer.Length == 0)
                            {
                                break;
                            }
                            totalBytesRead += buffer.Length;
                            await stream.WriteAsync(buffer);
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                            {
                                PostToSave.Value = (double)(100.0 * totalBytesRead / PostToSave.PostToDownload.file.size);
                            });
                        }
                        inputStream.Dispose();
                        await stream.FlushAsync();
                    }

                    ShowSystemMessage("Posts saved (Without Tags)");
                    return;
                }
                else
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (string tag in tags)
                    {
                        builder.Append($"{tag};");
                    }
                    string keywords = builder.ToString();

                    SoftwareBitmap softwareBitmap;

                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        IInputStream inputStream = await httpResponse.Content.ReadAsInputStreamAsync();
                        ulong totalBytesRead = 0;
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            PostToSave.ProgressColor = new SolidColorBrush(Colors.Magenta);
                            PostToSave.Value = 0;
                        });
                        while (true)
                        {
                            // Read from the web.
                            IBuffer buffer = new Windows.Storage.Streams.Buffer(1024);
                            buffer = await inputStream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.None);
                            if (buffer.Length == 0)
                            {
                                break;
                            }
                            totalBytesRead += buffer.Length;
                            await stream.WriteAsync(buffer);
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                            {
                                PostToSave.Value = (double)(100.0 * totalBytesRead / PostToSave.PostToDownload.file.size);
                            });
                        }
                        inputStream.Dispose();
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                        // Get the SoftwareBitmap representation of the file
                        softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                        encoder.SetSoftwareBitmap(softwareBitmap);

                        var propertySet = new Windows.Graphics.Imaging.BitmapPropertySet();
                        var kwds = new BitmapTypedValue(keywords, Windows.Foundation.PropertyType.String);
                        propertySet.Add(new KeyValuePair<string, BitmapTypedValue>("System.Keywords", kwds));

                        await encoder.BitmapProperties.SetPropertiesAsync(propertySet);

                        await encoder.FlushAsync();
                    }
                }
                client.Dispose();
                ShowSystemMessage("Saved Image!");
            }
            catch (Exception e)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    ShowSystemMessage("Saving Error");
                });
            }
        }
        #endregion Image Saving

        private async void GetLikedPosts()
        {
            LoadVotesFromFile();
            host.Username = SettingsHandler.Username;
            host.ApiKey = SettingsHandler.ApiKey;
            int count = 1;
            while (true)
            {
                // Get liked posts
                var posts = await host.GetPosts($"votedup:{SettingsHandler.Username}", 75, count);

                if(posts == null || posts.Count == 0)
                {
                    break;
                }

                foreach (var post in posts)
                {
                    bool needsBreak = false;
                    try
                    {
                        SettingsHandler.VotedPosts.Add(post.id, true);
                    }
                    catch (Exception)
                    {
                        needsBreak = true;
                    }
                    if (needsBreak) { break; }
                }
                count++;
            }
            count = 1;
            while (true) 
            { 
                // Get liked posts
                var posts2 = await host.GetPosts($"voteddown:{SettingsHandler.Username}", 75, count);

                if (posts2 == null || posts2.Count == 0)
                {
                    break;
                }

                foreach (var post in posts2)
                {
                    bool needsBreak = false;
                    try
                    {
                        SettingsHandler.VotedPosts.Add(post.id, false);
                    }
                    catch (Exception)
                    {
                        needsBreak = true;
                    }
                    if (needsBreak) { break; }
                }
                count++;
            }
            SaveVotesToFile();
            //ShowSystemMessage("Voted index complete");
        }

        private async void SaveVotesToFile()
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();

                binaryFormatter.Serialize(memoryStream, SettingsHandler.VotedPosts);

                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = await storageFolder.CreateFileAsync("LikedPosts.bin", CreationCollisionOption.ReplaceExisting);
                var stream = await file.OpenStreamForWriteAsync();
                stream.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
                stream.Flush();
                stream.Close();
            }

        }

        private async void LoadVotesFromFile()
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                    StorageFile file = await storageFolder.GetFileAsync("LikedPosts.bin");
                    var stream = await file.OpenStreamForReadAsync();

                    var binaryFormatter = new BinaryFormatter();

                    stream.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    SettingsHandler.VotedPosts = (Dictionary<int, bool>)binaryFormatter.Deserialize(memoryStream);
                }
            }
            catch (Exception)
            {

            }
        }

        // Unused update checker for github updates, but waiting on microsoft store review :D
        private async void CheckForUpdate()
        {
            var github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("Fluff"));
            var releases = await github.Repository.Release.GetAll("EpsiRho", "Fluff");
            if(releases.Count == 0)
            {
                return;
            }

            var latest = releases[0];

            try
            {
                double latestNum = Convert.ToDouble(latest.TagName);

                if (latestNum > 0.5)
                {
                    UpdateTitle.Text = latest.Name;
                    UpdateDescription.Text = latest.Body;
                    UpdateDialog.ShowAsync();
                }
            }
            catch(Exception)
            {
                ShowSystemMessage("Couldn't Check for Update");
            }
        }
        private async void UpdateDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var uri = new Uri("https://github.com/EpsiRho/Fluff/releases");

            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
        }





    }
}
