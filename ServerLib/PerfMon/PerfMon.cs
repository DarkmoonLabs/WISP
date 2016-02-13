#region Using

using System;
using System.Diagnostics;
using Shared;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Configuration;

#endregion

namespace Shared
{

    /// <summary>
    /// A helper class to create the specified performance counters.
    /// </summary>
    public class PerfMon
    {
        [DllImport("Kernel32.dll")]
        public static extern void QueryPerformanceCounter(ref long ticks);
        public static Dictionary<string, PerfHistory> History { get; set; }
        
        private static string m_ProcessName = null;
        public static string ProcessName 
        {
            get
            {
                if (m_ProcessName == null)
                {
                    m_ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                }

                return m_ProcessName;
            }
        }
        private static object m_SyncRoot = new object();

        static PerfMon()
        {
            History = new Dictionary<string, PerfHistory>();
            _Category = "Wisp";
            UninstallCustomCounters();            
        }

        public static void LoadFromConfig()
        {
            if (m_Sampling)
            {
                return;
            }

            try
            {
                PerfConfig section = (PerfConfig)ConfigurationManager.GetSection("PerformanceCounters");
                if (section != null)
                {
                    foreach (PerfConfigElement cmd in section.PerfItems)
                    {
                        try
                        {
                            if (cmd.IsCustom.Trim().ToLower() == "true")
                            {
                                TrackCustomCounter(cmd.CounterName, cmd.Help, (PerformanceCounterType)Enum.Parse(typeof(PerformanceCounterType), cmd.PerformanceCounterType), TimeSpan.FromSeconds(int.Parse(cmd.SampleIntervalSecs)), int.Parse(cmd.MaxSamplesInHistory), cmd.Divisor);
                            }
                            else
                            {
                                //PerfMon.TrackSystemCounter("% Processor Time", "Processor", "_Total");
                                //for (int i = 0; i < Environment.ProcessorCount; i++)
                                //{
                                //    PerfMon.TrackSystemCounter("% Processor Time", "Processor", i.ToString());
                                //}                                                                
                                if (cmd.CounterName == "% Processor Time" && cmd.CounterGroup == "Processor" && cmd.InstanceName == "AllCores")
                                {
                                    // add one for hte total
                                    TrackSystemCounter(cmd.CounterName, cmd.CounterGroup, "_Total", TimeSpan.FromSeconds(int.Parse(cmd.SampleIntervalSecs)), cmd.Help, int.Parse(cmd.MaxSamplesInHistory), cmd.Divisor);

                                    // add one for all the individual cores as well
                                    for (int i = 0; i < Environment.ProcessorCount; i++)
                                    {
                                        TrackSystemCounter(cmd.CounterName, cmd.CounterGroup, i.ToString(), TimeSpan.FromSeconds(int.Parse(cmd.SampleIntervalSecs)), string.Format("CPU Core {0} % Usage", i + 1), int.Parse(cmd.MaxSamplesInHistory), cmd.Divisor);
                                    }
                                }
                                else
                                {
                                    TrackSystemCounter(cmd.CounterName, cmd.CounterGroup, cmd.InstanceName, TimeSpan.FromSeconds(int.Parse(cmd.SampleIntervalSecs)), cmd.Help, int.Parse(cmd.MaxSamplesInHistory), cmd.Divisor);
                                }
                            }
                            Log1.Logger("Performance").Info("Loaded data for available Performance Counter [" + cmd.CounterGroup + ": " + cmd.CounterName + "].");
                        }
                        catch (Exception e)
                        {
                            Log1.Logger("Performance").Error("Unable to read Performance Counter info from config file. [" + cmd.CounterGroup + ": " + cmd.CounterName + "]. " + e.Message, e);
                        }
                    }
                }
            }
            catch (Exception fatl)
            {
                Log1.Logger("Performance").Error("Unable to load Server Performance Counter data from config file. " + fatl.Message, fatl);
            }
        }

        private static bool m_Sampling = false;
        /// <summary>
        /// Starts the sample timer and starts collecting samples
        /// </summary>
        public static void StartSamplingCounters()
        {
            if (History.Count < 1)
            {
                Log1.Logger("Performance").Error("Unable to start sampling performance counters: No performance counters are currently active.");
                return;
            }
            m_Sampling = true;
            SetTimer(1000);
        }

        /// <summary>
        /// Stops the sample timer and starts collecting samples
        /// </summary>
        public static void StopSamplingCounters()
        {
            CancelTimer();
            m_Sampling = false;
        }

        #region Timer Util

        private static Timer m_Timer;

        private static void SetTimer(int ms)
        {
            if (ms < 1)
            {
                CancelTimer();
                return;
            }

            if (m_Timer == null)
            {
                m_Timer = new Timer(new TimerCallback(OnTimerElapsed), null, ms, Timeout.Infinite);
            }
            else
            {
                CancelTimer();
                m_Timer.Change(ms, Timeout.Infinite);
            }
        }

        private static void CancelTimer()
        {
            if (m_Timer == null)
            {
                return;
            }
            m_Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }                

        #endregion

        public static void UntrackSystemCounter(string counterName, string groupName, string instanceName)
        {
            if (m_Sampling)
            {
                return;
            }
            PerformanceCounter pc = null;
            string key = counterName + "|" + groupName + "|" + instanceName;                
            lock (m_SyncRoot)
            {
                PerfHistory h = null;
                if (!History.TryGetValue(key, out h))
                {
                    return;
                }
                pc = h.Counter;
                History.Remove(key);
            }

            if (pc != null)
            {
                pc.Dispose();
            }
        }

        public static PerfHistory TrackSystemCounter(string counterName, string groupName, string instanceName, TimeSpan sampleInterval, string helpText, int maxSamplesToCache, string divisor)
        {
            if (m_Sampling)
            {
                return null;
            }
            if (instanceName.ToLower() == "processname")
            {
                instanceName = ProcessName;
            }

            string key = counterName + "|" + groupName + "|" + instanceName;
            PerfHistory h = null;
            try
            {                
                PerformanceCounter pc = null;
                lock (m_SyncRoot)
                {
                    if (History.ContainsKey(key))
                    {
                        return null;
                    }

                   
                    if (instanceName.Length > 0)
                    {
                        h = GetHistory(counterName, groupName, instanceName);
                    }
                    else
                    {
                        h = GetHistory(counterName, groupName, "");
                    }
                    
                    if (h != null)
                    {
                        try
                        {
                            h.Divisor = float.Parse(divisor);
                        }
                        catch (FormatException exc)
                        {
                            Log1.Logger("Performance").Error("System Counter " + h.Key + " has an invalid [Divisor] - must be in float format!");
                            h.Divisor = 1;
                        }
                        h.Counter = pc;
                        h.SampleInterval = sampleInterval;
                        h.HelpText = helpText;
                        h.MaxHistorySamplesToKeep = maxSamplesToCache;
                    }
                }

                if (pc != null)
                {
                    pc.NextValue();
                }

                Log1.Logger("Performance").Debug("Tracking system performance counter [" + key + "]");                
            }
            catch (Exception e)
            {
                Log1.Logger("Performance").Error("Failed to track System Counter [" + key + "]", e);
            }
            return h;
        }

        protected static void OnTimerElapsed(object state)
        {
            DateTime now = DateTime.UtcNow;
            lock (m_SyncRoot)
            {
                bool anyActiveCounters = false;
                Dictionary<string, PerfHistory>.Enumerator enu = History.GetEnumerator();
                while (enu.MoveNext())
                {
                    PerfHistory h = enu.Current.Value;
                    if (!h.IsEnabled)
                    {
                        continue;
                    }
                    anyActiveCounters = true;
                    if (now - h.LastSample >= h.SampleInterval)
                    {
                        h.AddSample((float)Math.Round(h.Counter.NextValue() / h.Divisor, 2));
                        //Log1.Logger("Performance").Debug("Sampled performance counter [" + h.Category + "|" + h.CounterName + "|" + h.InstanceName + "] with a value of [" + h.History[h.History.Count - 1].Value + "].");
                    }
                }

                if (!anyActiveCounters)
                {
                    Log1.Logger("Performance").Error("Unable to start sampling performance counters: No performance counters are currently active.");
                    StopSamplingCounters();
                }
            }

            StartSamplingCounters();
        }

        private static PerfHistory GetHistory(string counterName, string groupName, string instanceName)
        {
            PerfHistory rslt;
            string counter = PerfHistory.GetKey(counterName, groupName, instanceName);
            if (!History.TryGetValue(counter, out rslt))
            {
                rslt = new PerfHistory();
                rslt.CounterName = counterName;
                rslt.Category = groupName;
                rslt.InstanceName = instanceName;
                History.Add(counter, rslt);
            }

            return rslt;
        }

        /// <summary>
        /// Creates an instance of the class.
        /// </summary>
        private PerfMon()
        {
            _Category = "Wisp";
        }

        #region Custom Counters

        private static CounterCreationDataCollection _Counters = new CounterCreationDataCollection();
        private static string _Category = string.Empty;

        /// <summary>
        /// Creates the performance counters
        /// </summary>
        public static void InstallCounters()
        {
            if (m_Sampling)
            {
                return;
            }

            try
            {                
                if (!PerformanceCounterCategory.Exists(_Category))
                {
                    PerformanceCounterCategory.Create(_Category, _Category, PerformanceCounterCategoryType.Unknown, _Counters);
                }

                int count = 0;
                lock (m_SyncRoot)
                {
                    
                    foreach (PerfHistory ph in History.Values)
                    {
                        if (!ph.IsEnabled)
                        {
                            continue;
                        }

                        if (ph.InstanceName.Trim() == "")
                        {
                            ph.Counter = new PerformanceCounter(ph.IsCustom ? "Wisp" : ph.Category, ph.CounterName, !ph.IsCustom);
                        }
                        else
                        {
                            ph.Counter = new PerformanceCounter(ph.IsCustom ? "Wisp" : ph.Category, ph.CounterName, ph.InstanceName, !ph.IsCustom);
                        }
                        count++;
                    }
                }
                Log1.Logger("Performance").Info("Installed " + count + " performance counters.");
            }
            catch (Exception e)
            {
                Log1.Logger("Performance").Error("Failed to install performance counters.", e);
            }
        }

        /// <summary>
        /// Deletes the performance counters
        /// </summary>
        public static void UninstallCustomCounters()
        {
            if (m_Sampling)
            {
                return;
            }

            try
            {
                if (!PerformanceCounterCategory.Exists(_Category))
                {
                    return;
                }

                PerformanceCounterCategory.Delete(_Category);
                lock (m_SyncRoot)
                {
                    List<string> delete = new List<string>();
                    Dictionary<string, PerfHistory>.Enumerator enu = History.GetEnumerator();
                    while (enu.MoveNext())
                    {
                        if (enu.Current.Value.Counter.CategoryName == "Wisp")
                        {
                            delete.Add(enu.Current.Key);
                        }
                    }

                    foreach (string key in delete)
                    {
                        History.Remove(key);
                    }
                }
                Log1.Logger("Performance").Info("Uninstalled custom performance counters.");
            }
            catch (Exception e)
            {
                Log1.Logger("Performance").Error("Failed to uninstall custom performance counters.", e);
            }
        }

        /// <summary>
        /// Add a custom performance counter.  Counter won't be installed until InstallCounters() is called.
        /// </summary>
        public static void TrackCustomCounter(string name, string helpText, PerformanceCounterType type, TimeSpan sampleInterval, int maxSamplesToCache, string divisor)
        {
            if (m_Sampling)
            {
                return;
            }

            CounterCreationData ccd = new CounterCreationData();
            ccd.CounterName = name;
            ccd.CounterHelp = helpText;
            ccd.CounterType = type;
            PerfHistory h = GetHistory(name, "Wisp", "");
            try
            {
                h.Divisor = float.Parse(divisor);
            }
            catch (FormatException exc)
            {
                Log1.Logger("Performance").Error("Custom Counter " + h.Key + " has an invalid [Divisor] - must be in float format!");
                h.Divisor = 1;
            }

            h.SampleInterval = sampleInterval;
            h.HelpText = helpText;
            h.MaxHistorySamplesToKeep = maxSamplesToCache;
            h.IsCustom = true;
            _Counters.Add(ccd);
            Log1.Logger("Performance").Debug("Tracking custom performance counter [" + name +"|Wisp|" + "]"); 
        }

        public static void IncrementCustomCounter(string counter, long amount)
        {
            if (!m_Sampling)
            {
                return;
            }

            PerfHistory ph = null;
            lock (m_SyncRoot)
            {
                if (!History.TryGetValue(PerfHistory.GetKey(counter, "Wisp", ""), out ph))
                {
                    Log1.Logger("Performance").Debug("Tried incremented counter [" + counter + "] but that counter doesn't exist.");
                    return;
                }
                if (ph.Counter != null)
                {
                    ph.Counter.IncrementBy(amount);
                }
            }
        }

        public static void SetCustomCounter(string counter, long rawValue)
        {
            if (!m_Sampling)
            {
                return;
            }

            string key = PerfHistory.GetKey(counter, "Wisp", "");
            PerfHistory ph = null;
            lock (m_SyncRoot)
            {
                if (!History.TryGetValue(key, out ph))
                {
                    Log1.Logger("Performance").Debug("Tried set raw value for counter [" + counter + "] but that counter doesn't exist.");
                    return;
                }

                ph.Counter.RawValue = rawValue;
            }
        }

        #endregion


        public static bool IsSampling { get { return m_Sampling; } }
    }
}
