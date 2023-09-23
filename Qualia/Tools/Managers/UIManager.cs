using System;
using System.Collections.Generic;

namespace Qualia.Tools.Managers;

internal class ExtendedInfo
{
    public static readonly ActionManager.ApplyActionDelegate DefaultHandler = delegate { };

    public Config Config;
    public readonly List<IConfigParam> ConfigParams = new();
    public ActionManager.ApplyActionDelegate OnChanged = DefaultHandler;
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

        if (_dict.TryGetValue(obj, out var info))
        {
            return info;
        }

        info = new ExtendedInfo();
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
        var info = ExtendedInfo.GetInfo(t) ?? throw new InvalidOperationException("Info is not found.");
        info.UIParam = param;
        return t;
    }

    public static Notification.ParameterChanged GetUIParam<T>(this T t) where T : class
    {
        var info = ExtendedInfo.GetInfo(t) ?? throw new InvalidOperationException("Info is not found.");
        return info.UIParam;
    }

    public static T SetConfigParams<T>(this T t, List<IConfigParam> configParams) where T : class
    {
        var info = ExtendedInfo.GetInfo(t) ?? throw new InvalidOperationException("Info is not found.");
        info.ConfigParams.AddRange(configParams);
        return t;
    }

    public static List<IConfigParam> GetConfigParams<T>(this T t) where T : class
    {
        var info = ExtendedInfo.GetInfo(t) ?? throw new InvalidOperationException("Info is not found.");
        return info.ConfigParams;
    }

    public static T PutConfig<T>(this T t, Config config) where T : class
    {
        var info = ExtendedInfo.GetInfo(t) ?? throw new InvalidOperationException("Info is not found.");
        info.Config = config;
        return t;
    }

    public static Config GetConfig<T>(this T t) where T : class 
    {
        var info = ExtendedInfo.GetInfo(t) ?? throw new InvalidOperationException("Info is not found.");
        return info.Config;
    }

    public static T SetUIHandler<T>(this T t, ActionManager.ApplyActionDelegate onChanged) where T : class
    {
        var info = ExtendedInfo.GetInfo(t) ?? throw new InvalidOperationException("Info is not found.");
        if (info.OnChanged != ExtendedInfo.DefaultHandler && info.OnChanged != onChanged)
        {
            throw new InvalidOperationException("OnChange handler has been already set for this object.");
        }

        info.OnChanged = onChanged;
        return t;
    }

    public static ActionManager.ApplyActionDelegate GetUIHandler<T>(this T t) where T : class
    {
        var info = ExtendedInfo.GetInfo(t) ?? throw new InvalidOperationException("Info is not found.");
        return info.OnChanged;
    }

    public static void InvokeUIHandler<T>(this T t, ApplyAction action) where T : class
    {
        var info = ExtendedInfo.GetInfo(t) ?? throw new InvalidOperationException("Info is not found.");
        if (action.Param == Notification.ParameterChanged.Unknown)
        {
            action.Param = t.GetUIParam();
        }

        t.GetUIHandler()(action);
    }
}