using Qualia.Controls;
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
        public static bool All<T>(this List<T> list,
                                  Func<T, bool> predicate)
        {
            return list.AsEnumerable().All(predicate);
        }

        public static DispatcherOperation Dispatch(this DispatcherObject obj,
                                                   Action action,
                                                   DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return obj.Dispatcher.BeginInvoke(action, priority);
        }

        public static void SetVisible(this UIElement element,
                                      bool visible)
        {
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public static TabItem Tab(this TabControl tab,
                                  int index)
        {
            return (tab.Items[index] as TabItem);
        }

        public static TabItem SelectedTab(this TabControl tab)
        {
            return tab.SelectedIndex > -1 ? tab.Items[tab.SelectedIndex] as TabItem : null;
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

        public static long TotalNanoseconds(this TimeSpan span)
        {
            return (long)(span.TotalMilliseconds * 1000 * 1000);
        }

        public static string GetDirectoryName(string path,
                                              string defaultePath)
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

        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this Dictionary<TKey, TValue> to,
                                                                   Dictionary<TKey, TValue> from)
        {
            return to.Union(from).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <returns>Selected function instance.</returns>
        public static T Fill<T>(this SelectorControl comboBox,
                                Config config,
                                string paramName = null) where T : class
        {
            return Initializer.FillComboBox<T>(SelectorControlWrapper.Wrap(comboBox), config, paramName);
        }

        static class Initializer
        {
            public static T FillComboBox<T>(SelectorControlWrapper comboBoxWrapper,
                                            Config config,
                                            string paramName) where T : class
            {

                var items = BaseFunction<T>.GetItems()
                                           .Select(SelectorControlWrapper.GetSelectableItemForName<T>)
                                           .ToList();


                FillComboBox(items, comboBoxWrapper, config, paramName);

                //comboBox.ToolTip = string.Join("\n\n", BaseFunction<T>.GetItemsWithDescription());
                //comboBox.ToolTip = ToolTipsProvider.GetFunctionToolTip();

                return BaseFunction<T>.GetInstance(comboBoxWrapper);
            }

            private static void FillComboBox(in IEnumerable<ISelectableItem> items,
                                             SelectorControlWrapper comboBox,
                                             Config config,
                                             string paramName)
            {
                comboBox.Clear();

                foreach (var item in items)
                {
                    comboBox.AddItem(item);
                }


                var defaultValue = comboBox.DefaultValue;

                if (string.IsNullOrEmpty(paramName))
                {
                    paramName = comboBox.Name;
                }

                var selectedItemName = config.Get(paramName, !string.IsNullOrEmpty(defaultValue) ? defaultValue : items.Count() > 0 ? items.First().Text : null);
                var selectedItem = items.First(i => i.Text == selectedItemName);

                if (comboBox.Count > 0)
                {
                    if (!comboBox.Contains(selectedItem))
                    {
                        selectedItem = comboBox.GetItemAt(0);
                    }
                }
                else
                {
                    selectedItem = null;
                }

                if (selectedItem != null)
                {
                    comboBox.SelectedItem = selectedItem;
                }
            }
        }
    }
}
