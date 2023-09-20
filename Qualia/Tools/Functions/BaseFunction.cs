using System;
using System.Linq;
using Qualia.Controls.Base;
using Qualia.Controls.Base.Values;

namespace Qualia.Tools.Functions;

public unsafe class BaseFunctionInfo
{
    public static string[] GetItemsByType(Type funcType)
    {
        return (string[])funcType.GetMethod("GetItems").Invoke(null, null);
    }
}

public unsafe class BaseFunction<T> : BaseFunctionInfo where T : class
{
    public readonly string DefaultFunction;

    public BaseFunction(string defaultFunction)
    {
        DefaultFunction = defaultFunction;
    }

    public static string GetDefaultFunctionName()
    {
        var functions = GetItems();
        return (GetInstanceByName(functions[0]) as BaseFunction<T>).DefaultFunction;
    }

    public static T GetInstance(SelectorControl selector)
    {
        if (selector.SelectedItem == null)
        {
            return default(T);
        }

        return GetInstanceByName(selector.SelectedItem.Text);
    }

    public static T GetInstance(SelectorControlWrapper selector)
    {
        if (selector.SelectedItem == null)
        {
            return default(T);
        }

        return GetInstanceByName(selector.SelectedItem.Text);
    }

    public static T GetInstance(FunctionControl function)
    {
        return function.SelectedFunction == null ? default : GetInstanceByName(function.SelectedFunction.Name);
    }

    public static T GetInstanceByName(string name)
    {
        var funcName = name;

        if (!GetItems().Contains(funcName)) // Call it just to tell compiler to compile GetItems, but not to exclude it.
        {
            throw new InvalidOperationException($"Unknown function name: {funcName}.");
        }

        var type = typeof(T).GetNestedTypes().FirstOrDefault(type => type.Name == funcName);
        if (type == null)
        {
            throw new InvalidOperationException($"Unknown function name: {funcName}.");
        }

        return (T)type.GetField("Instance").GetValue(null);
    }

    public string GetNameByInstance(BaseFunction<T> function)
    {
        var types = typeof(T).GetNestedTypes();

        foreach (var type in types)
        {
            var instance = (T)type.GetField("Instance").GetValue(null);
            if (instance == function)
            {
                return type.Name;
            }
        }

        return string.Empty;
    }

    public static string[] GetItems()
    {
        return GetItems(false);
    }

    public static string[] GetItemsWithDescription()
    {
        return GetItems(true);
    }

    private static string[] GetItems(bool uncludeDescription)
    {
        return typeof(T).GetNestedTypes()
            .Select(type => type.Name + (uncludeDescription ? "\n" + GetDescription(type.Name) : ""))
            .ToArray();
    }

    public static string GetDescription(SelectorControl selector)
    {
        return selector.SelectedItem == null ? null : GetDescription(selector.SelectedItem.Text);
    }

    private static string GetDescription(object name)
    {
        var funcName = name.ToString();

        var type = typeof(T).GetNestedTypes()
            .Where(type => type.Name == funcName)
            .FirstOrDefault();

        string description = null;
        var fieldInfo = type.GetField("Description");
        if (fieldInfo != null)
        {
            description = (string)fieldInfo.GetValue(null);
            description = string.IsNullOrEmpty(description) ? null : description.Replace(" ... ", "\n");
        }

        string derivativeDescription = null;
        fieldInfo = type.GetField("DerivativeDescription");
        if (fieldInfo != null)
        {
            derivativeDescription = (string)fieldInfo.GetValue(null);
            derivativeDescription = string.IsNullOrEmpty(description) ? null : derivativeDescription.Replace(" ... ", "\n");
        }

        if (!string.IsNullOrEmpty(derivativeDescription))
        {
            if (!string.IsNullOrEmpty(description))
            {
                description += "\n";
            }

            description += derivativeDescription;
        }

        return description ?? "No description.";
    }
}