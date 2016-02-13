//#define UNITY

using System;
using System.Collections.Generic;
using System.Text;


#if SILVERLIGHT
using System.IO.IsolatedStorage;
using System.IO;
#endif

namespace Shared
{
    /// <summary>
    /// Helper class that reads standard App.config XML config files (or isolated storage application settings, in Silverlight).
    /// In the Unity client this is a simple memory store only and does not read from config files.
    /// </summary>
    public class ConfigHelper
    {
#if SILVERLIGHT

        public static bool SaveFile(string fileName, string data) 
        {
            try
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(fileName, FileMode.Create, isf))
                    {
                        using (StreamWriter sw = new StreamWriter(isfs))
                        {
                            sw.Write(data); sw.Close();
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }         
        
        public static string LoadFile(string fileName) 
        {
            try
            {
                string data = String.Empty;
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(fileName, FileMode.Open, isf))
                    {
                        using (StreamReader sr = new StreamReader(isfs))
                        {
                            string lineOfData = String.Empty;
                            while ((lineOfData = sr.ReadLine()) != null)
                                data += lineOfData;
                        }
                    }
                }
                return data;
            }
            catch
            {
                return "";
            }
        }

        public static TT Read<TT>(string name)
        {
            return Read<TT>(name, default(TT));
        }

        public static TT Read<TT>(string name, TT defaultValue)
        {
            System.IO.IsolatedStorage.IsolatedStorageSettings settings = System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings;
            TT value;
            if (settings == null || !settings.TryGetValue<TT>(name, out value))
                return defaultValue;
            return value;
        }

        public static void Write<TT>(string name, TT value)
        {
            System.IO.IsolatedStorage.IsolatedStorageSettings settings = System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings;
            if (settings == null)
                return;
            if (settings.Contains(name))
                settings[name] = value;
            else
                settings.Add(name, value);
            settings.Save();
        }


#else
        private static Dictionary<string, string> m_Configs = new Dictionary<string, string>();

        /// <summary>
        /// Should the config be read from a disk file or from memory?
        /// </summary>
        public static bool IsInMemoryOnly = false;
#endif

        public static int GetIntConfig(string key, int defaultVal)
        {
            try
            {
#if SILVERLIGHT
                int dat = Read<int>(key);
                return dat;
#else
                string dat = "";
                if (!IsInMemoryOnly)
                {
#if !UNITY
                    dat = System.Configuration.ConfigurationManager.AppSettings[key];
#else
                    m_Configs.TryGetValue(key, out dat);
#endif
                }
                else
                {
                    m_Configs.TryGetValue(key, out dat);
                }

                if (dat == null)
                {
                    return defaultVal;
                }

                return int.Parse(dat);
#endif
            }
            catch (Exception e)
            {
                return defaultVal;
            }

            return -1;
        }

        public static int GetIntConfig(string key)
        {
            return GetIntConfig(key, -1);
        }

        public static float GetFloatConfig(string key)
        {
            try
            {
#if SILVERLIGHT
                string dat = Read<string>(key);
#else
                string dat = "";
                if (!IsInMemoryOnly)
                {
#if !UNITY
                    dat = System.Configuration.ConfigurationManager.AppSettings[key];
#else
                    m_Configs.TryGetValue(key, out dat);
#endif                    
                }
                else
                {
                    m_Configs.TryGetValue(key, out dat);
                }

#endif
                if (dat == null)
                {
                    return -1f;
                }

                return float.Parse(dat);
            }
            catch
            {
            }

            return -1f;
        }

        public static float GetFloatConfig(string key, float defaultValue)
        {
            float val = GetFloatConfig(key);
            if (val == -1)
            {
                return defaultValue;
            }

            return val;
        }

        public static string GetStringConfig(string key, string defaultValue)
        {
            string val = GetStringConfig(key);
            if (val.Length < 1)
            {
                return defaultValue;
            }

            return val;
        }

        public static string GetStringConfig(string key)
        {
            try
            {
#if SILVERLIGHT
                string dat = Read<string>(key);
#else
                string dat = "";
                if (!IsInMemoryOnly)
                {
#if !UNITY
                    dat = System.Configuration.ConfigurationManager.AppSettings[key];
#else
                    m_Configs.TryGetValue(key, out dat);
#endif
                }
                else
                {
                    m_Configs.TryGetValue(key, out dat);
                }
#endif
                if (dat == null)
                {
                    return string.Empty;
                }
                return dat.Trim();
            }
            catch
            {
            }

            return string.Empty;
        }

#if SILVERLIGHT
        public static void SetConfig(string key, string value)
        {
            try
            {
                Write(key, value);
            }
            catch
            {
            }
        }

        public static void SetConfig(string key, int value)
        {
            try
            {
                Write(key, value);
            }
            catch
            {
            }

        }
#endif
#if !SILVERLIGHT
        public static void SetConfig(string key, string value)
        {
            if (IsInMemoryOnly)
            {
                m_Configs.Remove(key);
                m_Configs.Add(key, value);
            }
        }

        public static void SetConfig(string key, int value)
        {
            if (IsInMemoryOnly)
            {
                m_Configs.Remove(key);
                m_Configs.Add(key, value.ToString());
            }
            throw new NotImplementedException("ConfigHelper.SetConfig not implemented on this platform");
        }

        public static bool SaveFile(string p, string xml)
        {
            throw new NotImplementedException("ConfigHelper.SaveFile not implemented on this platform");
        }

        public static string LoadFile(string p)
        {
            throw new NotImplementedException("ConfigHelper.LoadFile not implemented on this platform");
        }
#endif
    }

}
