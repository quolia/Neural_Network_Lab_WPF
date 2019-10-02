using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        string Extender;

        static Dictionary<string, Dictionary<string, string>> Cache = new Dictionary<string, Dictionary<string, string>>();

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
                Extender = "." + extender
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
            return String.IsNullOrEmpty(value) ? new long[0] : value.Split(new[] { ',' }).Select(s => long.Parse(s.Trim())).ToArray();
        }

        public long[] GetArray(string name, string defaultValue = null)
        {
            if (defaultValue == null)
            {
                defaultValue = string.Empty;
            }

            string value = GetValue(name, defaultValue);
            return String.IsNullOrEmpty(value) ? new long[0] : value.Split(new[] { ',' }).Select(s => long.Parse(s.Trim())).ToArray();
        }

        public void Remove(Const.Param name)
        {
            var values = GetValues();
            if (values.TryGetValue(name.ToString("G") + Extender, out _))
            {
                values.Remove(name.ToString("G") + Extender);
            }
            SaveValues(values);
        }

        public void Remove(string name)
        {
            var values = GetValues();
            if (values.TryGetValue(name + Extender, out _))
            {
                values.Remove(name + Extender);
            }
            SaveValues(values);
        }

        public void Set(Const.Param name, string value)
        {
            var values = GetValues();
            values[name.ToString("G") + Extender] = value;
            SaveValues(values);
        }

        public void Set(string name, string value)
        {
            name = CutName(name);

            var values = GetValues();
            values[name + Extender] = value;
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
            Set(name, String.Join(",", list.Select(l => l.ToString())));
        }

        public void Set<T>(string name, IEnumerable<T> list)
        {
            Set(name, String.Join(",", list.Select(l => l.ToString())));
        }

        private string GetValue(Const.Param name, string defaultValue = null)
        {
            var values = GetValues();

            if (values.TryGetValue(name.ToString("G") + Extender, out string value))
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

            if (values.TryGetValue(name + Extender, out string value))
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
            if (Cache.ContainsKey(Name))
            {
                Cache[Name] = values;
            }
            else
            {
                Cache.Add(Name, values);
            }
        }

        public void FlushToDrive()
        {
            if (Cache.ContainsKey(Name))
            {
                var values = Cache[Name];

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
            if (Cache.ContainsKey(Name))
            {
                return Cache[Name];
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

            Cache.Add(Name, result);

            return result;
        }

        public void Clear()
        {
            File.WriteAllLines(Name, Array.Empty<string>());
            Cache.Clear();
        }
    }
}