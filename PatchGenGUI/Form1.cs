using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace PatchGenGUI
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private void cmdBrowseFrom_Click(object sender, EventArgs e)
        {
            dlgBrowse.ShowDialog();
            if (dlgBrowse.SelectedPath != null && dlgBrowse.SelectedPath.Length > 0)
            {
                txtFromDir.Text = dlgBrowse.SelectedPath;
            }
            CheckVerDirectories();
        }

        private void cmdBrowseTo_Click(object sender, EventArgs e)
        {
            dlgBrowse.ShowDialog();
            if (dlgBrowse.SelectedPath != null && dlgBrowse.SelectedPath.Length > 0)
            {
                txtToDir.Text = dlgBrowse.SelectedPath;
            }
            CheckVerDirectories();
        }

        private void cmdBrowseOutput_Click(object sender, EventArgs e)
        {
            dlgBrowse.ShowDialog();
            if (dlgBrowse.SelectedPath != null && dlgBrowse.SelectedPath.Length > 0)
            {
               txtOutputDir.Text = dlgBrowse.SelectedPath;
            }
            CheckOutputDir();
        }

        private void cmdGenPatch_Click(object sender, EventArgs e)
        {
            if (!CheckOutputDir() || !CheckVerDirectories() || !CheckVerError())
            {
                MessageBox.Show("Can't generate patch. Resolve errors first.");
                return;
            }

            if (!File.Exists("WispPatchGen.exe"))
            {
                MessageBox.Show("Couldn't find 'WispPatchGen.exe' in " + Environment.CurrentDirectory + ".\r\nCan't generate patch.");
                return;
            }

            if (!File.Exists("xdelta3.exe"))
            {
                MessageBox.Show("Couldn't find support file 'xdelta2.exe' in " + Environment.CurrentDirectory + ".\r\nCan't generate patch.");
                return;
            }

            Process process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.RedirectStandardError = false;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.FileName = Path.Combine(Environment.CurrentDirectory, "WispPatchGen.exe");
            string fromV = numFromVersion.Value.ToString("N2");
            string toV = numToVersion.Value.ToString("N2");
            string patchFile = Path.Combine(txtOutputDir.Text, "patch_" + fromV + "_" + toV + ".patch");
            
            if (File.Exists(patchFile))
            {
                MessageBox.Show("Patch already appears to exist:\r\n" + patchFile);
                return;
            }

            string from = txtFromDir.Text.TrimEnd(char.Parse("\\"));
            //if (!from.EndsWith("\\"))
            //{
            //    from += "\\";
            //}

            string to = txtToDir.Text.TrimEnd(char.Parse("\\"));
            //if (!to.EndsWith("\\"))
            //{
            //    to += "\\";
            //}

            string fromD = "\"" + from +"\"";
            string toD = "\"" + to + "\"";
            string outD = "\"" + patchFile + "\"";
            string patchNotes = "\"" + txtPatchNotes.Text.Replace("\"", "'") + "\"";
            fromV = "\"" + fromV + "\"";
            toV = "\"" + toV + "\"";

            process.StartInfo.Arguments = fromV + " " + toV + " " + fromD + " " + toD + " " + outD + " " + patchNotes;
            
            process.Start();

            txtVersionsEntry.Text = numToVersion.Value.ToString("N2") + " " + "patch_" + numFromVersion.Value.ToString("N2") + "_" + numToVersion.Value.ToString("N2") + ".patch";
        }

        private bool CheckVerError()
        {
            bool hasErr = false;
            if (numFromVersion.Value >= numToVersion.Value)
            {
                errProv.SetError(numFromVersion, "From version number must be less than 'To' version number.");
                errProv.SetError(numToVersion, "From version number must be less than 'To' version number.");
                hasErr = true;
            }
            else
            {
                errProv.SetError(numFromVersion, "");
                errProv.SetError(numToVersion, "");
            }

            return !hasErr;
        }

        private bool CheckVerDirectories()
        {
            bool hasErr = false;
            if (!Directory.Exists(txtFromDir.Text))
            {
                errProv.SetError(cmdBrowseFrom, "Directory does not exist");
                hasErr = true;
            }
            else
            {
                errProv.SetError(cmdBrowseFrom, "");
            }

            if (!Directory.Exists(txtToDir.Text))
            {
                errProv.SetError(cmdBrowseTo, "Directory does not exist");
                hasErr = true;
            }
            else
            {
                errProv.SetError(cmdBrowseTo, "");
            }

            if (hasErr)
            {
                return !hasErr;
            }

            if (Path.GetFullPath(txtFromDir.Text) == Path.GetFullPath(txtToDir.Text))
            {
                errProv.SetError(cmdBrowseTo, "Directory can't be the same as 'previous version' directory.");
                errProv.SetError(cmdBrowseFrom, "Directory can't be the same as 'new version' directory");
                hasErr = true;
            }
            else
            {
                errProv.SetError(cmdBrowseTo, "");
                errProv.SetError(cmdBrowseFrom, "");
            }

            return !hasErr;
        }

        private bool CheckOutputDir()
        {
            if (!Directory.Exists(txtOutputDir.Text))
            {
                errProv.SetError(cmdBrowseOutput, "Output directory does not exist.");
                return false;
            }
            else
            {
                errProv.SetError(cmdBrowseOutput, "");
                return true;
            }
        }

        private void numToVersion_ValueChanged(object sender, EventArgs e)
        {
            CheckVerError();
        }

        private void numFromVersion_ValueChanged(object sender, EventArgs e)
        {
            CheckVerError();
        }


    }
}
