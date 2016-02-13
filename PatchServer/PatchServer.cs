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

namespace PatchServer
{
    public partial class PatchServer : ServiceBase
    {
        public PatchServerProcess m_Server;

        public PatchServer()
        {
            InitializeComponent();            
        }

        public void Setup()
        {
            OnStart(null);
        }
         
        protected override void OnStart(string[] args)
        {
            Log1.Logger("Patcher").Info("Starting Patch server");
            Thread t = new Thread(new ThreadStart(StartAsync));
            t.Start();
        }

        private void StartAsync()
        {
            m_Server = new PatchServerProcess();
            m_Server.StartServer();
            Log1.Logger("Patcher").Info("Ready.");
        }

        protected override void OnStop()
        {            
            if (m_Server == null)
            {
                return;
            }

            Log1.Logger("Patcher").Info("Stopping Patch server...");            
            m_Server.StopServer();
        }

        protected override void OnShutdown()
        {
            Log1.Logger("Patcher").Info("Machine shutting down.");
        }

    }
}
