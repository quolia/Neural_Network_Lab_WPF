﻿using System;
using System.Collections.Generic;

namespace Qualia.Tools
{
    internal class ExtendedInfo
    {
        public static readonly Action<Notification.ParameterChanged> DefaultHandler = delegate { };

        public Config Config;
        public readonly List<IConfigParam> ConfigParams = new();
        public Action<Notification.ParameterChanged> OnChanged = DefaultHandler;
        public Notification.ParameterChanged UIParam;

        private static readonly Dictionary<object, ExtendedInfo> _dict = new();

        public static void Clear()
        {
            _dict.Clear();
        }

        public static ExtendedInfo GetInfo(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            if (_dict.ContainsKey(obj))
            {
                return _dict[obj];
            }

            var info = new ExtendedInfo();
            _dict.Add(obj, info);

            return info;
        }
    }

    public static class UIManager
    {
        public static void Clear()
        {
            ExtendedInfo.Clear();
        }

        public static T SetUIParam<T>(this T t, Notification.ParameterChanged param) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException();
            }

            info.UIParam = param;
            return t;
        }

        public static Notification.ParameterChanged GetUIParam<T>(this T t) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException();
            }

            return info.UIParam;
        }

        public static T SetConfigParams<T>(this T t, List<IConfigParam> configParams) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException();
            }

            info.ConfigParams.AddRange(configParams);
            return t;
        }

        public static List<IConfigParam> GetConfigParams<T>(this T t) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException();
            }

            return info.ConfigParams;
        }

        public static T PutConfig<T>(this T t, Config config) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException();
            }

            info.Config = config;
            return t;
        }

        public static Config GetConfig<T>(this T t) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException();
            }

            return info.Config;
        }

        public static T SetUIHandler<T>(this T t, Action<Notification.ParameterChanged> onChanged) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException();
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
                throw new InvalidOperationException();
            }

            return info.OnChanged;
        }

        public static void InvokeUIHandler<T>(this T t, Notification.ParameterChanged param = Notification.ParameterChanged.Unknown) where T : class
        {
            var info = ExtendedInfo.GetInfo(t);
            if (info == null)
            {
                throw new InvalidOperationException();
            }

            t.GetUIHandler()(param == Notification.ParameterChanged.Unknown ? t.GetUIParam() : param);
        }
    }
}
