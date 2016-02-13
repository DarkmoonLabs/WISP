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

namespace ZeusAccountService
{
    public partial class WispService : ServiceBase
    {
        public WispServerProcess m_Server;

        public WispService()
        {
            InitializeComponent();
        }

        public void Setup()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            // Initialize the server in a worker thread, or the Windows Service system may abort the 
            // application because it's taking too long to "start up"
            Log1.Logger("Server").Info("Starting server. Please wait...");
            Thread t = new Thread(new ThreadStart(StartAsync));
            t.Start();
        }

        private void StartAsync()
        {
            m_Server = new WispServerProcess();
            m_Server.StartServer();
            Log1.Logger("Server").Info("Ready.");
        }

        protected override void OnStop()
        {
            if (m_Server == null)
            {
                return;
            }

            Log1.Logger("Server").Info("Stopping server...");
            m_Server.StopServer();
        }

        protected override void OnShutdown()
        {
            Log1.Logger("Server").Info("Shutting down.");
        }

    }
}
