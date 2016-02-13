using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Shared;
using System.Threading;
using Zeus;

namespace ZeusService
{
    public partial class Zeus : ServiceBase
    {
        public WispServerProcess m_Server;

        public Zeus()
        {
            InitializeComponent();            
        }

        public void Setup()
        {
            OnStart(null);
        }
         
        protected override void OnStart(string[] args)
        {
            Log1.Logger("Zeus.Service").Info("Starting Zeus server");
            Thread t = new Thread(new ThreadStart(StartAsync));
            t.Start();
        }

        private void StartAsync()
        {
            m_Server = new WispServerProcess();
            m_Server.StartServer();
            Log1.Logger("Zeus.Service").Info("Ready.");
        }

        protected override void OnStop()
        {            
            if (m_Server == null)
            {
                return;
            }

            Log1.Logger("Zeus.Service").Info("Stopping Zeus server...");            
            m_Server.StopServer();
        }

        protected override void OnShutdown()
        {
            Log1.Logger("Zeus.Service").Info("Machine shutting down.");
        }


    }
}
