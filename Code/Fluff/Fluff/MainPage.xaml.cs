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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

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
        bool SysMessIsOpen;

        public MainPage()
        {
            this.InitializeComponent();
            contentFrame.Navigate(typeof(PostsSearch));
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            PostNavigationArgs.Page = 1;
            PostNavigationArgs.PostsList = new ObservableCollection<e6API.Post>();
            DownloadQueueModel = new ObservableCollection<DownloadQueueItem>();
            SelectedIndex = 0;
            host = new RequestHost("Fluff/0.3(by EpsilonRho)");
            GetSettings();
            SetSettingsUI();
        }

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

        private void SetSettingsUI()
        {
            RatingSelection.SelectedItem = SettingsHandler.Rating;
            PostCountSlider.Value = SettingsHandler.PostCount;
            CommentSwitch.IsOn = SettingsHandler.ShowComments;
            VolumeSwitch.IsOn = SettingsHandler.MuteVolume;
            if(SettingsHandler.Username != "")
            {
                UsernameEntry.Text = SettingsHandler.Username;
            }
            if (SettingsHandler.ApiKey != "")
            {
                ApiKeyEntry.Text = SettingsHandler.ApiKey;
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var MyFrame = contentFrame;
            if (MyFrame.CanGoBack)
            {
                MyFrame.GoBack(new SuppressNavigationTransitionInfo());
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog.ShowAsync();
        }

        private void PostsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenPostsFull.Begin();
            ClosePoolsFull.Begin();
            CloseWikiFull.Begin();
            CloseUploadFull.Begin();
            contentFrame.Navigate(typeof(PostsSearch));
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

        private void WikiButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (WikiUnderline.ScaleX != 1)
            {
                OpenWikiHalf.Begin();
            }
        }

        private void WikiButton_Click(object sender, RoutedEventArgs e)
        {
            ClosePostsFull.Begin();
            ClosePoolsFull.Begin();
            OpenWikiFull.Begin();
            CloseUploadFull.Begin();
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
                    file = await Windows.Storage.DownloadsFolder.CreateFileAsync(PostToSave.PostToDownload.file.md5 + "." + PostToSave.PostToDownload.file.ext);
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
                localSettings.Values["username"] = u;
                localSettings.Values["apikey"] = a;
                UsernameSet.Text = u;
            }
            else
            {

            }
            LoginProgress.Visibility = Visibility.Collapsed;
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
    }
}
