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
    public class PostNavigationArgs
    {
        public Post ClickedPost { get; set; }
        public Pool ClickedPool { get; set; }
        public Set ClickedSet { get; set; }
        public ObservableCollection<Post> PostsList { get; set; }
        public ObservableCollection<Pool> PoolsList { get; set; }
        public ObservableCollection<Set> SetsList { get; set; }
        public int Page { get; set; }
        public bool IsSetSearch { get; set; }
        public string Tags { get; set; }
        public int SortOrder { get; set; }
        public double ScrollPercent { get; set; }
        public bool IsPopDay { get; set; }
        public bool IsPopWeek { get; set; }
        public bool IsPopMonth { get; set; }
    }

    public static class PagesStack
    {
        public static List<PostNavigationArgs> ArgsStack;
    }
}
