using System;
using System.Linq;

namespace Qualia.Tools
{
    unsafe public class BaseFunction<T> where T : class
    {
        public static string DefaultValue = null;

        public BaseFunction(string defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public static T GetInstance(object name)
        {
            var funcName = name.ToString();

            if (!GetItems().Contains(funcName)) // Call it just to tell compiler to compile GetItems, but not to exclude it.
            {
                throw new InvalidOperationException($"Unknown function name: {funcName}.");
            }

            var type = typeof(T).GetNestedTypes()
                                .Where(type => type.Name == funcName)
                                .FirstOrDefault();

            if (type == null)
            {
                throw new InvalidOperationException($"Unknown function name: {funcName}.");
            }

            return (T)type.GetField("Instance").GetValue(null);
        }

        public static string[] GetItems()
        {
            return typeof(T).GetNestedTypes()
                            .Select(type => type.Name)
                            .ToArray();
        }

        public static string GetDescription(object name)
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
                description = string.IsNullOrEmpty(description) ? null : description;
            }
            
            return description ?? "No description.";
        }
    }
}
