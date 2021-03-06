using e6API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Fluff.Classes
{
    /// <summary>
    /// This allows for sending property changed calls to update the UI when the current post changes.
    /// </summary>
    public class CurrentPostNotif : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Post currentPost;
        public Post CurrentPost
        {
            get { return currentPost; }
            set
            {
                if(currentPost != value)
                {
                    currentPost = value;
                    NotifyPropertyChanged("CurrentPost");
                }
            }
        }

        private Visibility showHighQuality;
        public Visibility ShowHighQuality
        {
            get { return showHighQuality; }
            set
            {
                if (showHighQuality != value)
                {
                    showHighQuality = value;
                    NotifyPropertyChanged("ShowHighQuality");
                }
            }
        }
    }
}
