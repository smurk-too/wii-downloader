using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace WiiDownloader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {                                                  
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);  
         
            Application.Run(new WiiDownloader_Form());        
        }        
    }
}
