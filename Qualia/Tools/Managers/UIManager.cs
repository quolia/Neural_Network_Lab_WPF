using System;
using System.Collections.Generic;

namespace Qualia.Tools
{
    public class ExtendedInfo
    {
        public static readonly Action<Notification.ParameterChanged> DefaultHandler = delegate { };

        public Notification.ParameterChanged UIParam;
        public readonly List<IConfigParam> ConfigParams = new();
        public Config Config;
        public Action<Notification.ParameterChanged> OnChanged = DefaultHandler;

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

        public static T SetConfigParams<T>(this T t, List<IConfigParam> configParams) where T : class
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

        public static T PutConfig<T>(this T t, Config config) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException("ExtendedInfo not found.");
            }

            info.Config = config;
            return t;
        }

        public static Config GetConfig<T>(this T t) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException("ExtendedInfo not found.");
            }

            return info.Config;
        }

        public static T SetUIHandler<T>(this T t, Action<Notification.ParameterChanged> onChanged) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException("ExtendedInfo not found.");
            }

            if (info.OnChanged != ExtendedInfo.DefaultHandler)
            {
                throw new InvalidOperationException();
            }

            info.OnChanged = onChanged;
            return t;
        }

        public static Action<Notification.ParameterChanged> GetUIHandler<T>(this T t) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException("ExtendedInfo not found.");
            }

            return info.OnChanged;
        }

        public static void InvokeUIHandler<T>(this T t, Notification.ParameterChanged param = Notification.ParameterChanged.Unknown) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException("ExtendedInfo not found.");
            }

            t.GetUIHandler()(param == Notification.ParameterChanged.Unknown ? t.GetUIParam() : param);
        }
    }
}
