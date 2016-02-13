using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using System.Diagnostics;
using Shared;
using System.Reflection;

namespace PatchClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static class UIThread
        {
            public static Dispatcher Dispatcher { get; set; }
            public static void Run(Action a)
            {
                if (Dispatcher != null)
                {
                    Dispatcher.BeginInvoke(a);
                }
            }
        }

        private DispatcherTimer m_Timer = new DispatcherTimer();

        /// <summary>
        /// Delegate to allow thread-safe UI updates of the patch download progress info
        /// </summary>
        /// <param name="patchFileName">the filename that we're downloading</param>
        /// <param name="curDownload">how many bytes we currently have</param>
        /// <param name="totalDownload">how many bytes we're expecting, total, for this file</param>
        private delegate void UpdatePatchProgressDelegate(string patchFileName, long curDownload, long totalDownload);

        public MainWindow()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                String resourceName = "AssemblyLoadingAndReflection." +
                   new AssemblyName(args.Name).Name + ".dll";
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };

            InitializeComponent();
            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
            MouseDown += new MouseButtonEventHandler(MainWindow_MouseDown);
            cmdQuit.Click += new RoutedEventHandler(cmdQuit_Click);
            Patcher.PatchInfoAdded += new Patcher.PatchInfoAddedDelegate(Patcher_PatchInfoAdded);
        }

        void Patcher_PatchInfoAdded(string message)
        {
            UIThread.Run(() =>
            {
                txtStatus.Text = message;
            });
        }

        void cmdQuit_Click(object sender, RoutedEventArgs e)
        {            
            Close();
        }

        void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UIThread.Dispatcher = this.Dispatcher;
            
            Patcher.PatchDownloadProgress += new Patcher.PatchDownloadProgressDelegate(Patcher_OnPatchDownloadProgress);
            Patcher.PatchApplyProgress += new Patcher.PatchApplyProgressDelegate(Patcher_OnPatchApplyProgress);
            Patcher.PatchUnzipProgress += new Patcher.PatchUnzipProgressDelegate(Patcher_OnPatchUnzipProgress);
            Patcher.PatchArrived += new Patcher.PatchArrivedDelegate(Patcher_OnPatchArrived);
            Patcher.ClientIsCurrent += new EventHandler(Patcher_OnClientIsCurrent);
            Patcher.PatchNotesArrived += new Patcher.PatchNotesArrivedDelegate(Patcher_PatchNotesArrived);
            Patcher.Initialize();

            // Start patch process in one second.
            m_Timer.Interval = TimeSpan.FromSeconds(1);
            m_Timer.Tick += new EventHandler(StartPatch);
            m_Timer.Start();
        }

        private int m_DecompressSpinner = 0;
        void Patcher_OnPatchUnzipProgress(string patchFileName, bool running)
        {
            UIThread.Run(() =>
            {
                if (running)
                {
                    string[] spinner = new string[] { "|", "/", "-", "\\" };
                    m_DecompressSpinner++;
                    if (m_DecompressSpinner >= spinner.Length)
                    {
                        m_DecompressSpinner = 0;
                    }
                    txtStatus.Text = "Decompressing... " + spinner[m_DecompressSpinner];
                }
                else
                {
                    txtStatus.Text = "Decompressing... Done.";
                }
            });
        }

        void Patcher_OnPatchApplyProgress(string patchFileName, long curProcess, long totalDiffs)
        {
            UIThread.Run(() =>
            {
                string prog = string.Format("Applying patch: {0}/{1} differences", curProcess, totalDiffs);
                lblFilename.Text = prog;
                pb.Value = curProcess;
                pb.Maximum = totalDiffs;
                UpdatePatchApplyProgress(patchFileName, curProcess, totalDiffs);
            });
        }

        ///
        private void UpdatePatchApplyProgress(string patchFileName, long curProcess, long totalDiffs)
        {
            UIThread.Run(() =>
            {
                // Update progress bar
                pb.Maximum = (int)totalDiffs;
                pb.Value = (int)curProcess;

                if (pb.Value > 0 && pb.Value < pb.Maximum)
                {
                    pb.Visibility = lblFilename.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    pb.Visibility = lblFilename.Visibility = System.Windows.Visibility.Collapsed;
                }

                if (curProcess == totalDiffs)
                {
                    pb.Value = 0;
                }
            });

        }

        void Patcher_PatchNotesArrived(string notes)
        {
            UIThread.Run(() =>
            {
                if (txtNotes.Text.Length == 0)
                {
                    txtNotes.AppendText(notes);
                }
                else
                {
                    txtNotes.AppendText("\r\n\r\n" + notes);
                }
            });
        }

        /// <summary>
        /// Starts the patch process, via m_Timer.  the use of the timer tends to make the patch client startup sequence feel smoother on many systems.
         /// </summary>
        private void StartPatch(object sender, EventArgs e)
        {
            m_Timer.Stop();

            // see if we're doing a manual patch job.
            string[] args = Environment.GetCommandLineArgs();
            if (args != null && args.Length > 1)
            {
                string patchFile = args[1];
                if (!File.Exists(patchFile) || (!patchFile.EndsWith(".patch")))
                {
                    MessageBox.Show("The file " + patchFile + " does not seem to be a valid patch file.");
                    Application.Current.Shutdown();
                }

                // We have a manual patch file.  apply it.
                //Log.LogMsg("Applying patch file " + patchFile);
                Patcher.ApplyLocalPatch(patchFile);
                return;
            }

            Patcher.ConnectToPatchServer();
        }

        /// <summary>
        /// Event handler gets called by the patcher when the patch server tells us we're current. When this happens, we enable the "Play" button via this event handler.
        /// </summary>
        private void Patcher_OnClientIsCurrent(object sender, EventArgs e)
        {
            // Enable Play Button
            Patcher_PatchInfoAdded("Files up to date.");
            UIThread.Run(() =>
               {
                   txtStatus.Text = "Launching...";
                   Launch();
               });
        }

        private int rec = 0;
        /// <summary>
        /// Event handler code gets called when a patch has arrived.  This is for informational purposes only.  
        /// The patcher handles all necessary responses to this event internally.
        /// </summary>
        private void Patcher_OnPatchDownloadProgress(string patchFileName, long curDownload, long totalDownload)
        {
            rec++;
            //Log.LogMsg(string.Format(rec + " - " + "Patch progress for {0} at {1} / {2}", patchFileName, curDownload, totalDownload));
            UIThread.Run(() =>
                {
                    string len = string.Format("{0} MB", Util.ConvertBytesToMegabytes(totalDownload).ToString("0.00"));
                    lblFilename.Text = System.IO.Path.GetFileName(patchFileName) + "(" + len + ")";
                    pb.Value = curDownload;
                    UpdatePatchProgress(patchFileName, curDownload, totalDownload);
                });
        }

        /// <summary>
        /// Event handler code gets called when a patch has arrived.  This is for informational purposes only.  
        /// The patcher handles all necessary responses to this event internally.
        /// </summary>
        private void Patcher_OnPatchArrived(string patchFile, long fileLength)
        {
            UIThread.Run(() =>
            {
                string len = string.Format("{0} MB", Util.ConvertBytesToMegabytes(fileLength).ToString("0.00"));
                lblFilename.Text = System.IO.Path.GetFileName(patchFile) + "(" + len + ")";
                pb.Value = 100;
                UpdatePatchProgress(patchFile, fileLength, fileLength);
            });
        }

        /// <summary>
        /// UI update method, should be called via Invoke - patcher runs in a different thread than the UI.
        /// </summary>
        /// <param name="patchFileName">the filename we're downloading</param>
        /// <param name="curDownload">current number of bytes downloaded so far</param>
        /// <param name="totalDownload">total number of bytes expected for this download</param>
        private void UpdatePatchProgress(string patchFileName, long curDownload, long totalDownload)
        {
            UIThread.Run(() =>
          {
              if (pb.Value == 0 && curDownload > 0)
              {
                  // Send a text message to the log label
                  txtStatus.Text = "Receiving " + Util.ConvertBytesToMegabytes((ulong)totalDownload).ToString("N") + "MB patch " + System.IO.Path.GetFileName(patchFileName) + "...";
              }

              // Update progress bar
              pb.Maximum = (int)totalDownload;
              pb.Value = (int)curDownload;

              if (pb.Value > 0 && pb.Value < pb.Maximum)
              {
                  pb.Visibility = lblFilename.Visibility = System.Windows.Visibility.Visible;
              }
              else
              {
                  pb.Visibility = lblFilename.Visibility = System.Windows.Visibility.Collapsed;
              }

              if (curDownload == totalDownload)
              {
                  pb.Value = 0;
              }
          });
             
        }

        // Play button, shuts down the patcher and launches the application desginated in the App.Config file under "LaunchOnCurrent"
        void Launch()
        {
            string launch = ConfigHelper.GetStringConfig("LaunchOnCurrent");
            if (launch != "")
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.FileName = System.IO.Path.Combine(Environment.CurrentDirectory, launch);
                if (System.IO.File.Exists(p.StartInfo.FileName))
                {
                    try
                    {
                        p.Start();
                    }
                    catch (Exception er)
                    {
                        MessageBox.Show("Failed to launch " + p.StartInfo.FileName +". " + er.Message);
                    }
                }
            }
            Close();
            Application.Current.Shutdown();
        }

 
    }
}
