using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace WiiDownloader
{
    public partial class WiiDownloaderWait : Form
    {
        public WiiDownloaderWait()
        {    
            InitializeComponent();
          /*  string SETTINGS_INI_FILE = System.IO.Path.Combine(System.IO.Path.GetFullPath(".\\"), @"Database", @"Settings", @"settings.ini");
            if (!File.Exists(SETTINGS_INI_FILE))
                labelFirstTime.Visible = true;
            else
                labelFirstTime.Visible = false;     */         
        }
    }
}
