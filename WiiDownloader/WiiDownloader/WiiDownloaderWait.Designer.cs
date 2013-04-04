namespace WiiDownloader
{
    partial class WiiDownloaderWait
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WiiDownloaderWait));
            this.labelWiiDownloaderWait = new System.Windows.Forms.Label();
            this.labelFirstTime = new System.Windows.Forms.Label();
            this.progressBar2 = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // labelWiiDownloaderWait
            // 
            this.labelWiiDownloaderWait.AutoSize = true;
            this.labelWiiDownloaderWait.Location = new System.Drawing.Point(13, 23);
            this.labelWiiDownloaderWait.Name = "labelWiiDownloaderWait";
            this.labelWiiDownloaderWait.Size = new System.Drawing.Size(424, 24);
            this.labelWiiDownloaderWait.TabIndex = 1;
            this.labelWiiDownloaderWait.Text = "Please, wait while WiiDownloader startup.";
            this.labelWiiDownloaderWait.UseWaitCursor = true;
            // 
            // labelFirstTime
            // 
            this.labelFirstTime.AutoSize = true;
            this.labelFirstTime.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFirstTime.Location = new System.Drawing.Point(14, 59);
            this.labelFirstTime.MaximumSize = new System.Drawing.Size(300, 20);
            this.labelFirstTime.MinimumSize = new System.Drawing.Size(300, 20);
            this.labelFirstTime.Name = "labelFirstTime";
            this.labelFirstTime.Size = new System.Drawing.Size(300, 20);
            this.labelFirstTime.TabIndex = 2;
            this.labelFirstTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelFirstTime.UseWaitCursor = true;
            // 
            // progressBar2
            // 
            this.progressBar2.Location = new System.Drawing.Point(-5, 91);
            this.progressBar2.Name = "progressBar2";
            this.progressBar2.Size = new System.Drawing.Size(589, 22);
            this.progressBar2.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar2.TabIndex = 4;
            this.progressBar2.UseWaitCursor = true;
            this.progressBar2.Value = 100;
            // 
            // WiiDownloaderWait
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(584, 112);
            this.ControlBox = false;
            this.Controls.Add(this.progressBar2);
            this.Controls.Add(this.labelFirstTime);
            this.Controls.Add(this.labelWiiDownloaderWait);
            this.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.MaximumSize = new System.Drawing.Size(600, 150);
            this.MinimumSize = new System.Drawing.Size(600, 150);
            this.Name = "WiiDownloaderWait";
            this.ShowIcon = false;
            this.Text = "WiiDownloader";
            this.UseWaitCursor = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelWiiDownloaderWait;
        public System.Windows.Forms.Label labelFirstTime;
        private System.Windows.Forms.ProgressBar progressBar2;
    }
}