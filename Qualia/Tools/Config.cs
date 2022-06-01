using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tools
{
    public interface IConfigValue
    {
        void SetConfig(Config config);
        void LoadConfig();
        void SaveConfig();
        void VanishConfig();
        bool IsValid();
        void SetChangeEvent(Action action);
        void InvalidateValue();
    }

    public class Config
    {
        public static Config Main = new Config("config.txt");
        public readonly string Name;
        public Config ParentConfig;
        
        private string _extender;

        private static readonly Dictionary<string, Dictionary<string, string>> s_cache = new Dictionary<string, Dictionary<string, string>>();

        public Config(string fileName, Config parentConfig = null)
        {
            Name = fileName;
            ParentConfig = parentConfig;
        }

        private string CutName(string name)
        {
            return name.StartsWith("Ctl", StringComparison.InvariantCultureIgnoreCase) ? name.Substring(3) : name;
        }

        public Config Extend(long extender)
        {
            var config = new Config(Name)
            {
                ParentConfig = this,
                _extender = "." + extender
            };

            return config;
        }

        public string GetString(Const.Param paramName, string defaultValue = null)
        {
            return GetValue(paramName, defaultValue);
        }

        public string GetString(string paramName, string defaultValue = null)
        {
            return GetValue(paramName, defaultValue);
        }

        public double? GetDouble(Const.Param paramName, double? defaultValue = null)
        {
            if (Converter.TryTextToDouble(GetValue(paramName, Converter.DoubleToText(defaultValue)), out double? value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        public double? GetDouble(string paramName, double? defaultValue = null)
        {
            if (Converter.TryTextToDouble(GetValue(paramName, Converter.DoubleToText(defaultValue)), out double? value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        public long? GetInt(Const.Param paramName, long? defaultValue = null)
        {
            if (Converter.TryTextToInt(GetValue(paramName, Converter.IntToText(defaultValue)), out long? value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        public long? GetInt(string paramName, long? defaultValue = null)
        {
            if (Converter.TryTextToInt(GetValue(paramName, Converter.IntToText(defaultValue)), out long? value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        public bool GetBool(Const.Param paramName, bool defaultValue = false)
        {
            return 1 == GetInt(paramName, defaultValue ? 1 : 0);
        }

        public bool GetBool(string paramName, bool defaultValue = false)
        {
            return 1 == GetInt(paramName, defaultValue ? 1 : 0);
        }

        public long[] GetArray(Const.Param paramName, string defaultValue = null)
        {
            if (defaultValue == null)
            {
                defaultValue = string.Empty;
            }

            string value = GetValue(paramName, defaultValue);
            return string.IsNullOrEmpty(value) ? Array.Empty<long>() : value.Split(new[] { ',' }).Select(s => long.Parse(s.Trim())).ToArray();
        }

        public long[] GetArray(string paramName, string defaultValue = null)
        {
            if (defaultValue == null)
            {
                defaultValue = string.Empty;
            }

            string value = GetValue(paramName, defaultValue);
            return string.IsNullOrEmpty(value) ? Array.Empty<long>() : value.Split(new[] { ',' }).Select(s => long.Parse(s.Trim())).ToArray();
        }

        public void Remove(Const.Param paramName)
        {
            var values = GetValues();
            if (values.TryGetValue(paramName.ToString("G") + _extender, out _))
            {
                values.Remove(paramName.ToString("G") + _extender);
            }

            SaveValues(values);
        }

        public void Remove(string paramName)
        {
            var values = GetValues();
            if (values.TryGetValue(paramName + _extender, out _))
            {
                values.Remove(paramName + _extender);
            }

            SaveValues(values);
        }

        public void Set(Const.Param paramName, string value)
        {
            var values = GetValues();
            values[paramName.ToString("G") + _extender] = value;

            SaveValues(values);
        }

        public void Set(string paramName, string value)
        {
            paramName = CutName(paramName);

            var values = GetValues();
            values[paramName + _extender] = value;

            SaveValues(values);
        }

        public void Set(Const.Param paramName, double? value)
        {
            Set(paramName, Converter.DoubleToText(value));
        }

        public void Set(string paramName, double? value)
        {
            Set(paramName, Converter.DoubleToText(value));
        }

        public void Set(Const.Param paramName, long? value)
        {
            Set(paramName, Converter.IntToText(value));
        }

        public void Set(string paramName, long? value)
        {
            Set(paramName, Converter.IntToText(value));
        }

        public void Set(Const.Param paramName, bool value)
        {
            Set(paramName, value ? 1 : 0);
        }

        public void Set(string paramName, bool value)
        {
            Set(paramName, value ? 1 : 0);
        }

        public void Set<T>(Const.Param paramName, IEnumerable<T> list)
        {
            Set(paramName, string.Join(",", list.Select(l => l.ToString())));
        }

        public void Set<T>(string paramName, IEnumerable<T> list)
        {
            Set(paramName, string.Join(",", list.Select(l => l.ToString())));
        }

        private string GetValue(Const.Param paramName, string defaultValue = null)
        {
            var values = GetValues();

            if (values.TryGetValue(paramName.ToString("G") + _extender, out string value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        private string GetValue(string paramName, string defaultValue = null)
        {
            paramName = CutName(paramName);

            var values = GetValues();

            if (values.TryGetValue(paramName + _extender, out string value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        private void SaveValues(Dictionary<string, string> values)
        {
            if (s_cache.ContainsKey(Name))
            {
                s_cache[Name] = values;
            }
            else
            {
                s_cache.Add(Name, values);
            }
        }

        public void FlushToDrive()
        {
            if (s_cache.ContainsKey(Name))
            {
                var values = s_cache[Name];

                var lines = new List<string>();
                foreach (var pair in values)
                {
                    lines.Add(pair.Key + ":" + pair.Value);
                }

                File.WriteAllLines(Name, lines);
            }
        }

        private Dictionary<string, string> GetValues()
        {
            if (s_cache.ContainsKey(Name))
            {
                return s_cache[Name];
            }

            var result = new Dictionary<string, string>();

            if (!File.Exists(Name))
            {
                Clear();
            }

            var lines = File.ReadAllLines(Name);

            foreach (var line in lines)
            {
                if (line.Contains(":"))
                {
                    var parts = line.Split(new[] { ':' });
                    if (parts.Length > 1)
                    {
                        result[parts[0]] = string.Join(":", parts.Except(parts.Take(1)));
                    }
                }
            }

            s_cache.Add(Name, result);

            return result;
        }

        public void Clear()
        {
            File.WriteAllLines(Name, Array.Empty<string>());
            s_cache.Clear();
        }
    }
}