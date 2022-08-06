using System;
using System.Collections.Generic;

namespace Qualia.Tools
{
    public class ExtendedInfo
    {
        public Notification.ParameterChanged UIParam { get; set; }

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

    public static class ManagerUI
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
    }
}
