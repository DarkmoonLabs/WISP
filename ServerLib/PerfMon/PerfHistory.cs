using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Shared
{
    public class PerfHistory :
 ISerializableWispObject, IComparable

    {
        public TimeSpan SampleInterval { get; set; }
        public DateTime LastSample
        {
            get
            {
                if (History.Count == 0)
                {
                    return DateTime.MinValue;
                }

                return History[History.Count - 1].Timestamp;
            }
        }

        public PerfHistory()
        {
#if !IS_CLIENT
            Divisor = 1;
#endif
        }

#if !IS_CLIENT
        public PerfHistory(PerfHistory h) : base()
        {
            IsEnabled = h.IsEnabled;
            Category = h.Category;
            CounterName = h.CounterName;
            InstanceName = h.InstanceName;
            HelpText = h.HelpText;
            SampleInterval = h.SampleInterval;
        }

        public PerformanceCounterType CounterType { get; set; }
        public PerformanceCounter Counter { get; set; }
        public bool IsCustom { get; set; }
        public int MaxHistorySamplesToKeep { get; set; }
        public float Divisor { get; set; }
#endif
        public bool IsEnabled { get; set; }
        public string Category { get; set; }
        public string CounterName { get; set; }
        public string InstanceName { get; set; }
        public string HelpText { get; set; }

        public string Key
        {
            get
            {
                return GetKey(CounterName, Category, InstanceName);
            }
        }

        public static string GetKey(string counterName, string catgeory, string instance)
        {
            return counterName + "|" + catgeory + "|" + instance;
        }

#if IS_CLIENT

        private System.Collections.ObjectModel.ObservableCollection<HistoryItem> m_History = new System.Collections.ObjectModel.ObservableCollection<HistoryItem>();
        public System.Collections.ObjectModel.ObservableCollection<HistoryItem> History
        {
            get { return m_History; }
            set { m_History = value; }
        }
        
#else
        public List<HistoryItem> History = new List<HistoryItem>();
#endif
        public void Serialize(ref byte[] buffer, Pointer p)
        {
            BitPacker.AddBool(ref buffer, p, IsEnabled);
            BitPacker.AddLong(ref buffer, p, SampleInterval.Ticks);
            BitPacker.AddString(ref buffer, p, Category);
            BitPacker.AddString(ref buffer, p, CounterName);
            BitPacker.AddString(ref buffer, p, InstanceName);
            BitPacker.AddString(ref buffer, p, HelpText);
            BitPacker.AddInt(ref buffer, p, History.Count);
            for (int i = 0; i < History.Count; i++)
            {
                BitPacker.AddSingle(ref buffer, p, History[i].Value);
                BitPacker.AddLong(ref buffer, p, History[i].Timestamp.Ticks);
            }
        }

        public void Deserialize(byte[] data, Pointer p)
        {
            IsEnabled = BitPacker.GetBool(data, p);
            SampleInterval = TimeSpan.FromTicks(BitPacker.GetLong(data, p));
            Category = BitPacker.GetString(data, p);
            CounterName = BitPacker.GetString(data, p);
            InstanceName = BitPacker.GetString(data, p);
            HelpText = BitPacker.GetString(data, p);
            int numHistory = BitPacker.GetInt(data, p);
            for (int i = 0; i < numHistory; i++)
            {
                HistoryItem hi = new HistoryItem();
                hi.Value = BitPacker.GetSingle(data, p);
                hi.Timestamp = new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc);
                History.Add(hi);
            }
        }

#if !IS_CLIENT
        public void AddSample(float value)
        {
            HistoryItem h = new HistoryItem();
            h.Value = value;
            h.Timestamp = DateTime.UtcNow;
            History.Add(h);

            if (History.Count > MaxHistorySamplesToKeep)
            {
                History.RemoveAt(0);
            }
        }
#endif

        public struct HistoryItem
        {
            public float Value { get; set; }
            public DateTime Timestamp { get; set; }

            public string CategoryDesc
            {
                get
                {
                    return Timestamp.ToShortTimeString();
                }
            }
        }

        private static uint m_TypeHash = 0;
        public uint TypeHash
        {
            get
            {
                if (m_TypeHash == 0)
                {
                    m_TypeHash = Factory.GetTypeHash(this.GetType());
                }

                return m_TypeHash;
            }
        }

        public int CompareTo(object obj)
        {
            return this.Key.CompareTo(((PerfHistory)obj).Key);
        }
    }
}
