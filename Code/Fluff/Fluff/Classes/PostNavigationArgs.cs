using e6API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fluff.Classes
{
    /// <summary>
    /// Some args that can be pushed through page transitions. should be non static in the future, pls fix.
    /// </summary>
    public static class PostNavigationArgs
    {
<<<<<<< Updated upstream
        public static Post ClickedPost { get; set; }
        public static ObservableCollection<Post> PostsList { get; set; }
        public static int Page { get; set; }
        public static bool NeedsRefersh { get; set; }
        public static string Tags { get; set; }
=======
        public Post ClickedPost { get; set; }
        public Pool ClickedPool { get; set; }
        public ObservableCollection<Post> PostsList { get; set; }
        public ObservableCollection<Pool> PoolsList { get; set; }
        public int Page { get; set; }
        public string Tags { get; set; }
    }

    public static class PagesStack
    {
        public static List<PostNavigationArgs> ArgsStack;
>>>>>>> Stashed changes
    }
}
