using Qualia.Controls;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Qualia.Tools
{
    public class ExtendedInfo
    {
        public Notification.ParameterChanged UIParam { get; set; }
        public readonly List<IConfigParam> ConfigParams = new();

        private static readonly Dictionary<object, ExtendedInfo> _dict = new();

        public static ExtendedInfo GetInfo(object o)
        {
            if (o == null)
            {
                return null;
            }

            if (_dict.ContainsKey(o))
            {
                return _dict[o];
            }

            var info = new ExtendedInfo();
            _dict.Add(o, info);

            return info;
        }
    }

    public static class UIManager
    {
        public static T SetUIParam<T>(this T t, Notification.ParameterChanged param) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException("ExtendedInfo not found.");
            }

            info.UIParam = param;
            return t;
        }

        public static Notification.ParameterChanged GetUIParam<T>(this T t) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException("ExtendedInfo not found.");
            }

            return info.UIParam;
        }

        public static T SetConfigParams<T>(this T t, IList<IConfigParam> configParams) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException("ExtendedInfo not found.");
            }

            info.ConfigParams.AddRange(configParams);
            return t;
        }

        public static List<IConfigParam> GetConfigParams<T>(this T t) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException("ExtendedInfo not found.");
            }

            return info.ConfigParams;
        }

        public static void UI_OnChanged(Notification.ParameterChanged param)
        {
            var main = Main.Instance;

            if (param == Notification.ParameterChanged.DynamicSettings)
            {
                // Skip "Apply changes" button.
            }
            else if (param == Notification.ParameterChanged.PreventComputerFromSleep)
            {
                main.CtlNoSleepLabel.Visibility = Visibility.Visible;
            }
            else if (param == Notification.ParameterChanged.DisablePreventComputerFromSleep)
            {
                main.CtlNoSleepLabel.Visibility = Visibility.Collapsed;
            }
            else if (param == Notification.ParameterChanged.IsPreventRepetition)
            {
                var taskFunction = TaskFunction.GetInstance(main.CtlInputDataPresenter.CtlTaskFunction);
                taskFunction.VisualControl.SetIsPreventRepetition(main.CtlInputDataPresenter.CtlIsPreventRepetition.Value);
            }
            else if (param == Notification.ParameterChanged.NeuronsCount)
            {
                main.OnNetworkStructureChanged();
            }
            else
            {
                main.TurnApplyChangesButtonOn(true);
                main.CtlMenuStart.IsEnabled = false;
            }
        }
    }
}
