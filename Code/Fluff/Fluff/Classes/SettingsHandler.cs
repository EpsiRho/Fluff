using e6API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Fluff.Classes
{
    /// <summary>
    /// This contains the current app settings.
    /// </summary>
    public static class SettingsHandler
    {
        public static string UserAgent = "Fluff/0.9 (by EpsilonRho)";
        public static string Username { get; set; }
        public static string ApiKey { get; set; }
        public static string Rating { get; set; }
        public static bool ShowComments { get; set; }
        public static bool MuteVolume { get; set; }
        public static bool DownloadQuality { get; set; }
        public static double PostCount { get; set; }
        public static int AutoScrollTime { get; set; }

        public static Dictionary<int, bool> VotedPosts { get; set; }
        public static ObservableCollection<string> FavoriteTags { get; set; }
        public static async void SaveFavsToFile()
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();

                var list = FavoriteTags.ToList();

                binaryFormatter.Serialize(memoryStream, list);

                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = await storageFolder.CreateFileAsync("FavoriteTags.bin", CreationCollisionOption.ReplaceExisting);
                var stream = await file.OpenStreamForWriteAsync();
                stream.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
                stream.Flush();
                stream.Close();
            }

        }
    }
}
