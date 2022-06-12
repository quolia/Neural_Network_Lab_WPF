using System;
using System.Linq;

namespace Qualia.Tools
{
    unsafe public class BaseFunction<T> where T : class
    {
        public static T GetInstance(object name)
        {
            var funcName = name.ToString();

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
    }
}
