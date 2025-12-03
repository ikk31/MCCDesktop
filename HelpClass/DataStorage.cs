using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MCCDesktop.HelpClass
{
    public class DataStorage
    {
        //public static readonly Dictionary<string, BitmapImage> _cache = new Dictionary<string, BitmapImage>();
        public static Dictionary<string, BitmapImage> CachePhoto {  get; set; } = new Dictionary<string, BitmapImage>();


    }
}
