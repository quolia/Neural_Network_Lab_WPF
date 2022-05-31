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

        public Config(string name, Config parentConfig = null)
        {
            Name = name;
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

        public string GetString(Const.Param name, string defaultValue = null)
        {
            return GetValue(name, defaultValue);
        }

        public string GetString(string name, string defaultValue = null)
        {
            return GetValue(name, defaultValue);
        }

        public double? GetDouble(Const.Param name, double? defaultValue = null)
        {
            if (Converter.TryTextToDouble(GetValue(name, Converter.DoubleToText(defaultValue)), out double? value))
            {
                return value;
            }
            else
            {
                Set(name, defaultValue);
                return defaultValue;
            }
        }

        public double? GetDouble(string name, double? defaultValue = null)
        {
            if (Converter.TryTextToDouble(GetValue(name, Converter.DoubleToText(defaultValue)), out double? value))
            {
                return value;
            }
            else
            {
                Set(name, defaultValue);
                return defaultValue;
            }
        }

        public long? GetInt(Const.Param name, long? defaultValue = null)
        {
            if (Converter.TryTextToInt(GetValue(name, Converter.IntToText(defaultValue)), out long? value))
            {
                return value;
            }
            else
            {
                Set(name, defaultValue);
                return defaultValue;
            }
        }

        public long? GetInt(string name, long? defaultValue = null)
        {
            if (Converter.TryTextToInt(GetValue(name, Converter.IntToText(defaultValue)), out long? value))
            {
                return value;
            }
            else
            {
                Set(name, defaultValue);
                return defaultValue;
            }
        }

        public bool GetBool(Const.Param name, bool defaultValue = false)
        {
            return 1 == GetInt(name, defaultValue ? 1 : 0);
        }

        public bool GetBool(string name, bool defaultValue = false)
        {
            return 1 == GetInt(name, defaultValue ? 1 : 0);
        }

        public long[] GetArray(Const.Param name, string defaultValue = null)
        {
            if (defaultValue == null)
            {
                defaultValue = string.Empty;
            }

            string value = GetValue(name, defaultValue);
            return string.IsNullOrEmpty(value) ? new long[0] : value.Split(new[] { ',' }).Select(s => long.Parse(s.Trim())).ToArray();
        }

        public long[] GetArray(string name, string defaultValue = null)
        {
            if (defaultValue == null)
            {
                defaultValue = string.Empty;
            }

            string value = GetValue(name, defaultValue);
            return string.IsNullOrEmpty(value) ? new long[0] : value.Split(new[] { ',' }).Select(s => long.Parse(s.Trim())).ToArray();
        }

        public void Remove(Const.Param name)
        {
            var values = GetValues();
            if (values.TryGetValue(name.ToString("G") + _extender, out _))
            {
                values.Remove(name.ToString("G") + _extender);
            }

            SaveValues(values);
        }

        public void Remove(string name)
        {
            var values = GetValues();
            if (values.TryGetValue(name + _extender, out _))
            {
                values.Remove(name + _extender);
            }

            SaveValues(values);
        }

        public void Set(Const.Param name, string value)
        {
            var values = GetValues();
            values[name.ToString("G") + _extender] = value;

            SaveValues(values);
        }

        public void Set(string name, string value)
        {
            name = CutName(name);

            var values = GetValues();
            values[name + _extender] = value;

            SaveValues(values);
        }

        public void Set(Const.Param name, double? value)
        {
            Set(name, Converter.DoubleToText(value));
        }

        public void Set(string name, double? value)
        {
            Set(name, Converter.DoubleToText(value));
        }

        public void Set(Const.Param name, long? value)
        {
            Set(name, Converter.IntToText(value));
        }

        public void Set(string name, long? value)
        {
            Set(name, Converter.IntToText(value));
        }

        public void Set(Const.Param name, bool value)
        {
            Set(name, value ? 1 : 0);
        }

        public void Set(string name, bool value)
        {
            Set(name, value ? 1 : 0);
        }

        public void Set<T>(Const.Param name, IEnumerable<T> list)
        {
            Set(name, string.Join(",", list.Select(l => l.ToString())));
        }

        public void Set<T>(string name, IEnumerable<T> list)
        {
            Set(name, string.Join(",", list.Select(l => l.ToString())));
        }

        private string GetValue(Const.Param name, string defaultValue = null)
        {
            var values = GetValues();

            if (values.TryGetValue(name.ToString("G") + _extender, out string value))
            {
                return value;
            }
            else
            {
                Set(name, defaultValue);
                return defaultValue;
            }
        }

        private string GetValue(string name, string defaultValue = null)
        {
            name = CutName(name);

            var values = GetValues();

            if (values.TryGetValue(name + _extender, out string value))
            {
                return value;
            }
            else
            {
                Set(name, defaultValue);
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
                    if (parts.Count() > 1)
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