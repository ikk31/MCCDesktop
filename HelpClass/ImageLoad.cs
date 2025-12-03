using MCCDesktop.Instruments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MCCDesktop.HelpClass
{

    public class ImageLoad : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string photoPath && !string.IsNullOrEmpty(photoPath))
            {
                if (DataStorage.CachePhoto.ContainsKey(photoPath))
                    return DataStorage.CachePhoto[photoPath];

                try
                {
                    LoadImageAsync(photoPath);
                }
                catch
                {
                }
            }
            return new BitmapImage(new Uri("/assets/ImageEmployee/18601.png", UriKind.Relative));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static async void LoadImageAsync(string photoPath)
        {
            try
            {
                var bytes = await new ApiClient().GetPhoto(photoPath);
                if (bytes != null)
                {
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            if (!DataStorage.CachePhoto.ContainsKey(photoPath))
                            DataStorage.CachePhoto.Add(photoPath, bitmap);
                            RefreshAllBindings();
                        });
                    }

                }
            }
            catch
            {
            }
        }

        private static void RefreshAllBindings()
        {
            foreach (Window window in Application.Current.Windows)
            {
                var itemsControls = FindVisualChildren<ItemsControl>(window);
                foreach (var itemsControl in itemsControls)
                {
                    itemsControl.Items.Refresh();
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }
                    foreach (T childOfChild in FindVisualChildren<T>(child!))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

    }
}
