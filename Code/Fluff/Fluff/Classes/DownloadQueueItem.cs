using e6API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace Fluff.Classes
{
    public class DownloadQueueItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public Post PostToDownload { get; set; }
        private SolidColorBrush progressColor;
        public SolidColorBrush ProgressColor
        {
            get { return progressColor; }
            set
            {
                if (value != progressColor)
                {
                    progressColor = value;
                    NotifyPropertyChanged("ProgressColor");
                }
            }
        }
        public double _value { get; set; }
        public double Value
        {
            get { return _value; }
            set
            {
                if (value != _value)
                {
                    _value = value;
                    NotifyPropertyChanged("Value");
                }
            }
        }
    }
}
