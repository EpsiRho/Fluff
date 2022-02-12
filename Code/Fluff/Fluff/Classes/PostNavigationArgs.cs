using e6API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fluff.Classes
{
    public static class PostNavigationArgs
    {
        public static Post ClickedPost { get; set; }
        public static ObservableCollection<Post> PostsList { get; set; }
        public static int Page { get; set; }
    }
}
