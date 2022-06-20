using Qualia.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace Qualia.Tools
{
    public interface IXmlConfigParam
    {
        string ToXml();
    }

    public interface IConfigParam : IXmlConfigParam
    {
        void SetConfig(Config config);
        void LoadConfig();
        void SaveConfig();
        void RemoveFromConfig();
        bool IsValid();
        void SetChangeEvent(Action action);
        void InvalidateValue();
    }

    sealed public class Config
    {
        public static Config Main = new(App.WorkingDirectory + "config.txt");

        private readonly string _fileName;
        private readonly Config _parentConfig;

        private readonly string _extender;

        private static readonly Dictionary<string, Dictionary<string, string>> s_cacheLoad = new();
        private static readonly Dictionary<string, Dictionary<string, string>> s_cacheSave = new();

        public Config(string fileName)
        {
            _fileName = fileName;
        }

        public Config(string fileName, Config parentConfig, string extender)
        {
            _fileName = fileName;
            _parentConfig = parentConfig;

            _extender = extender;
        }

        private static string CutParamPrefix(string paramName)
        {
            return paramName.StartsWith("Ctl", StringComparison.InvariantCultureIgnoreCase) ? paramName.Substring(3) : paramName;
        }

        private static string CutValueDescription(string value)
        {
            return string.IsNullOrEmpty(value) ? value : value.Split(new[] { '\n' })[0];
        }

        public static string PrepareParamName(string paramName)
        {
            return CutParamPrefix(paramName);
        }

        private static string PrepareValue(string value)
        {
            return CutValueDescription(value);
        }

        /*
        private Config _Extend(object extender)
        {
            extender = PrepareParamName(extender.ToString());

            Config config = new(_fileName, this);
            config._extender = extender + ".";

            return config;
        }
        */
        /*
        public Config Extend(object extender)
        {
            extender = PrepareParamName(extender.ToString());

            Config config = new(_fileName, this);
            config._extender = _extender + extender + ".";

            return config;
        }
*/
        public Config Extend(string extender)
        {
            return new(_fileName, this, PrepareParamName(extender) + ".");
        }

        public Config Extend(SelectValueControl select)
        {
            return Extend(select.Name)
                  .Extend((select.SelectedItem ?? select.DefaultValue).ToString());
        }

        public Config Extend(FunctionControl function)
        {
            return Extend(function.CtlFunction);
        }
        public Config Extend(FrameworkElement element)
        {
            return Extend(element.Name);
        }

        public Config Extend(long extender)
        {
            return Extend(Converter.IntToText(extender));
        }

        private string GetFullParamName(string paramName)
        {
            string name = "";

            Config config = this;

            while (config != null)
            {
                name = config._extender + name;
                config = config._parentConfig;
            }

            return name + paramName;
        }


        public string Get(FrameworkElement paramName, string defaultValue) 
        {
            return Get(paramName.Name, defaultValue);
        }

        public string Get(Constants.Param paramName, string defaultValue) 
        {
            return Get(ParamToStr(paramName), defaultValue);
        }

        public string Get(string paramName, string defaultValue)
        {
            paramName = PrepareParamName(paramName);

            var values = GetLoaded();

            if (values.TryGetValue(GetFullParamName(paramName), out string value))
            {
                return value;
            }

            Set(paramName, defaultValue);
            return defaultValue;
        }

        public int Get(Constants.Param paramName, int defaultValue)
        {
            return Get(ParamToStr(paramName), defaultValue);
        }

        public int Get(FrameworkElement paramName, int defaultValue)
        {
            return Get(paramName.Name, defaultValue);
        }

        public int Get(string paramName, int defaultValue)
        {
            return (int)Get(paramName, (long)defaultValue);
        }

        public long Get(Constants.Param paramName, long defaultValue)
        {
            return Get(ParamToStr(paramName), defaultValue);
        }

        public long Get(FrameworkElement paramName, long defaultValue)
        {
            return Get(paramName.Name, defaultValue);
        }

        public long Get(string paramName, long defaultValue)
        {
            long value = Converter.TextToInt(Get(paramName,
                                                 Converter.IntToText(defaultValue)),
                                             defaultValue);
            Set(paramName, value);
            return value;
        }

        public bool Get(FrameworkElement paramName, bool defaultValue)
        {
            return Get(paramName.Name, defaultValue);
        }

        public bool Get(Constants.Param paramName, bool defaultValue)
        {
            return Get(ParamToStr(paramName), defaultValue);
        }

        public bool Get(string paramName, bool defaultValue)
        {
            return 1 == Get(paramName, (long)(defaultValue ? 1 : 0));
        }

        public double Get(FrameworkElement paramName, double defaultValue)
        {
            return Get(paramName.Name, defaultValue);
        }

        public double Get(Constants.Param paramName, double defaultValue)
        {
            return Get(ParamToStr(paramName), defaultValue);
        }

        public double Get(string paramName, double defaultValue)
        {
            var value = Converter.TextToDouble(Get(paramName,
                                                   Converter.DoubleToText(defaultValue)),
                                               defaultValue);
            Set(paramName, value);
            return value;
        }

        public long[] Get(Constants.Param paramName, long[] defaultValue)
        {
            return Get(ParamToStr(paramName), defaultValue);
        }

        public long[] Get(string paramName, long[] defaultValue)
        {
            var value = Get(paramName, string.Join(",", defaultValue));
            return string.IsNullOrEmpty(value)
                   ? defaultValue
                   : value.Split(new[] { ',' }).Select(s => Converter.TextToInt(s.Trim(), 0)).ToArray();
        }

        public void Remove(Constants.Param paramName)
        {
            Remove(ParamToStr(paramName));
        }

        public void Remove(FrameworkElement paramName)
        {
            Remove(paramName.Name);
        }

        public void Remove(string paramName)
        {
            paramName = PrepareParamName(paramName);

            var values = GetSaved();
            if (values.TryGetValue(GetFullParamName(paramName), out _))
            {
                values.Remove(GetFullParamName(paramName));
            }

            SaveValues(values);

            values = GetLoaded();
            values.Remove(GetFullParamName(paramName));
        }

        public void Set(FrameworkElement paramName, string value)
        {
            Set(paramName.Name, value);
        }

        public void Set(Constants.Param paramName, string value)
        {
            Set(ParamToStr(paramName), value);
        }

        public void Set(string paramName, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            paramName = GetFullParamName(PrepareParamName(paramName));
            value = GetFullParamName(PrepareValue(value));

            var valuesToSave = GetSaved();
            valuesToSave[paramName] = value;

            var valuesLoaded = GetLoaded();
            valuesLoaded[paramName] = value;

            SaveValues(valuesToSave.Merge(valuesLoaded));
        }

        public void Set(FrameworkElement paramName, double value)
        {
            Set(paramName.Name, Converter.DoubleToText(value));
        }

        public void Set(Constants.Param paramName, double value)
        {
            Set(paramName, Converter.DoubleToText(value));
        }

        public void Set(string paramName, double value)
        {
            Set(paramName, Converter.DoubleToText(value));
        }

        public void Set(Constants.Param paramName, long value)
        {
            Set(paramName, Converter.IntToText(value));
        }

        public void Set(string paramName, long value)
        {
            Set(paramName, Converter.IntToText(value));
        }

        public void Set(Constants.Param paramName, bool value)
        {
            Set(paramName, value ? 1 : 0);
        }

        public void Set(string paramName, bool value)
        {
            Set(paramName, value ? 1 : 0);
        }
        public void Set(FrameworkElement paramName, bool value)
        {
            Set(paramName.Name, value ? 1 : 0);
        }

        public void Set<T>(Constants.Param paramName, IEnumerable<T> list)
        {
            Set(ParamToStr(paramName), list);
        }

        public void Set<T>(string paramName, IEnumerable<T> list)
        {
            Set(paramName, string.Join(",", list.Select(l => l.ToString())));
        }

        private void SaveValues(Dictionary<string, string> values)
        {
            if (s_cacheSave.ContainsKey(_fileName))
            {
                s_cacheSave[_fileName] = values;
            }
            else
            {
                s_cacheSave.Add(_fileName, values);
            }
        }

        public void FlushToDrive()
        {
            if (!s_cacheSave.ContainsKey(_fileName))
            {
                return;
            }

            List<string> lines = new();

            var values = s_cacheSave[_fileName];
            foreach (var pair in values)
            {
                lines.Add(pair.Key + ":" + pair.Value);
            }

            File.WriteAllLines(_fileName, lines);
        }

        private Dictionary<string, string> GetLoaded()
        {
            if (s_cacheLoad.ContainsKey(_fileName))
            {
                return s_cacheLoad[_fileName];
            }

            Dictionary<string, string> result = new();

            if (!File.Exists(_fileName))
            {
                Clear();
            }

            var lines = File.ReadAllLines(_fileName);

            foreach (var line in lines)
            {
                if (!line.Contains(":"))
                {
                    continue;
                }

                var parts = line.Split(new[] { ':' });
                if (_fileName != "config.txt" && !parts[0].Contains("."))
                {
                    //continue;                        
                }

                if (parts.Length > 1)
                {
                    result[parts[0]] = string.Join(":", parts.Except(parts.Take(1)));
                }
            }

            s_cacheLoad.Add(_fileName, result);

            return result;
        }

        private Dictionary<string, string> GetSaved()
        {
            if (s_cacheSave.ContainsKey(_fileName))
            {
                return s_cacheSave[_fileName];
            }
            
            s_cacheSave[_fileName] = new();
            return s_cacheSave[_fileName];
        }

        public void Clear()
        {
            File.WriteAllLines(_fileName, Array.Empty<string>());

            s_cacheLoad.Clear();
            s_cacheSave.Clear();
        }

        private static string ParamToStr(Constants.Param param)
        {
            return param.ToString("G");
        }
    }
}