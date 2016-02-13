namespace PatchGenGUI
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.numFromVersion = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cmdBrowseFrom = new System.Windows.Forms.Button();
            this.txtFromDir = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cmdBrowseTo = new System.Windows.Forms.Button();
            this.txtToDir = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.numToVersion = new System.Windows.Forms.NumericUpDown();
            this.dlgBrowse = new System.Windows.Forms.FolderBrowserDialog();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.txtPatchNotes = new System.Windows.Forms.TextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.cmdBrowseOutput = new System.Windows.Forms.Button();
            this.txtOutputDir = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cmdGenPatch = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.txtVersionsEntry = new System.Windows.Forms.TextBox();
            this.errProv = new System.Windows.Forms.ErrorProvider(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.numFromVersion)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numToVersion)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errProv)).BeginInit();
            this.SuspendLayout();
            // 
            // numFromVersion
            // 
            this.numFromVersion.DecimalPlaces = 2;
            this.numFromVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numFromVersion.ForeColor = System.Drawing.Color.Black;
            this.numFromVersion.Location = new System.Drawing.Point(112, 25);
            this.numFromVersion.Name = "numFromVersion";
            this.numFromVersion.Size = new System.Drawing.Size(65, 20);
            this.numFromVersion.TabIndex = 0;
            this.numFromVersion.ValueChanged += new System.EventHandler(this.numFromVersion_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(21, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Version Number:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cmdBrowseFrom);
            this.groupBox1.Controls.Add(this.txtFromDir);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.numFromVersion);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.ForeColor = System.Drawing.Color.Gray;
            this.groupBox1.Location = new System.Drawing.Point(12, 135);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(427, 86);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Previous (old) Version:";
            // 
            // cmdBrowseFrom
            // 
            this.cmdBrowseFrom.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmdBrowseFrom.ForeColor = System.Drawing.Color.Black;
            this.cmdBrowseFrom.Location = new System.Drawing.Point(334, 50);
            this.cmdBrowseFrom.Name = "cmdBrowseFrom";
            this.cmdBrowseFrom.Size = new System.Drawing.Size(75, 22);
            this.cmdBrowseFrom.TabIndex = 4;
            this.cmdBrowseFrom.Text = "Brwose";
            this.cmdBrowseFrom.UseVisualStyleBackColor = true;
            this.cmdBrowseFrom.Click += new System.EventHandler(this.cmdBrowseFrom_Click);
            // 
            // txtFromDir
            // 
            this.txtFromDir.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtFromDir.Location = new System.Drawing.Point(112, 51);
            this.txtFromDir.Name = "txtFromDir";
            this.txtFromDir.Size = new System.Drawing.Size(216, 20);
            this.txtFromDir.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Black;
            this.label3.Location = new System.Drawing.Point(54, 54);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Directory:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(1, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(440, 117);
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.cmdBrowseTo);
            this.groupBox2.Controls.Add(this.txtToDir);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.numToVersion);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.ForeColor = System.Drawing.Color.Gray;
            this.groupBox2.Location = new System.Drawing.Point(12, 227);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(427, 86);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Current (new) Version:";
            // 
            // cmdBrowseTo
            // 
            this.cmdBrowseTo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmdBrowseTo.ForeColor = System.Drawing.Color.Black;
            this.cmdBrowseTo.Location = new System.Drawing.Point(334, 50);
            this.cmdBrowseTo.Name = "cmdBrowseTo";
            this.cmdBrowseTo.Size = new System.Drawing.Size(75, 22);
            this.cmdBrowseTo.TabIndex = 4;
            this.cmdBrowseTo.Text = "Brwose";
            this.cmdBrowseTo.UseVisualStyleBackColor = true;
            this.cmdBrowseTo.Click += new System.EventHandler(this.cmdBrowseTo_Click);
            // 
            // txtToDir
            // 
            this.txtToDir.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtToDir.Location = new System.Drawing.Point(112, 51);
            this.txtToDir.Name = "txtToDir";
            this.txtToDir.Size = new System.Drawing.Size(216, 20);
            this.txtToDir.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(54, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(52, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Directory:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Black;
            this.label4.Location = new System.Drawing.Point(21, 27);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "Version Number:";
            // 
            // numToVersion
            // 
            this.numToVersion.DecimalPlaces = 2;
            this.numToVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numToVersion.ForeColor = System.Drawing.Color.Black;
            this.numToVersion.Location = new System.Drawing.Point(112, 25);
            this.numToVersion.Name = "numToVersion";
            this.numToVersion.Size = new System.Drawing.Size(65, 20);
            this.numToVersion.TabIndex = 0;
            this.numToVersion.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numToVersion.ValueChanged += new System.EventHandler(this.numToVersion_ValueChanged);
            // 
            // dlgBrowse
            // 
            this.dlgBrowse.ShowNewFolderButton = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.txtPatchNotes);
            this.groupBox3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox3.ForeColor = System.Drawing.Color.Gray;
            this.groupBox3.Location = new System.Drawing.Point(12, 382);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(427, 149);
            this.groupBox3.TabIndex = 6;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Patch Notes For The New Version";
            // 
            // txtPatchNotes
            // 
            this.txtPatchNotes.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPatchNotes.Location = new System.Drawing.Point(6, 22);
            this.txtPatchNotes.Multiline = true;
            this.txtPatchNotes.Name = "txtPatchNotes";
            this.txtPatchNotes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPatchNotes.Size = new System.Drawing.Size(413, 121);
            this.txtPatchNotes.TabIndex = 0;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.cmdBrowseOutput);
            this.groupBox4.Controls.Add(this.txtOutputDir);
            this.groupBox4.Controls.Add(this.label5);
            this.groupBox4.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox4.ForeColor = System.Drawing.Color.Gray;
            this.groupBox4.Location = new System.Drawing.Point(12, 319);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(427, 57);
            this.groupBox4.TabIndex = 6;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Patch File Output Directory:";
            // 
            // cmdBrowseOutput
            // 
            this.cmdBrowseOutput.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmdBrowseOutput.ForeColor = System.Drawing.Color.Black;
            this.cmdBrowseOutput.Location = new System.Drawing.Point(334, 22);
            this.cmdBrowseOutput.Name = "cmdBrowseOutput";
            this.cmdBrowseOutput.Size = new System.Drawing.Size(75, 22);
            this.cmdBrowseOutput.TabIndex = 4;
            this.cmdBrowseOutput.Text = "Brwose";
            this.cmdBrowseOutput.UseVisualStyleBackColor = true;
            this.cmdBrowseOutput.Click += new System.EventHandler(this.cmdBrowseOutput_Click);
            // 
            // txtOutputDir
            // 
            this.txtOutputDir.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOutputDir.Location = new System.Drawing.Point(112, 23);
            this.txtOutputDir.Name = "txtOutputDir";
            this.txtOutputDir.Size = new System.Drawing.Size(216, 20);
            this.txtOutputDir.TabIndex = 3;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.Black;
            this.label5.Location = new System.Drawing.Point(54, 26);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(52, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Directory:";
            // 
            // cmdGenPatch
            // 
            this.cmdGenPatch.Location = new System.Drawing.Point(162, 596);
            this.cmdGenPatch.Name = "cmdGenPatch";
            this.cmdGenPatch.Size = new System.Drawing.Size(131, 23);
            this.cmdGenPatch.TabIndex = 7;
            this.cmdGenPatch.Text = "Generate Patch";
            this.cmdGenPatch.UseVisualStyleBackColor = true;
            this.cmdGenPatch.Click += new System.EventHandler(this.cmdGenPatch_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.txtVersionsEntry);
            this.groupBox5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox5.ForeColor = System.Drawing.Color.Gray;
            this.groupBox5.Location = new System.Drawing.Point(12, 537);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(427, 50);
            this.groupBox5.TabIndex = 7;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Copy/paste into patch server\'s Verions.txt file:";
            // 
            // txtVersionsEntry
            // 
            this.txtVersionsEntry.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtVersionsEntry.ForeColor = System.Drawing.Color.Maroon;
            this.txtVersionsEntry.Location = new System.Drawing.Point(24, 23);
            this.txtVersionsEntry.Name = "txtVersionsEntry";
            this.txtVersionsEntry.ReadOnly = true;
            this.txtVersionsEntry.Size = new System.Drawing.Size(385, 20);
            this.txtVersionsEntry.TabIndex = 0;
            // 
            // errProv
            // 
            this.errProv.ContainerControl = this;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(445, 628);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.cmdGenPatch);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WISP Patch Gen GUI";
            ((System.ComponentModel.ISupportInitialize)(this.numFromVersion)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numToVersion)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errProv)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NumericUpDown numFromVersion;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtFromDir;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button cmdBrowseFrom;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button cmdBrowseTo;
        private System.Windows.Forms.TextBox txtToDir;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numToVersion;
        private System.Windows.Forms.FolderBrowserDialog dlgBrowse;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox txtPatchNotes;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button cmdBrowseOutput;
        private System.Windows.Forms.TextBox txtOutputDir;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button cmdGenPatch;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.TextBox txtVersionsEntry;
        private System.Windows.Forms.ErrorProvider errProv;
    }
}

