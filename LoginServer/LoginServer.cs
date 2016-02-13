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
using Microsoft.Win32;

namespace Shared
{
    public partial class LoginServerProc : ServiceBase
    {
        public LobbyLoginServer m_Server;

        public LoginServerProc()
        {
            InitializeComponent();            
        }

        public void Setup()
        {
            OnStart(null);
        }
         
        protected override void OnStart(string[] args)
        {
            Log1.Logger("Login").Info("Starting Login server");
            Thread t = new Thread(new ThreadStart(StartAsync));
            t.Start();
        }

        private void StartAsync()
        {
            m_Server = new LobbyLoginServer();
            m_Server.StartServer();
            Log1.Logger("Login").Info("Ready.");
        }

        protected override void OnStop()
        {            
            if (m_Server == null)
            {
                return;
            }

            Log1.Logger("Login").Info("Stopping Login server...");            
            m_Server.StopServer();
        }

        protected override void OnShutdown()
        {
            Log1.Logger("Login").Info("Machine shutting down.");
        }

    }
}
