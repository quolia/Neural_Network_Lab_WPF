using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Qualia.Tools
{
    public static class Extension
    {
        public static bool All<T>(this List<T> list, Func<T, bool> predicate)
        {
            return list.AsEnumerable().All(predicate);
        }

        public static DispatcherOperation Dispatch(this UIElement element, Action action, DispatcherPriority priority)
        {
            return element.Dispatcher.BeginInvoke(action, priority);
        }

        public static void Visible(this UIElement element, bool visible)
        {
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public static TabItem Tab(this TabControl tab, int index)
        {
            return (tab.Items[index] as TabItem);
        }

        public static TabItem SelectedTab(this TabControl tab)
        {
            return (tab.Items[tab.SelectedIndex] as TabItem);
        }

        public static Color GetColor(this Brush brush)
        {
            return ((SolidColorBrush)brush).Color;
        }

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : class
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); ++i)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (child as T);
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(this ContentControl depObj) where T : class
        {
            if (depObj != null)
            {
                if (depObj.Content != null && depObj.Content is T)
                {
                    yield return depObj.Content as T;
                }

                if (depObj.Content is ContentControl)
                {
                    foreach (T childOfChild in FindVisualChildren<T>(depObj.Content as ContentControl))
                    {
                        yield return childOfChild;
                    }
                }
                else if (depObj.Content is DependencyObject)
                {
                    foreach (T childOfChild in FindVisualChildren<T>(depObj.Content as DependencyObject))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static T GetParentOfType<T>(this FrameworkElement element) where T : class
        {
            if (element.Parent == null)
            {
                return null;
            }

            if (element.Parent is T)
            {
                return element.Parent as T;
            }
            else if (element.Parent is FrameworkElement)
            {
                return (element.Parent as FrameworkElement).GetParentOfType<T>();
            }

            return null;
        }

        public static long TotalMicroseconds(this TimeSpan span)
        {
            return (long)(span.TotalMilliseconds * 1000);
        }

        public static string GetDirectoryName(string path, string defaultePath)
        {
            try
            {
                return Path.GetDirectoryName(path);
            }
            catch
            {
                return defaultePath;
            }
        }
    }

    public static class Initializer
    {
        private static string CutName(string name)
        {
            var prefix = "Ctl";
            return name.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase) ? name.Substring(prefix.Length) : name;
        }

        public static void FillComboBox<T>(ComboBox comboBox, Config config, string defaultValue) where T : class
        {
            var items = typeof(BaseFunction<T>).GetMethod("GetItems").Invoke(null, null);
            FillComboBox(items as string[], comboBox, config, defaultValue);
        }

        public static void FillComboBox(Func<string[]> getItemsFunc, ComboBox comboBox, Config config, string defaultValue)
        {
            FillComboBox(getItemsFunc(), comboBox, config, defaultValue);
        }

        public static void FillComboBox(in string[] items, ComboBox cb, Config config, string defaultValue)
        {
            var paramName = CutName(cb.Name);

            cb.Items.Clear();

            foreach (var i in items)
            {
                cb.Items.Add(i);
            }

            var item = config.GetString(paramName, !string.IsNullOrEmpty(defaultValue) ? defaultValue : items.Length > 0 ? items[0] : null);
            if (items.Length > 0)
            {
                if (!items.Contains(item))
                {
                    item = items[0];
                }
            }
            else
            {
                item = null;
            }

            if (!string.IsNullOrEmpty(item))
            {
                cb.SelectedItem = item;
            }
        }
    }
}
