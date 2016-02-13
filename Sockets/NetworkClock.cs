using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !SILVERLIGHT
using System.Timers;
#else
using System.Windows.Threading;
#endif

namespace Shared
{
    /// <summary>
    /// Allows a client connection to synchronize its clock with a server connection.
    /// </summary>
    public class NetworkClock
    {       
        
#if !SILVERLIGHT
        private Timer m_Timer;
#else
        private DispatcherTimer m_Timer;
#endif

        private int m_CurrentSyncStep = 0;        
        private struct Sample
        {
            public Sample(long sampleRequestLocalTime, long currentLocalTime, long remoteTime)
            {
                // estimate that round trip time / 2 is the amount of time it takes for the clock packet to get from the server to us
                long roundTripTime = currentLocalTime - sampleRequestLocalTime;
                long Latency = roundTripTime / 2;
                //Log.LogMsg("Clock sync packet had " + TimeSpan.FromTicks(Latency).TotalMilliseconds + "ms latency.");

                if (Latency < 1)
                {
                    Latency = 0;
                }

                // The difference between local time and server time 
                ClientServerDelta = remoteTime - currentLocalTime + Latency;
            }
            public long ClientServerDelta;
        }

        private long m_ClientServerTimeDelta = 0;
        /// <summary>
        /// How much difference the clock is on the server
        /// </summary>
        public long ClientServerTimeDelta
        {
            get { return m_ClientServerTimeDelta; }
            set { m_ClientServerTimeDelta = value; }
        }

        /// <summary>
        /// Current synchronized network time, in ticks
        /// </summary>
        public long UTCTimeTicks
        {
            get
            {
                try
                {
                    return DateTime.UtcNow.Ticks + ClientServerTimeDelta;
                }
                catch
                {
                    return DateTime.UtcNow.Ticks;
                }
            }
        }

        /// <summary>
        /// Current synchronized network time
        /// </summary>
        public DateTime UTCTime
        {
            get
            {
                try
                {
                    return new DateTime(DateTime.UtcNow.Ticks + ClientServerTimeDelta, DateTimeKind.Utc);
                }
                catch
                {
                    return DateTime.UtcNow;
                }
            }
        }
        
        private int m_NumSamplesForSync = 0;
        /// <summary>
        /// The number of time samples to request from the remote connection, each time sync
        /// </summary>
        public int NumSamplesForSync
        {
            get { return m_NumSamplesForSync; }
            set 
            { 
                m_NumSamplesForSync = value;
                CheckSyncEligibility();
            }
        }

        private int m_SyncTimeAllowed = -1;
        /// <summary>
        /// The amount of time, in ms, to span the sending of NumSamplesForSync sync request packets
        /// </summary>
        public int SyncTimeAllowed
        {
            get { return m_SyncTimeAllowed; }
            set 
            { 
                m_SyncTimeAllowed = value;
                CheckSyncEligibility();
            }
        }

        private int m_TimeBetweenSyncs = 0;
        /// <summary>
        /// The time, in ms, to wait between sync attempts
        /// </summary>
        public int TimeBetweenSyncs
        {
            get { return m_TimeBetweenSyncs; }
            set 
            {
                m_TimeBetweenSyncs = value;
                CheckSyncEligibility();
            }
        }
        
        private void SetTimer(int delay)
        {
            if (delay < 1)
            {
                CancelTimer();
                return;
            }

            if (m_Timer == null)
            {
#if !SILVERLIGHT
                m_Timer = new Timer(delay);
                m_Timer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
#else
                /*
                UIThread.Run(() =>
                {
                    m_Timer = new DispatcherTimer();
                    m_Timer.Tick += new EventHandler(OnTimer_Elapsed);
                    m_Timer.Start();
                });
                 * */
#endif
                
            }
            else
            {
                m_Timer.Stop();
#if SILVERLIGHT
                m_Timer.Interval = TimeSpan.FromMilliseconds(delay);
                m_Timer.Start();
#else
                m_Timer.Interval = delay;                             
#endif
            }
#if !SILVERLIGHT
            m_Timer.Start();
#endif
        }

        private void TimerElapsed()
        {
            m_Timer.Stop();
            RequestSyncSample();
        }

#if SILVERLIGHT
        void OnTimer_Elapsed(object sender, EventArgs e)
        {
            TimerElapsed();
        }
#else
        void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            TimerElapsed();
        }
#endif

        private void CancelTimer()
        {
            if (m_Timer == null)
            {
                return;
            }
            m_Timer.Stop();
        }

        /// <summary>
        /// Creates a network clock that syncs itself with the remote connection
        /// </summary>
        /// <param name="owner">the network connection that should be time sync'd</param>
        /// <param name="numSamplesForSync">The number of samples to request per sync period.  More samples can, in some cases, increase accuracy at the expense of additional network traffic and time it takes to perform the sync. 10 samples over a period of 1000ms is generally a good starting point.  Set to zero to prevent requesting ANY sync samples. A setting of zero is appropriate for all servers, as the client is responsible for syncing itself to the server.</param>
        /// <param name="syncTimeAllowed">The time, in miliseconds, to span the sending of @numSamplesForSync time sync requests. Given sufficient sampling points (@numSamplesForSync), longer @syncTimeAllowed can sometimes increase clock accuracy by spreading the sampling out over a longer period of time. 10 samples over a period of 1000ms is generally sufficient is generally a good starting point.</param>
        /// <param name="timeBetweenSyncs">The time, in miliseconds, between clock syncs. Once ever minute or few minutes might be a good starting point.  You can also adjust this parameter on the fly (NetworkClock.TimeBetweenTyncs).</param>
        public NetworkClock(INetworkConnection owner, int numSamplesForSync, int syncTimeAllowed, int timeBetweenSyncs)
        {
            Owner = owner;
            NumSamplesForSync = numSamplesForSync;
            SyncTimeAllowed = syncTimeAllowed;
            TimeBetweenSyncs = timeBetweenSyncs;           
        }

        private bool m_SyncEnabled = false;
        /// <summary>
        /// Set to true to enable synchronization with server.  Server side connections shouldn't ever need to set this to true, since the client is generally responsible for syncing itself to the server.
        /// </summary>
        public bool SyncEnabled
        {
            get { return m_SyncEnabled; }
            set 
            { 
                m_SyncEnabled = value;
                CheckSyncEligibility();
            }
        }

        private int GetTimeBetweenSyncSteps()
        {
            int timeBetweenSyncSteps = 0;
            if (NumSamplesForSync > 0)
            {
                if (SyncTimeAllowed > 0)
                {
                    if (NumSamplesForSync > 1)
                    {
                        timeBetweenSyncSteps = (int)Math.Ceiling((float)SyncTimeAllowed / (float)NumSamplesForSync);
                    }
                }
            }            

           return timeBetweenSyncSteps;
        }

        /// <summary>
        /// Checks to see if the time sync should currently be happening and then starts/stops the sync process as appropriate.
        /// </summary>
        private void CheckSyncEligibility()
        {
            if (!SyncEnabled)
            {
                return;
            }
            if (NumSamplesForSync > 0 && m_CurrentSyncStep == 0)
            {
                Log.LogMsg("Starting clock synchronization. Requesting [" + NumSamplesForSync + " samples] over the next [" + TimeSpan.FromMilliseconds(SyncTimeAllowed).TotalSeconds + " seconds].");
                RequestSyncSample();
            }                        
        }

        /// <summary>
        /// The network connection which owns this clock
        /// </summary>
        public INetworkConnection Owner { get; private set; }
        
        private int SyncStepInProgress = 0;

        private void RequestSyncSample()
        {            
            // got the lock
            if (Owner.IsAlive)
            {
                //Log.LogMsg("Requesting clock sample from server.");

                m_CurrentSyncStep++;
                PacketClockSync msg = Owner.CreatePacket((int)PacketType.ClockSync, 0, false, false) as PacketClockSync;
                msg.StartTime = DateTime.UtcNow.Ticks;
                Owner.Send(msg);

                if (m_CurrentSyncStep >= NumSamplesForSync)
                {
                    // end of sampling sequence.  evaluate results and restart sequence, if appropriate
                    m_CurrentSyncStep = 0;
                    Recalculate();
                    if (TimeBetweenSyncs > 0)
                    {
                        SetTimer(TimeBetweenSyncs);
                    }
                }
                else
                {
                    // more samples to be taken.  determine when and kick it off.
                    int nextStepDelay = GetTimeBetweenSyncSteps();
                    if (nextStepDelay > 0)
                    {
                        SetTimer(nextStepDelay);
                    }
                    else
                    {
                        RequestSyncSample();
                    }
                }
            }
        }

        private List<Sample> m_Samples = new List<Sample>();
        public void AddSample(long sampleRequestLocalTime, long currentLocalTime, long remoteTime)
        {            
            Sample s = new Sample(sampleRequestLocalTime, currentLocalTime, remoteTime);
            lock (m_Samples)
            {
                //Log.LogMsg("Adding new clock sample: " + TimeSpan.FromTicks(s.ClientServerDelta).TotalMilliseconds + "ms difference.");
                m_Samples.Add(s);       
                if(m_Samples.Count == 1 && ClientServerTimeDelta == 0)
                {
                    // first clock sample.  use value to get into the ballbpark.
                    ClientServerTimeDelta = s.ClientServerDelta;
                }
            }
        }

        protected virtual void Recalculate()
        {
            lock (m_Samples)
            {
                // Get the median difference
                long median = GetMedianTimeDifference();
                long bounds = (long)((float)median * 1.5f);
                int i = 0;
                while (i < m_Samples.Count && m_Samples.Count > 1)
                {
                    if (Math.Abs(m_Samples[i].ClientServerDelta) >= Math.Abs(bounds))
                    {
                        // greater than 150% of the median. Probably a failed/retransmit that's not charactertistic of average latency. discard.
                        m_Samples.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                if (m_Samples.Count > 0)
                {
                    ClientServerTimeDelta = (long)m_Samples.Average(n => n.ClientServerDelta);
                }
                Log.LogMsg("New synchronized server clock offset is " + TimeSpan.FromTicks(ClientServerTimeDelta).TotalMilliseconds + "ms .");
                m_Samples.Clear();
            }
        }

        private long GetMedianTimeDifference()
        {
            if (m_Samples.Count < 1)
            {
                return 0;
            }

            int numberCount = m_Samples.Count;
            int halfIndex = numberCount / 2;
            var sortedNumbers = m_Samples.OrderBy(n => n.ClientServerDelta);
            double median;
            if ((numberCount % 2) == 0)
            {
                median = ((sortedNumbers.ElementAt(halfIndex).ClientServerDelta + sortedNumbers.ElementAt((halfIndex - 1)).ClientServerDelta) / 2);
            }
            else
            {
                median = sortedNumbers.ElementAt(halfIndex).ClientServerDelta;
            }

            return (long)median;
        }


    }
}
