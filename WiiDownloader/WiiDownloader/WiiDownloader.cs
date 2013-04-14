using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net;
using Ini;
using System.Threading;
using System.Reflection;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace WiiDownloader
{
    public partial class WiiDownloader_Form : Form
    {
        /*  WiiDownloader VERSION */
        string WiiDownloader_version = "3.3";

        string hackmii_installer_version = "1.2";
        string hackmii_installer_old_version = "1.0";     
        
        WiiDownloaderWait WiiDownloaderWaitForm = new WiiDownloaderWait();

        string folderForFiles = "COPY_TO_DEVICE";
        string FEATURED_DATABASE_VERSION, FEATURED_APPLICATION_VERSION, FEATURED_MODMII_VERSION, FEATURED_IMAGES_VERSION;         

        int HelpPage, passageFreezed, errorCount, downloadToDo, downloadJustStarted, stepNumber;
        int TYMER_INTERVAL;
        int MAX_DELAY_FOR_TYMER;
        static int fileCompareError, max_compare_error = 10;
        long fileJustDownloaded, fileSize;

        bool SHOW_PROCESS, UPDATE_MD5_INI_FILE, NoFreeSpace, NetworkOk, CACHE_ENABLED, timeOut, wadDownloadComplete;
        bool DOWNLOAD_OR_PROGRAM_WORKING, EnableSearch, ContentDownloaded, timerWorking, firmwareChaghed = true;       
        
        /*  WiiDownloader LINK */
        string codeGooglePage = "http://wii-downloader.googlecode.com";
        string wiiDownloaderFilesLink = "http://wii-downloader.googlecode.com/files/";
        string WiiDownloaderGooglePage = "http://code.google.com/p/wii-downloader/downloads/list?can=2&q=&colspec=Filename+Summary+Uploaded+ReleaseDate+Size+DownloadCount";
        string ModmiiGooglePage = "http://code.google.com/p/modmii/downloads/list?can=3&q=&colspec=Filename+Summary+Uploaded+ReleaseDate+Size+DownloadCount";
        string imageFolderLink = "https://dl.dropbox.com/u/89890459/WiiDownloader/Database/images/";

        string image_PAD_Path, image_PLUS_Path, image_MINUS_Path, image_ONE_Path, image_A_Path, image_B_Path, image_HOME_Path;

        internal const int SC_CLOSE = 0xF060;           //close button's code in Windows API
        internal const int MF_ENABLED = 0x00000000;     //enabled button status
        internal const int MF_GRAYED = 0x1;             //disabled button status (enabled = false)                   

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr HWNDValue, bool isRevert);

        [DllImport("user32.dll")]
        private static extern int EnableMenuItem(IntPtr tMenu, int targetItem, int targetStatus);

        private System.Windows.Forms.TabControl _tabControl;
        private System.Windows.Forms.TabPage _tabPage1;
        private System.Windows.Forms.TabPage _tabPage2;
        private System.Windows.Forms.TabPage _tabPage3;
        private System.Windows.Forms.TabPage _tabPage4;

        string  DIR_FOR_NUSD,                
                STARTUP_PATH,                
                SETTINGS_PATH,
                NUS_PATH,
                DOWNLOAD_PATH,
                CACHE_PATH,
                EXTRACTED_FILES_PATH,
                LANGUAGES_PATH,
                SCRIPT_PATH,
                CUSTOM_SCRIPT_PATH,
                STANDARD_SCRIPT_PATH,
                TEMP_SCRIPT_PATH,
                IMAGES_PATH,
                TOOLS_PATH,
                TEMP_PATH,
                TUTORIAL_PATH,
                TUTORIAL_IMAGES_PATH,
                DATABASE_PATH,                
                MOD_MII_TEMP_PATH,
                MOD_MII_PATH,
                MOD_MII_OUTPUT_PATH,
                MOD_MII_DOWNLOAD_QUEUES_PATH,
                SETTINGS_INI_FILE,
                WIIDOWNLOADER_TEMP_FOLDER;        

        public WiiDownloader_Form()
        {
            InitializeComponent();                      

            STARTUP_PATH = System.IO.Directory.GetCurrentDirectory();
            
            WIIDOWNLOADER_TEMP_FOLDER = CombinePath(System.IO.Directory.GetDirectoryRoot(STARTUP_PATH), "wiidownloader_temp_folder");            

            if (Directory.Exists(CombinePath(STARTUP_PATH, "Database", "script", "Standard", "Hacking")))
                DeleteFolder(CombinePath(STARTUP_PATH, "Database", "script", "Standard", "Hacking"), true);

            WiiDownloaderWaitForm.Owner = this;
            WiiDownloaderWaitForm.Show();
            WiiDownloaderWaitForm.Refresh();

            progressBarDownload.Value = 0;
            fileCompareError = 0;                       
            
            linkLabelVersion.Text = "WiiDownloader v." + WiiDownloader_version;

            labelDownloadToDo.Text = "";
            this.Location = new Point(100, 30);

            // associate variabiles path
            associatePath();

            // check if WiiDownloader is just started
            CheckForStartupOptions();

            // temp files and folder clean           
            FileDelete(CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "wiidownloaderTempLink.txt"));
            DeleteFolder(WIIDOWNLOADER_TEMP_FOLDER, true);

            // no more used files
            FileDelete(CombinePath(STARTUP_PATH, "WiiDownloaderUpdater.bat"));            
            FileDelete(CombinePath(STARTUP_PATH, "WiiDownloaderDoUpdate.bat"));
            FileDelete(CombinePath(STARTUP_PATH, "NewWiiDownloader.exe"));            
            FileDelete(CombinePath(STARTUP_PATH, "libWiiSharp.dll"));
            FileDelete(CombinePath(SETTINGS_PATH, "WiiDownloaderInfo.ini"));
            FileDelete(CombinePath(TOOLS_PATH, "createBootmiiLink.bat"));
            FileDelete(CombinePath(TOOLS_PATH, "createMediafireLink.bat"));
            FileDelete(CombinePath(STARTUP_PATH, "error_log.txt"));
            FileDelete(CombinePath(STARTUP_PATH, "tutorial.txt"));

            if (Directory.Exists(TEMP_SCRIPT_PATH))
                DeleteFolder(TEMP_SCRIPT_PATH, true);

            EnableSearch = true;
            DOWNLOAD_OR_PROGRAM_WORKING = false;
            DirectoryCheck();

            if (!File.Exists(SETTINGS_INI_FILE))
                createSettingsFile();            
            
            // associo le varie tab pages e le salvo
            _tabControl = MyTabControl;
            _tabPage1 = MyTabControl.TabPages[0];
            _tabPage2 = MyTabControl.TabPages[1];
            _tabPage3 = MyTabControl.TabPages[2];
            _tabPage4 = MyTabControl.TabPages[3];

            // e ora "nascondo" quelle che non servono (eliminandole)
            _tabControl.TabPages.Remove(_tabPage2);
            _tabControl.TabPages.Remove(_tabPage3);
            _tabControl.TabPages.Remove(_tabPage4);

            createDictionary();            

            NetworkCheck();

            if (NetworkOk)
                TakeLastFeaturedVersion();
            MySleep(100);

            string errorMsg;
            if (NetworkOk)
                errorMsg = "WiiDownloader will be closed.\n\r\n\rSorry but probably is only a problem on code.google ,\n\rand I can't do anything for resolve this.\n\rI can suggest you to try again later.";
            else
                errorMsg = "Without connection isn't possible to download necessary files for use WiiDownloader.";

            CheckForToolsFile(errorMsg);
            MySleep(100);
            CheckForImagesFile(errorMsg);
            MySleep(100);

            radioButtonStandard.Checked = true;

            StartupCheck(false);

            if (WiiDownloaderWaitForm.IsHandleCreated)
                WiiDownloaderWaitForm.Close();
            
            // forse non serve
            SetAllowUnsafeHeaderParsing(true);            
                      
        }

        private void WiiDownloaderJustStartedCheck()
        {
            int cont = 0;
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName == "WiiDownloader")
                    cont++;
            }
            if (cont > 1)
            {
                MessageBox.Show("WiiDownloader has been already opened/started.", "WiiDownloader", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(-1);
            }
        }

        public static class Prompt
        {
            public static void ShowHBCDialog(string titile, string whyWilbrand, string homebrewChannel, string hbc_image_path)
            {                
                Form prompt = new Form();
                prompt.Width = 500;
                prompt.Height = 330;
                prompt.Text = titile;
                prompt.Icon = WiiDownloader_Form.ActiveForm.Icon;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;
                prompt.MaximumSize = new Size(500, 330);
                prompt.MinimumSize = new Size(500, 330);
                prompt.SizeGripStyle = SizeGripStyle.Hide;

                Label textLabel_whyWilbrand = new Label() { Left = 20, Top = 20, Text = whyWilbrand, Size = new Size(450, 60), Font = new System.Drawing.Font("Arial", 10F) };                
                Button confirmation = new Button() { Left = 350, Top = 215, Height = 50, Width = 100, Text = "Ok" , Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))};          
     
                confirmation.Click += (sender, e) => { prompt.Close(); };
                prompt.Controls.Add(textLabel_whyWilbrand);                
                if (hbc_image_path != "")
                {
                    Label textLabel_homebrewChannel = new Label() { Left = 20, Top = 90, Text = homebrewChannel, Size = new Size(450, 40), Font = new System.Drawing.Font("Arial", 10F) };
                    PictureBox hbcimage = new PictureBox() { Left = 20, Top = 130, Height = 160, Width = 200, Image = new Bitmap(hbc_image_path) };
                    prompt.Controls.Add(textLabel_homebrewChannel);
                    prompt.Controls.Add(hbcimage);
                }
                prompt.Controls.Add(confirmation);   
                
                prompt.ControlBox = false;
                prompt.ShowDialog();                
            }
        }

        private void CheckForStartupOptions()
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            // change it to TRUE only for update MD5 and filesize in nusdabtabase.ini
            if (ini.IniReadValue("options", "update_md5_ini_file") == "True")
                UPDATE_MD5_INI_FILE = true;
            else
                UPDATE_MD5_INI_FILE = false;

            if (ini.IniReadValue("options", "show_process") == "True")
                SHOW_PROCESS = true;
            else
                SHOW_PROCESS = false;

            this.Text = "WiiDownloader";
            if (UPDATE_MD5_INI_FILE || SHOW_PROCESS)
                this.Text = this.Text + " [";
            if (UPDATE_MD5_INI_FILE)
                this.Text = this.Text + "MD5 change enabled";
            if (UPDATE_MD5_INI_FILE && SHOW_PROCESS)
                this.Text = this.Text + " and ";
            if (SHOW_PROCESS)
                this.Text = this.Text + "show process enabled";
            if (UPDATE_MD5_INI_FILE || SHOW_PROCESS)
                this.Text = this.Text + "]";
        }

        private void associatePath()
        {
            DATABASE_PATH = CombinePath(STARTUP_PATH, "Database");
            NUS_PATH = CombinePath(DATABASE_PATH, "nus");
            TEMP_PATH = CombinePath(DATABASE_PATH, "temp");
            DOWNLOAD_PATH = CombinePath(TEMP_PATH, "download");
            CACHE_PATH = CombinePath(DATABASE_PATH, "cache");
            EXTRACTED_FILES_PATH = CombinePath(TEMP_PATH, "extract");
            LANGUAGES_PATH = CombinePath(DATABASE_PATH, "languages");
            SCRIPT_PATH = CombinePath(DATABASE_PATH, "script");
            CUSTOM_SCRIPT_PATH = CombinePath(SCRIPT_PATH, "Custom");
            STANDARD_SCRIPT_PATH = CombinePath(SCRIPT_PATH, "Standard");
            TEMP_SCRIPT_PATH = CombinePath(SCRIPT_PATH, "Temp");
            IMAGES_PATH = CombinePath(DATABASE_PATH, "images");
            TOOLS_PATH = CombinePath(DATABASE_PATH, "tools");
            TUTORIAL_PATH = CombinePath(DATABASE_PATH, "tutorial");
            TUTORIAL_IMAGES_PATH = CombinePath(TUTORIAL_PATH, "images");
            MOD_MII_PATH = CombinePath(DATABASE_PATH, "ModMii");
            MOD_MII_TEMP_PATH = CombinePath(MOD_MII_PATH, "temp");
            MOD_MII_OUTPUT_PATH = CombinePath(MOD_MII_PATH, "COPY_TO_SD", "WAD");
            MOD_MII_DOWNLOAD_QUEUES_PATH = CombinePath(MOD_MII_PATH, "temp", "DownloadQueues");
            SETTINGS_PATH = CombinePath(DATABASE_PATH, "settings");
            SETTINGS_INI_FILE = CombinePath(SETTINGS_PATH, "settings.ini");
        }

        private bool waitForAppClose(string processToCheck)
        {
            int loop_count = 0;
            string process_checked;
           
            if (processToCheck != "editedmodmii")
            {
            loop_for_Wait:
                foreach (Process clsProcess in Process.GetProcesses())
                {
                    process_checked = clsProcess.ProcessName.ToLower();
                    if (process_checked == processToCheck)
                    {
                        MySleep(1000);
                        loop_count++;
                        if (loop_count < 20)
                            goto loop_for_Wait;
                        else
                            return false; // dopo 20 secondi che nusd non ha creato il WAD (a download compleato) c'è qualcosa che non va.
                    }
                }
            }
            else
                MySleep(1000); // modmmi ha già finito di sicuro tutto, ma aspetto 1 secondo tanto per sicurezza. =P

            MySleep(100);

            return true;
        }


        private void stopUsedProcess(string processToClose)
        {
           
            string process_checked;
            //faccio terminare tutti i processi utilizzabili da WiiDownloader

            if (processToClose != "editedmodmii")
            {              
                // se non è modmmi becco il programma utilizzato
                foreach (Process clsProcess in Process.GetProcesses())
                {
                    process_checked = clsProcess.ProcessName.ToLower();
                    if (process_checked == processToClose) 
                        clsProcess.Kill();
                }
            }
            else // usando modmmi si usano tutti sti programmi:
            {
                foreach (Process clsProcess in Process.GetProcesses())
                {
                    process_checked = clsProcess.ProcessName.ToLower();
                    if ((process_checked == "patchios") ||
                        (process_checked == "tmdedit") ||
                        (process_checked == "hexalter") ||
                        (process_checked == "findstr") ||
                        (process_checked == "nusd") ||
                        (process_checked == "wadmii") ||
                        (process_checked == "jptch") ||
                        (process_checked == "wget") ||
                        (process_checked == "sfk"))

                        clsProcess.Kill();
                }
            }
            Thread.Sleep(0);            
        }

        public void MySleep(int milliseconds)
        {
            bool timeElapsed = false;
            var TimerForSleep = new System.Timers.Timer(milliseconds);            
            TimerForSleep.Elapsed += (s, e) => timeElapsed = true;
            TimerForSleep.Start(); 

            while(!timeElapsed)
                Application.DoEvents();

            Thread.Sleep(0);
        }

        public string CombinePath(params string[] path)
        {
            string new_path = path[0];         

            for (int i = 1; i < path.Length; i++)
                new_path = System.IO.Path.Combine(new_path, path[i]);

            return new_path;
        }

        private void NetworkCheck()
        {
            NetworkOk = true;
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                return;
            else            
                NetworkOk = false;              
                                         
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            string standardDatabaseVersion = ini.IniReadValue("version", "database");

            if (standardDatabaseVersion == "0" || standardDatabaseVersion == "")
            {
                MessageBox.Show("Network connection not available. WiiDownloader will be closed.", "WiiDownloader", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }
            else
            {              
                DialogResult ans = MessageBox.Show(Dictionary.NoNetwork, "WiiDownloader", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (ans == DialogResult.No)
                    Environment.Exit(-1);
                else
                {
                    FEATURED_DATABASE_VERSION = FEATURED_APPLICATION_VERSION = FEATURED_MODMII_VERSION = FEATURED_IMAGES_VERSION = "";
                    SetFalseFeaturedVersion();
                    applicationUpdateToolStripMenuItem.Enabled = false;
                    databaseUpdateToolStripMenuItem.Enabled = false;
                    this.Text = "WiiDownloader [offline mode]";
                }
            }                      
        }              

        static bool AddFileCompareError()
        {
            fileCompareError++;

            if (fileCompareError > max_compare_error)                          
                return false;
            else
                return true;
        }

        static bool FileJustOpen(string file)
        {
            bool fisrtTry = true;
        retry_to_check:
            if (FileInUse(file))
            {
                if (fisrtTry)
                {
                    Thread.Sleep(2000);
                    fisrtTry = false;
                    goto retry_to_check;
                }
                
                string errorMsg;
                if (Dictionary.FileInUse == "")
                    errorMsg = "This file is being used by another program: if is possible, close that process.";
                else
                    errorMsg = Dictionary.FileInUse;

                DialogResult myDialogResult;
                myDialogResult = MessageBox.Show(file + "\n\r\n\r" + errorMsg, "WiiDownloader - Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);

                if (myDialogResult == DialogResult.Retry)
                    goto retry_to_check;
                else
                    return true;
            }
            return false;
        }


        static bool FileCompare(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
        retry_fs1:
            try
            {
                if(FileJustOpen(file1))
                    return false;
                fs1 = new FileStream(file1, FileMode.Open);
            }
            catch
            {
                try
                {
                    File.SetAttributes(file1, FileAttributes.Normal);
                    Thread.Sleep(0);
                    Thread.Sleep(100);
                    goto retry_fs1;
                }
                catch
                {
                    return false;
                }
            }

        retry_fs2:
            try
            {
                if (FileJustOpen(file2))
                    return false;
                fs2 = new FileStream(file2, FileMode.Open);
            }
            catch
            {
                try
                {
                    File.SetAttributes(file2, FileAttributes.Normal);
                    Thread.Sleep(0);
                    Thread.Sleep(100);
                    goto retry_fs2;
                }
                catch
                {
                    return false;
                }
            }



            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);
        }

        private bool ToolsFileCheck(string path, string fileToCheck)
        {
            string md5 = "";
            if (!File.Exists(CombinePath(path, fileToCheck)))
                return false;            

            switch (fileToCheck)
            {
                case "7za920.exe":
                    md5 = "42badc1d2f03a8b1e4875740d3d49336";
                    break;
                case "unrar.exe":
                    md5 = "d5cfde06873a24a7e04dba14400efc3c";
                    break;
                case "patchIOS.exe":
                    md5 = "a7498be06937038b6de022bf62cc31a6";
                    break;
                case "wget.exe":
                    md5 = "bd126a7b59d5d1f97ba89a3e71425731";
                    break;
                case "sleep.exe":
                    md5 = "1a1075e5e307f3a4b8527110a51ce827";
                    break;
                case "libWiiSharp.dll":
                    md5 = "bef875be2b7f4194af4cbcf59a84756c";
                    break;
                case "nusd.exe":
                    md5 = "277e97d9a308edfb96ebb407c51a2ec7";
                    break;                    
                case "wilbrand.exe":
                    md5 = "008c00606ce4e86173d2eeb4009d9b9d";
                    break;                
                default:
                    return false;
            }

            string md5_from_file = GetMD5HashFromFile(CombinePath(path, fileToCheck));

            if (md5_from_file != md5)
                return false;
            return true;
        }

        public void TakeLastFeaturedVersion()
        {
            FEATURED_DATABASE_VERSION = FEATURED_APPLICATION_VERSION = FEATURED_MODMII_VERSION = FEATURED_IMAGES_VERSION = "";
            
            if(!NetworkOk)
                return;

            string[] versionFile;

            if (WiiDownloaderWaitForm.IsHandleCreated)
                WiiDownloaderWaitForm.labelFirstTime.Text = "...Initial check...";            

            string errorMsg = "WiiDownloader will be closed.\n\r\n\rSorry but probably is only a problem on code.google server,\n\rand I can't do anything for resolve this.\n\rI can suggest you to try again later.";
            
            if (!DoDownloadForApplication("lastVersion.txt",
                                WiiDownloaderGooglePage,
                                SETTINGS_PATH,
                                true))
                    {
                        MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(-1);
                    }
           
            if (!File.Exists(CombinePath(SETTINGS_PATH, "lastVersion.txt")))
                return;

            // serching last version for WiiDownloader
            versionFile = File.ReadAllLines(CombinePath(SETTINGS_PATH, "lastVersion.txt"));
            foreach (string line in versionFile)
            {
                if (line.Trim() == "")
                    continue;
                if (!line.Contains("WiiDownloader v") && !line.Contains("WiiDownloaderImages v") && !line.Contains("WiiDownloaderDatabase v"))
                    continue;
                if (!line.Contains(".zip"))
                    continue;

                if (line.Contains("WiiDownloader v"))
                {
                    FEATURED_APPLICATION_VERSION = line;
                    FEATURED_APPLICATION_VERSION = FEATURED_APPLICATION_VERSION.Replace("WiiDownloader v", "");
                    FEATURED_APPLICATION_VERSION = FEATURED_APPLICATION_VERSION.Replace(".zip", "");
                    FEATURED_APPLICATION_VERSION = FEATURED_APPLICATION_VERSION.Trim();
                }
                else if (line.Contains("WiiDownloaderImages v"))
                {
                    FEATURED_IMAGES_VERSION = line;
                    FEATURED_IMAGES_VERSION = FEATURED_IMAGES_VERSION.Replace("WiiDownloaderImages v", "");
                    FEATURED_IMAGES_VERSION = FEATURED_IMAGES_VERSION.Replace(".zip", "");
                    FEATURED_IMAGES_VERSION = FEATURED_IMAGES_VERSION.Trim();
                }
                else if (line.Contains("WiiDownloaderDatabase v"))
                {
                    FEATURED_DATABASE_VERSION = line;
                    FEATURED_DATABASE_VERSION = FEATURED_DATABASE_VERSION.Replace("WiiDownloaderDatabase v", "");
                    FEATURED_DATABASE_VERSION = FEATURED_DATABASE_VERSION.Replace(".zip", "");
                    FEATURED_DATABASE_VERSION = FEATURED_DATABASE_VERSION.Trim();
                }

                if (FEATURED_APPLICATION_VERSION != "" && FEATURED_IMAGES_VERSION != "" && FEATURED_DATABASE_VERSION != "")
                    break;
            }            
            
            if (!DoDownloadForApplication("lastVersion.txt",
                                ModmiiGooglePage,
                                SETTINGS_PATH,
                                true))
            {
                MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            if (!File.Exists(CombinePath(SETTINGS_PATH, "lastVersion.txt")))
                return;

            // serching last version for WiiDownloader
            versionFile = File.ReadAllLines(CombinePath(SETTINGS_PATH, "lastVersion.txt"));
            foreach (string line in versionFile)
            {
                if (line.Trim() == "")
                    continue;
                if (line.Contains("ModMii v"))                 
                {
                    FEATURED_MODMII_VERSION = line;
                    FEATURED_MODMII_VERSION = FEATURED_MODMII_VERSION.Replace("ModMii v", "");
                    FEATURED_MODMII_VERSION = FEATURED_MODMII_VERSION.Replace(".zip", "");
                    FEATURED_MODMII_VERSION = FEATURED_MODMII_VERSION.Trim();
                    if (FEATURED_MODMII_VERSION.Length != 5)                    
                        FEATURED_MODMII_VERSION = "";

                    if (FEATURED_MODMII_VERSION != "")
                        break;
                    
                }                
            }

            FileDelete(CombinePath(SETTINGS_PATH, "lastVersion.txt"));
            SetFalseFeaturedVersion();            
        }

        public void SetFalseFeaturedVersion()
        {
            IniFile settingsFile = new IniFile(SETTINGS_INI_FILE);                          

            if (FEATURED_APPLICATION_VERSION == "")
                FEATURED_APPLICATION_VERSION = WiiDownloader_version;
            if (FEATURED_IMAGES_VERSION == "")
                FEATURED_IMAGES_VERSION = settingsFile.IniReadValue("version", "images");
            if (FEATURED_DATABASE_VERSION == "")
                FEATURED_DATABASE_VERSION = settingsFile.IniReadValue("version", "database");
            if (FEATURED_MODMII_VERSION == "")
                FEATURED_MODMII_VERSION = settingsFile.IniReadValue("version", "modmii");         
        }

        public void CheckForToolsFile(string errorMsg)
        {
            if (!ToolsFileCheck(TOOLS_PATH, "7za920.exe"))
            {
                if (!DoDownloadForApplication("7za920.exe",
                            wiiDownloaderFilesLink + "7za920.exe",                            
                            TOOLS_PATH,
                            true))
                {
                    MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }

            System.IO.DirectoryInfo toolsDir = new System.IO.DirectoryInfo(TOOLS_PATH);
            if (toolsDir.GetFiles().Length < 5)
            {

                if (!DoDownloadForApplication("tools.zip",                                     
                                     wiiDownloaderFilesLink + "tools.zip",                     
                                     DATABASE_PATH,
                                     true))
                {
                    MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }

                if (File.Exists(CombinePath(DATABASE_PATH, "tools.zip")))
                {
                    DOWNLOAD_OR_PROGRAM_WORKING = true;
                    if (WiiDownloaderWaitForm.IsHandleCreated)
                        WiiDownloaderWaitForm.labelFirstTime.Text = "...extracting " + "tools.zip";

                    if (!unZip("tools.zip",
                                 CombinePath(DATABASE_PATH, "tools.zip"),
                                 DATABASE_PATH, true))
                    {
                        MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(-1);
                    }

                    FileDelete(CombinePath(DATABASE_PATH, "tools.zip"));

                    DOWNLOAD_OR_PROGRAM_WORKING = false;
                }
            }

            if (!ToolsFileCheck(TOOLS_PATH, "unrar.exe"))
            {
                if (!DoDownloadForApplication("unrar.exe",
                            wiiDownloaderFilesLink + "unrar.exe",
                            TOOLS_PATH,
                            true))
                {
                    MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }


            if (!ToolsFileCheck(TOOLS_PATH, "patchIOS.exe"))
            {
                if (!DoDownloadForApplication("patchIOS.exe",
                            wiiDownloaderFilesLink + "patchIOS.exe",                            
                            TOOLS_PATH,
                            true))
                {
                    MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }


            if (!ToolsFileCheck(TOOLS_PATH, "wget.exe"))
            {
                if (!DoDownloadForApplication("wget.exe",
                            wiiDownloaderFilesLink + "wget.exe",
                            TOOLS_PATH,
                            true))
                {
                    MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }

            if (!ToolsFileCheck(TOOLS_PATH, "sleep.exe"))
            {
                if (!DoDownloadForApplication("sleep.exe",
                            wiiDownloaderFilesLink + "sleep.exe",                            
                            TOOLS_PATH,
                            true))
                {
                    MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }

            }


            if (!ToolsFileCheck(TOOLS_PATH, "libWiiSharp.dll"))
            {
                if (!DoDownloadForApplication("libWiiSharp.dll",
                            wiiDownloaderFilesLink + "libWiiSharp.dll",                            
                            TOOLS_PATH,
                            true))
                {
                    MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }

            if (!ToolsFileCheck(TOOLS_PATH, "nusd.exe"))
            {
                if (!DoDownloadForApplication("nusd.exe",
                            wiiDownloaderFilesLink + "nusd.exe",                            
                            TOOLS_PATH,
                            true))
                {
                    MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }

            if (!ToolsFileCheck(TOOLS_PATH, "wilbrand.exe"))
            {
                if (!DoDownloadForApplication("wilbrand.exe",
                            wiiDownloaderFilesLink + "wilbrand.exe",                            
                            TOOLS_PATH,
                            true))
                {
                    MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }
        }

        public void CheckForImagesFile(string errorMsg)
        {
            if (!NetworkOk)
                return;

            IniFile settingsFile = new IniFile(SETTINGS_INI_FILE);
            if (settingsFile.IniReadValue("version", "images") == FEATURED_IMAGES_VERSION)
                return;

            if (!DoDownloadForApplication("images.zip",
                                 wiiDownloaderFilesLink + "WiiDownloaderImages%20v" + FEATURED_IMAGES_VERSION + ".zip",
                                 DATABASE_PATH,
                                 true))
            {
                MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            if (File.Exists(CombinePath(DATABASE_PATH, "images.zip")))
            {
                DOWNLOAD_OR_PROGRAM_WORKING = true;

                if (WiiDownloaderWaitForm.IsHandleCreated)
                    WiiDownloaderWaitForm.labelFirstTime.Text = "...extracting " + "images.zip";

                if (!unZip("images.zip",
                             CombinePath(DATABASE_PATH, "images.zip"),
                             DATABASE_PATH, true))
                {
                    MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }

                FileDelete(CombinePath(DATABASE_PATH, "images.zip"));
                settingsFile.IniWriteValue("version", "images", FEATURED_IMAGES_VERSION);
                DOWNLOAD_OR_PROGRAM_WORKING = false;
            }
            else
            {
                MessageBox.Show(errorMsg, "WiiDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }
        }


        public void StartupCheck(bool fromEditorMode)
        {
            if (fromEditorMode)
            {
                linkLabelOfficialSite.Enabled = false;

                if (WiiDownloader.ScriptEdit.Global.editorMode != "New")
                    EnableButton(comboBoxReload());
                else
                    EnableButton(comboBoxInit());
            }
            else
            {     
                createDictionary();

                // CHECK if wiidownloader is already opened
                WiiDownloaderJustStartedCheck();  

                if (WiiDownloaderWaitForm.IsHandleCreated)
                    WiiDownloaderWaitForm.labelFirstTime.Text = "...Checking for updates...";

                if (!callCheckSettings("Startup"))
                    MessageBox.Show(Dictionary.NoUpdateError, "WiiDownloader (Warning)", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                infoTextBox.Clear();
                linkLabelOfficialSite.Enabled = false;
                EnableButton(comboBoxInit());
            }
        }

        public void DirectoryCheck()
        {
            if (!Directory.Exists(DATABASE_PATH))
                Directory.CreateDirectory(DATABASE_PATH);

            if (!Directory.Exists(SETTINGS_PATH))
                Directory.CreateDirectory(SETTINGS_PATH);

            if (!Directory.Exists(NUS_PATH))
                Directory.CreateDirectory(NUS_PATH);

            if (!Directory.Exists(TOOLS_PATH))
                Directory.CreateDirectory(TOOLS_PATH);

            if (!Directory.Exists(IMAGES_PATH))
                Directory.CreateDirectory(IMAGES_PATH);

            if (!Directory.Exists(LANGUAGES_PATH))
                Directory.CreateDirectory(LANGUAGES_PATH);

            if (!Directory.Exists(TUTORIAL_PATH))
                Directory.CreateDirectory(TUTORIAL_PATH);

            if (!Directory.Exists(TUTORIAL_IMAGES_PATH))
                Directory.CreateDirectory(TUTORIAL_IMAGES_PATH);

            if (!Directory.Exists(SCRIPT_PATH))
                Directory.CreateDirectory(SCRIPT_PATH);

            if (!Directory.Exists(STANDARD_SCRIPT_PATH))
                Directory.CreateDirectory(STANDARD_SCRIPT_PATH);

            if (!Directory.Exists(DOWNLOAD_PATH))
                Directory.CreateDirectory(DOWNLOAD_PATH);

            if (!Directory.Exists(CACHE_PATH))
                Directory.CreateDirectory(CACHE_PATH);

            if (!Directory.Exists(TEMP_PATH))
                Directory.CreateDirectory(TEMP_PATH);

            if (!Directory.Exists(CUSTOM_SCRIPT_PATH))
                Directory.CreateDirectory(CUSTOM_SCRIPT_PATH);

            if (!Directory.Exists(MOD_MII_TEMP_PATH))
                Directory.CreateDirectory(MOD_MII_TEMP_PATH);

            if (!Directory.Exists(MOD_MII_PATH))
                Directory.CreateDirectory(MOD_MII_PATH);

            if (!Directory.Exists(MOD_MII_DOWNLOAD_QUEUES_PATH))
                Directory.CreateDirectory(MOD_MII_DOWNLOAD_QUEUES_PATH);

            if (!Directory.Exists(MOD_MII_OUTPUT_PATH))
                Directory.CreateDirectory(MOD_MII_OUTPUT_PATH);

        }

        public static class Global
        {
            public static string editorMode,
                                    name,
                                    type,
                                    group,
                                    LanguageChoice;
        }

        public static class Dictionary
        {
            public static string
                group,
                type,
                name,
                button_edit,
                StartDownload,
                StopDownload,
                WilbrandDisclamer,
                Download,

                // Message
                error,
                AskAppUpdate,
                AskDatabaseUpdate,
                UpdateSkipped,
                DatabaseUpdatedTo,
                Extracting,
                ErrorDownloadingFrom,
                Downloading,
                CheckInfoLastVersion,
                CheckInfoLastDBVersion,
                CheckInfoLastAPPVersion,
                NoUpdateToDo,
                ErrorOccurred,
                InvalidScript,
                NoDevice,
                AskForDelete,
                NoDeviceSelected,
                AskForSeeFiles,
                AskForMerge,
                AskForDeleteType,
                NoDescription,
                DownloadComplete,
                DownloadNotComplete,
                AskForRepeatDownload,
                AskForAbortDownload,
                AskForAbortNusDownload,
                AskForAbortCiosDownload,
                AskForRepeatSingleNusDownload,
                AskForRepeatSingleCiosDownload,                                
                DownloadStopped,
                Patching,
                CreatingLink,
                NoResponse,
                Creating,
                Copying,
                CreatingWadAndcopy,
                NoMac,
                NoFirmware,
                NoRegion,
                NoValidFirmware,
                CopyingFile,
                ToTheRoot,
                FileNotFound,
                WiiInfoNotFound,
                UsingWilbrand,
                HackScriptDisclaimer,
                scriptCreated,
                wiiInfoSaves,
                TooCompareError,
                NoCheckBox,
                FoundInCache,                
                DeleteCacheFiles,
                DeletingCacheFiles,
                FileInUse,
                NoNetwork,
                errorDownloadingWithNoNetwork,
                NoUpdateError,
                NoFreeSpace1,
                NoFreeSpace2,
                GuideCreated,
                WhyMACaddress,
                WhyWilbrand,
                HomebrewChannel;
        }


        private void createDictionary()
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            Global.LanguageChoice = ini.IniReadValue("Language", "LanguageChoice");

            IniFile dictionary = new IniFile(CombinePath(LANGUAGES_PATH, Global.LanguageChoice + ".ini"));

            englishToolStripMenuItem.Checked = false;
            italianoToolStripMenuItem.Checked = false;
            españolToolStripMenuItem.Checked = false;
            chineseToolStripMenuItem.Checked = false;            
            dutchToolStripMenuItem.Checked = false;
            frenchToolStripMenuItem.Checked = false;
            portuguêsToolStripMenuItem.Checked = false;

            _tabControl.TabPages.Add(_tabPage2);
            _tabControl.TabPages.Add(_tabPage3);
            _tabControl.TabPages.Add(_tabPage4);

            switch (Global.LanguageChoice)
            {
                case "italiano":
                    italianoToolStripMenuItem.Checked = true;
                    break;
                case "español":
                    españolToolStripMenuItem.Checked = true;
                    break;
                case "português":
                    portuguêsToolStripMenuItem.Checked = true;
                    break;
                case "french":
                    frenchToolStripMenuItem.Checked = true;
                    break;
                case "chinese":
                    chineseToolStripMenuItem.Checked = true;
                    break;               
                case "dutch":
                    dutchToolStripMenuItem.Checked = true;
                    break;
                case "english":
                default:
                    englishToolStripMenuItem.Checked = true;
                    break;
            }

            renameAlwaysToolStripMenuItem.Checked = false;
            askAlwaysToolStripMenuItem.Checked = false;
            overwriteAlwaysToolStripMenuItem.Checked = false;
            string AboutMerge = ini.IniReadValue("AboutMerge", "Checked");
            switch (AboutMerge)
            {
                case "MergeAlways":
                    overwriteAlwaysToolStripMenuItem.Checked = true;
                    break;
                case "AskAlways":
                    askAlwaysToolStripMenuItem.Checked = true;
                    break;
                case "RenameAlways":
                    renameAlwaysToolStripMenuItem.Checked = true;
                    break;
                default:
                    overwriteAlwaysToolStripMenuItem.Checked = true;
                    break;
            }


            // menu            
            if (dictionary.IniReadValue("menu", "settings") != "")
                settingsToolStripMenuItem.Text = dictionary.IniReadValue("menu", "settings");
            if (dictionary.IniReadValue("menu", "language") != "")
                languagesToolStripMenuItem.Text = dictionary.IniReadValue("menu", "language");
            if (dictionary.IniReadValue("menu", "aboutYourWii") != "")
            {
                modYourWiiToolStripMenuItem.Text = dictionary.IniReadValue("menu", "aboutYourWii") + " (Mod your Wii)";
                MyTabControl.TabPages[1].Text = dictionary.IniReadValue("menu", "aboutYourWii");                
            }         
            if (dictionary.IniReadValue("menu", "applicationUpdateMode") != "")
                applicationUpdateToolStripMenuItem.Text = dictionary.IniReadValue("menu", "applicationUpdateMode");
            if (dictionary.IniReadValue("menu", "databaseUpdateMode") != "")
                databaseUpdateToolStripMenuItem.Text = dictionary.IniReadValue("menu", "databaseUpdateMode");
            if (dictionary.IniReadValue("menu", "appAutomaticMenu") != "")
                appAutomaticToolStrip.Text = dictionary.IniReadValue("menu", "appAutomaticMenu");
            if (dictionary.IniReadValue("menu", "appAskMenu") != "")
                appAskToolStrip.Text = dictionary.IniReadValue("menu", "appAskMenu");
            if (dictionary.IniReadValue("menu", "appManualMenu") != "")
                appManualToolStrip.Text = dictionary.IniReadValue("menu", "appManualMenu");
            if (dictionary.IniReadValue("menu", "appUpdateNow") != "")
                updateNowAppToolStrip.Text = dictionary.IniReadValue("menu", "appUpdateNow");
            if (dictionary.IniReadValue("menu", "databaseAutomaticMenu") != "")
                databaseAutomaticToolStrip.Text = dictionary.IniReadValue("menu", "databaseAutomaticMenu");
            if (dictionary.IniReadValue("menu", "databaseAskMenu") != "")
                databaseAskToolStrip.Text = dictionary.IniReadValue("menu", "databaseAskMenu");
            if (dictionary.IniReadValue("menu", "databaseManualMenu") != "")
                databaseManualToolStrip.Text = dictionary.IniReadValue("menu", "databaseManualMenu");
            if (dictionary.IniReadValue("menu", "databaseUpdateNow") != "")
                updateNowDatabaseToolStrip.Text = dictionary.IniReadValue("menu", "databaseUpdateNow");
            if (dictionary.IniReadValue("menu", "about") != "")
            {
                aboutToolStripMenuItem.Text = dictionary.IniReadValue("menu", "about");                
                MyTabControl.TabPages[3].Text = dictionary.IniReadValue("menu", "about");                
            }
            if (dictionary.IniReadValue("menu", "group") != "")
                labelGroup.Text = Dictionary.group = dictionary.IniReadValue("menu", "group");
            else
                Dictionary.group = labelGroup.Text;
            if (dictionary.IniReadValue("menu", "type") != "")
                labelType.Text = Dictionary.type = dictionary.IniReadValue("menu", "type");
            else
                Dictionary.type = labelType.Text;
            if (dictionary.IniReadValue("menu", "name") != "")
                labelName.Text = Dictionary.name = dictionary.IniReadValue("menu", "name");
            else
                Dictionary.name = labelName.Text;
            if (dictionary.IniReadValue("menu", "buttonNew") != "")
                buttonNew.Text = dictionary.IniReadValue("menu", "buttonNew");

            if (radioButtonStandard.Checked == true)
            {
                if (dictionary.IniReadValue("menu", "buttonView") != "")
                    buttonEdit.Text = Dictionary.button_edit = dictionary.IniReadValue("menu", "buttonView");
                else
                    buttonEdit.Text = Dictionary.button_edit = "View";
            }
            else
            {
                if (dictionary.IniReadValue("menu", "buttonEdit") != "")
                    buttonEdit.Text = Dictionary.button_edit = dictionary.IniReadValue("menu", "buttonEdit");
                else
                    buttonEdit.Text = Dictionary.button_edit = "Edit";
            }

            if (dictionary.IniReadValue("menu", "buttonDelete") != "")
                buttonDelete.Text = dictionary.IniReadValue("menu", "buttonDelete");
            if (dictionary.IniReadValue("menu", "StartDownload") != "")
                buttonDonwload.Text = Dictionary.StartDownload = dictionary.IniReadValue("menu", "StartDownload");
            else
                buttonDonwload.Text = Dictionary.StartDownload = "Start Download";
            if (dictionary.IniReadValue("menu", "StopDownload") != "")
                Dictionary.StopDownload = dictionary.IniReadValue("menu", "StopDownload");
            else
                Dictionary.StopDownload = "Stop Download";
            if (dictionary.IniReadValue("menu", "AppOfficialSite") != "")
                linkLabelOfficialSite.Text = dictionary.IniReadValue("menu", "AppOfficialSite");
            if (dictionary.IniReadValue("menu", "CopyToSD") != "")
                checkBoxCopyToSD.Text = dictionary.IniReadValue("menu", "CopyToSD");
            if (dictionary.IniReadValue("menu", "aboutOverwrite") != "")
                aboutOverwriteToolStripMenuItem.Text = dictionary.IniReadValue("menu", "aboutOverwrite");
            if (dictionary.IniReadValue("menu", "OverwriteAlways") != "")
                overwriteAlwaysToolStripMenuItem.Text = dictionary.IniReadValue("menu", "OverwriteAlways");
            if (dictionary.IniReadValue("menu", "AskAlways") != "")
                askAlwaysToolStripMenuItem.Text = dictionary.IniReadValue("menu", "AskAlways");
            if (dictionary.IniReadValue("menu", "RenameAlways") != "")
                renameAlwaysToolStripMenuItem.Text = dictionary.IniReadValue("menu", "RenameAlways");

            if (checkBoxCreateScript.Checked == true)
            {
                if (dictionary.IniReadValue("menu", "saveAndCreateScript") != "")
                    buttonSaveWiiInfo.Text = dictionary.IniReadValue("menu", "saveAndCreateScript");
                else
                    buttonSaveWiiInfo.Text = "Save and create script";
            }
            else
            {
                if (dictionary.IniReadValue("menu", "save") != "")
                    buttonSaveWiiInfo.Text = dictionary.IniReadValue("menu", "save");
                else
                    buttonSaveWiiInfo.Text = "Save";
            }
            if (dictionary.IniReadValue("menu", "undo") != "")
                buttonCancelWiiInfo.Text = dictionary.IniReadValue("menu", "undo");
            if (dictionary.IniReadValue("menu", "close") != "")
                buttonCloseHelp.Text = dictionary.IniReadValue("menu", "close");
            if (dictionary.IniReadValue("menu", "CreateScriptForHack") != "")
                checkBoxCreateScript.Text = dictionary.IniReadValue("menu", "CreateScriptForHack");
            if (dictionary.IniReadValue("menu", "HowToFindFW") != "")
                linkLabelFW.Text = dictionary.IniReadValue("menu", "HowToFindFW");
            if (dictionary.IniReadValue("menu", "HowToFindMAC") != "")
                linkLabelMAC.Text = dictionary.IniReadValue("menu", "HowToFindMAC");
            if (dictionary.IniReadValue("menu", "Download") != "")
                Dictionary.Download = dictionary.IniReadValue("menu", "Download");
            else
                Dictionary.Download = "Download";
            checkBoxDownload41.Text = Dictionary.Download + " SM 4.1";
            checkBoxDownload42.Text = Dictionary.Download + " SM 4.2";
            checkBoxDownload43.Text = Dictionary.Download + " SM 4.3";
            if (dictionary.IniReadValue("menu", "USeWilbrandFor") != "")
                checkBoxUseWilbrand.Text = dictionary.IniReadValue("menu", "USeWilbrandFor");

            checkBoxDownloadHackmiiInstaller.Text = Dictionary.Download + " Hackmii Installer " + hackmii_installer_version;
            if (dictionary.IniReadValue("menu", "DownloadPatchedSystemIOS") != "")
                checkBoxDownloadPatchedSystemIOS.Text = dictionary.IniReadValue("menu", "DownloadPatchedSystemIOS");
            if (dictionary.IniReadValue("menu", "DownloadLastestChannel") != "")
                checkBoxDownloadOfficialChannel.Text = dictionary.IniReadValue("menu", "DownloadLastestChannel");
            if (dictionary.IniReadValue("menu", "DownloadActiveIOS") != "")
                checkBoxDownloadActiveIOS.Text = dictionary.IniReadValue("menu", "DownloadActiveIOS");
            if (dictionary.IniReadValue("menu", "DownloadRecommendedcIOS") != "")
                checkBoxDownloadCIOS.Text = dictionary.IniReadValue("menu", "DownloadRecommendedcIOS");

            checkBoxDownloadIOS236.Text = Dictionary.Download + " IOS236 Installer";
            checkBoxDownloadMoreWADmanager.Text = Dictionary.Download + " WAM (Wad/App Manager)";
            checkBoxDownloadPriiloader.Text = Dictionary.Download + " Priiloader";
            checkBoxDownloadGX.Text = Dictionary.Download + " USB Loader GX";
            checkBoxDownloadWiiFlow.Text = Dictionary.Download + " WiiFlow";

            checkBoxDownloadCFG.Text = Dictionary.Download + " Configurable USB Loader";
            if (dictionary.IniReadValue("menu", "DownloadHackmiiInstallerWAD") != "")
                checkBoxDownloadHackmiiInstallerWAD.Text = dictionary.IniReadValue("menu", "DownloadHackmiiInstallerWAD");
            if (dictionary.IniReadValue("menu", "cache") != "")
                cacheToolStripMenuItem.Text = dictionary.IniReadValue("menu", "cache");
            if (dictionary.IniReadValue("menu", "enableCache") != "")
                enableCacheToolStripMenuItem.Text = dictionary.IniReadValue("menu", "enableCache");
            if (dictionary.IniReadValue("menu", "disableCache") != "")
                disableCacheToolStripMenuItem.Text = dictionary.IniReadValue("menu", "disableCache");
            if (dictionary.IniReadValue("menu", "freeCache") != "")
                freeCacheToolStripMenuItem.Text = dictionary.IniReadValue("menu", "freeCache");
            if (dictionary.IniReadValue("menu", "aboutShell") != "")
                aboutShellToolStripMenuItem.Text = dictionary.IniReadValue("menu", "aboutShell");
            if (dictionary.IniReadValue("menu", "hideShell") != "")
                hideShellToolStripMenuItem.Text = dictionary.IniReadValue("menu", "hideShell");
            if (dictionary.IniReadValue("menu", "showShell") != "")
                showShellToolStripMenuItem.Text = dictionary.IniReadValue("menu", "showShell");


            // message

            if (dictionary.IniReadValue("message", "error") != "")
                Dictionary.error = dictionary.IniReadValue("message", "error");
            else
                Dictionary.error = "Error";

            if ((dictionary.IniReadValue("message", "AskAppUpdate1") != "") && (dictionary.IniReadValue("message", "AskAppUpdate2") != "") && (dictionary.IniReadValue("message", "AskAppUpdate3") != ""))
                Dictionary.AskAppUpdate = dictionary.IniReadValue("message", "AskAppUpdate1") + "\r\n" +
                                      dictionary.IniReadValue("message", "AskAppUpdate2") + "\r\n" +
                                      dictionary.IniReadValue("message", "AskAppUpdate3");
            else
                Dictionary.AskAppUpdate = "Application must be updated." + "\r\n" +
                                          "WiiDownloader will restart automatically." + "\r\n" +
                                          "Update now?";

            if ((dictionary.IniReadValue("message", "AskDatabaseUpdate1") != "") && (dictionary.IniReadValue("message", "AskDatabaseUpdate2") != ""))
                Dictionary.AskDatabaseUpdate = dictionary.IniReadValue("message", "AskDatabaseUpdate1") + "\r\n" +
                                           dictionary.IniReadValue("message", "AskDatabaseUpdate2");
            else
                Dictionary.AskDatabaseUpdate = "There's an update for database." + "\r\n" +
                                               "Download it now?";
            
            if (dictionary.IniReadValue("message", "UpdateSkipped") != "")
                Dictionary.UpdateSkipped = dictionary.IniReadValue("message", "UpdateSkipped");
            else
                Dictionary.UpdateSkipped = "Update skipped.";

            if (dictionary.IniReadValue("message", "DatabaseUpdatedTo") != "")
                Dictionary.DatabaseUpdatedTo = dictionary.IniReadValue("message", "DatabaseUpdatedTo");
            else
                Dictionary.DatabaseUpdatedTo = "Database updated to v.";

            if (dictionary.IniReadValue("message", "Extracting") != "")
                Dictionary.Extracting = dictionary.IniReadValue("message", "Extracting");
            else
                Dictionary.Extracting = "Extracting";

            if (dictionary.IniReadValue("message", "ErrorDownloadingFrom") != "")
                Dictionary.ErrorDownloadingFrom = dictionary.IniReadValue("message", "ErrorDownloadingFrom");
            else
                Dictionary.ErrorDownloadingFrom = "Error downloading from:";

            if (dictionary.IniReadValue("message", "Downloading") != "")
                Dictionary.Downloading = dictionary.IniReadValue("message", "Downloading");
            else
                Dictionary.Downloading = "Downloading";

            if (dictionary.IniReadValue("message", "CheckInfoLastVersion") != "")
                Dictionary.CheckInfoLastVersion = dictionary.IniReadValue("message", "CheckInfoLastVersion");
            else
                Dictionary.CheckInfoLastVersion = "Checking info about last version..";

            if (dictionary.IniReadValue("message", "CheckInfoLastDBVersion") != "")
                Dictionary.CheckInfoLastDBVersion = dictionary.IniReadValue("message", "CheckInfoLastDBVersion");
            else
                Dictionary.CheckInfoLastDBVersion = "Checking database version..";

            if (dictionary.IniReadValue("message", "CheckInfoLastAPPVersion") != "")
                Dictionary.CheckInfoLastAPPVersion = dictionary.IniReadValue("message", "CheckInfoLastAPPVersion");
            else
                Dictionary.CheckInfoLastAPPVersion = "Checking application version..";

            if (dictionary.IniReadValue("message", "NoUpdateToDo") != "")
                Dictionary.NoUpdateToDo = dictionary.IniReadValue("message", "NoUpdateToDo");
            else
                Dictionary.NoUpdateToDo = "You have already the last version.";

            if (dictionary.IniReadValue("message", "ErrorOccurred") != "")
                Dictionary.ErrorOccurred = dictionary.IniReadValue("message", "ErrorOccurred");
            else
                Dictionary.ErrorOccurred = "An error has occurred:";

            if (dictionary.IniReadValue("message", "InvalidScript") != "")
                Dictionary.InvalidScript = dictionary.IniReadValue("message", "InvalidScript");
            else
                Dictionary.InvalidScript = "Invalid script: download aborted.";

            if ((dictionary.IniReadValue("message", "NoDevice1") != "") && (dictionary.IniReadValue("message", "NoDevice2") != ""))
                Dictionary.NoDevice = dictionary.IniReadValue("message", "NoDevice1") + "\r\n" +
                                  dictionary.IniReadValue("message", "NoDevice2");
            else
                Dictionary.NoDevice = "Sorry, but no one removable device has been found." + "\r\n" +
                                      "Impossible to do a direct copy.";

            if (dictionary.IniReadValue("message", "AskForDelete") != "")
                Dictionary.AskForDelete = dictionary.IniReadValue("message", "AskForDelete");
            else
                Dictionary.AskForDelete = "Are you sure to delete this script?";

            if (dictionary.IniReadValue("message", "AskForDeleteType") != "")
                Dictionary.AskForDeleteType = dictionary.IniReadValue("message", "AskForDeleteType");
            else
                Dictionary.AskForDeleteType = "For this \"type\" there isn't any more script. Delete it?";

            if (dictionary.IniReadValue("message", "NoDeviceSelected") != "")
                Dictionary.NoDeviceSelected = dictionary.IniReadValue("message", "NoDeviceSelected");
            else
                Dictionary.NoDeviceSelected = "No one device has been selected.";

            if (dictionary.IniReadValue("message", "AskForSeeFiles") != "")
                Dictionary.AskForSeeFiles = dictionary.IniReadValue("message", "AskForSeeFiles");
            else
                Dictionary.AskForSeeFiles = "Do you want to see the files to copy on your device?";

            if ((dictionary.IniReadValue("message", "FolderAlredyExist1") != "") && (dictionary.IniReadValue("message", "FolderAlredyExist2") != "") && (dictionary.IniReadValue("message", "FolderAlredyExist3") != ""))
                Dictionary.AskForMerge = dictionary.IniReadValue("message", "FolderAlredyExist1") + "\r\n" +
                                     dictionary.IniReadValue("message", "FolderAlredyExist2") + "\r\n" +
                                     dictionary.IniReadValue("message", "FolderAlredyExist3");
            else
                Dictionary.AskForMerge = "Folder \"COPY_TO_DEVICE\" already exists." + "\r\n" +
                                         "Merge downloaded files with the existing data in \"COPY_TO_DEVICE\" folder?" + "\r\n" +
                                         "Selecting \"NO\" the folder will be named COPY_TO_DEVICE(#)";

            if (dictionary.IniReadValue("message", "NoDescription") != "")
                Dictionary.NoDescription = dictionary.IniReadValue("message", "NoDescription");
            else
                Dictionary.NoDescription = "...No description is available...";

            if (dictionary.IniReadValue("message", "DownloadComplete") != "")
                Dictionary.DownloadComplete = dictionary.IniReadValue("message", "DownloadComplete");
            else
                Dictionary.DownloadComplete = "Download complete!";

            if (dictionary.IniReadValue("message", "DownloadNotComplete") != "")
                Dictionary.DownloadNotComplete = dictionary.IniReadValue("message", "DownloadNotComplete");
            else
                Dictionary.DownloadNotComplete = "Download NOT completed...";

            if (dictionary.IniReadValue("message", "AskForRepeatDownload") != "")
                Dictionary.AskForRepeatDownload = dictionary.IniReadValue("message", "AskForRepeatDownload");
            else
                Dictionary.AskForRepeatDownload = "Try to restart download?";

            if (dictionary.IniReadValue("message", "AskForAbortDownload") != "")
                Dictionary.AskForAbortDownload = dictionary.IniReadValue("message", "AskForAbortDownload");
            else
                Dictionary.AskForAbortDownload = "Abort also any subsequent download?";

            if (dictionary.IniReadValue("message", "AskForAbortNusDownload") != "")
                Dictionary.AskForAbortNusDownload = dictionary.IniReadValue("message", "AskForAbortNusDownload");
            else
                Dictionary.AskForAbortNusDownload = "Abort also any subsequent download from NUS?";

            if (dictionary.IniReadValue("message", "AskForAbortCiosDownload") != "")
                Dictionary.AskForAbortCiosDownload = dictionary.IniReadValue("message", "AskForAbortCiosDownload");
            else
                Dictionary.AskForAbortCiosDownload = "Abort also any subsequent download?";

            if (dictionary.IniReadValue("message", "AskForRepeatSingleNusDownload") != "")
                Dictionary.AskForRepeatSingleNusDownload = dictionary.IniReadValue("message", "AskForRepeatSingleNusDownload");
            else
                Dictionary.AskForRepeatSingleNusDownload = "Try to restart download from NUS?";

            if (dictionary.IniReadValue("message", "AskForRepeatSingleCiosDownload") != "")
                Dictionary.AskForRepeatSingleCiosDownload = dictionary.IniReadValue("message", "AskForRepeatSingleCiosDownload");
            else
                Dictionary.AskForRepeatSingleCiosDownload = "Try to recreate cIOS?";           
            
            if (dictionary.IniReadValue("message", "DownloadStopped") != "")
                Dictionary.DownloadStopped = dictionary.IniReadValue("message", "DownloadStopped");
            else
                Dictionary.DownloadStopped = "Download stopped by user.";

            if (dictionary.IniReadValue("message", "Patching") != "")
                Dictionary.Patching = dictionary.IniReadValue("message", "Patching");
            else
                Dictionary.Patching = "Patching";

            if (dictionary.IniReadValue("message", "CreatingLink") != "")
                Dictionary.CreatingLink = dictionary.IniReadValue("message", "CreatingLink");
            else
                Dictionary.CreatingLink = "Searching new link for";

            if (dictionary.IniReadValue("message", "NoResponse") != "")
                Dictionary.NoResponse = dictionary.IniReadValue("message", "NoResponse");
            else
                Dictionary.NoResponse = "No response from";

            if (dictionary.IniReadValue("message", "Creating") != "")
                Dictionary.Creating = dictionary.IniReadValue("message", "Creating");
            else
                Dictionary.Creating = "Creating";

            if (dictionary.IniReadValue("message", "Copying") != "")
                Dictionary.Copying = dictionary.IniReadValue("message", "Copying");
            else
                Dictionary.Copying = ".and copying file..";

            if (dictionary.IniReadValue("message", "CreatingWadAndcopy") != "")
                Dictionary.CreatingWadAndcopy = dictionary.IniReadValue("message", "CreatingWadAndcopy");
            else
                Dictionary.CreatingWadAndcopy = "Creating WAD and copying file...";

            if (dictionary.IniReadValue("message", "NoMac") != "")
                Dictionary.NoMac = dictionary.IniReadValue("message", "NoMac");
            else
                Dictionary.NoMac = "Mac Address isn't valid ...";

            if (dictionary.IniReadValue("message", "NoFirmware") != "")
                Dictionary.NoFirmware = dictionary.IniReadValue("message", "NoFirmware");
            else
                Dictionary.NoFirmware = "Please, select System Menu ...";

            if (dictionary.IniReadValue("message", "NoRegion") != "")
                Dictionary.NoRegion = dictionary.IniReadValue("message", "NoRegion");
            else
                Dictionary.NoRegion = "Please, select Region ...";

            if (dictionary.IniReadValue("message", "NoValidFirmware") != "")
                Dictionary.NoValidFirmware = dictionary.IniReadValue("message", "NoValidFirmware");
            else
                Dictionary.NoValidFirmware = "Please, select an existing System Menu for that region ...";

            if (dictionary.IniReadValue("message", "CopyingFile") != "")
                Dictionary.CopyingFile = dictionary.IniReadValue("message", "CopyingFile");
            else
                Dictionary.CopyingFile = "Copying";

            if (dictionary.IniReadValue("message", "ToTheRoot") != "")
                Dictionary.ToTheRoot = dictionary.IniReadValue("message", "ToTheRoot");
            else
                Dictionary.ToTheRoot = "in root";

            if (dictionary.IniReadValue("message", "FileNotFound") != "")
                Dictionary.FileNotFound = dictionary.IniReadValue("message", "FileNotFound");
            else
                Dictionary.FileNotFound = "Impossible to find the file";

            if (dictionary.IniReadValue("message", "WiiInfoNotFound") != "")
                Dictionary.WiiInfoNotFound = dictionary.IniReadValue("message", "WiiInfoNotFound");
            else
                Dictionary.WiiInfoNotFound = "Haven't been put necessary data for use Wilbrand. Insert them by clicking on \"Settings\" -> \"About your Wii.\"";

            if (dictionary.IniReadValue("message", "UsingWilbrand") != "")
                Dictionary.UsingWilbrand = dictionary.IniReadValue("message", "UsingWilbrand");
            else
                Dictionary.UsingWilbrand = "Creating exploit using Wilbrand...";

            if (dictionary.IniReadValue("message", "WilbrandDisclamer") != "")
                Dictionary.WilbrandDisclamer = richTextBoxWiiInfo.Text = dictionary.IniReadValue("message", "WilbrandDisclamer");
            else
                Dictionary.WilbrandDisclamer = richTextBoxWiiInfo.Text = "This information is used only for the use of Wilbrand. No data will be sent to third parties.";

            if (dictionary.IniReadValue("message", "HackScriptDisclaimer") != "")
                Dictionary.HackScriptDisclaimer = dictionary.IniReadValue("message", "HackScriptDisclaimer");
            else
                Dictionary.HackScriptDisclaimer = "This script was created automatically by the program, according to the options set by the user. After you restart the program this script will be deleted. Before starting the download make sure you have no other file.wad in the WAD folder of the SD.";

            if (dictionary.IniReadValue("message", "scriptCreated") != "")
                Dictionary.scriptCreated = dictionary.IniReadValue("message", "scriptCreated");
            else
                Dictionary.scriptCreated = "Ok! Wii informations stored and script created.";

            if (dictionary.IniReadValue("message", "wiiInfoSaves") != "")
                Dictionary.wiiInfoSaves = dictionary.IniReadValue("message", "wiiInfoSaves");
            else
                Dictionary.wiiInfoSaves = "OK! Wii informations stored.";

            if (dictionary.IniReadValue("message", "TooCompareError") != "")
                Dictionary.TooCompareError = dictionary.IniReadValue("message", "TooCompareError");
            else
                Dictionary.TooCompareError = "Download stopped by application: too much error trying to copy the same files... Try to use another device.";

            if (dictionary.IniReadValue("message", "noCheckBox") != "")
                Dictionary.NoCheckBox = dictionary.IniReadValue("message", "noCheckBox");
            else
                Dictionary.NoCheckBox = "...stop playing with the check boxes... =P";

            if (dictionary.IniReadValue("message", "FoundInCache") != "")
                Dictionary.FoundInCache = dictionary.IniReadValue("message", "FoundInCache");
            else
                Dictionary.FoundInCache = "already present in cache. Copying file..";
            
            if (dictionary.IniReadValue("message", "DeleteCacheFiles") != "")
                Dictionary.DeleteCacheFiles = dictionary.IniReadValue("message", "DeleteCacheFiles");
            else
                Dictionary.DeleteCacheFiles = "Are you sure to delete all the files in the cache?";
            if (dictionary.IniReadValue("message", "DeletingCacheFiles") != "")
                Dictionary.DeletingCacheFiles = dictionary.IniReadValue("message", "DeletingCacheFiles");
            else
                Dictionary.DeletingCacheFiles = "Deleting the cache files ...";

            if (dictionary.IniReadValue("message", "FileInUse") != "")
                Dictionary.FileInUse = dictionary.IniReadValue("message", "FileInUse");
            else
                Dictionary.FileInUse = "This file is being used by another program: if is possible, close that process.";

            if (dictionary.IniReadValue("message", "NoNetwork") != "")
                Dictionary.NoNetwork = dictionary.IniReadValue("message", "NoNetwork");
            else
                Dictionary.NoNetwork = "Network connection not available. Do tou want to use WiiDownloader in 'Offline mode' ?";

            if (dictionary.IniReadValue("message", "errorDownloadingWithNoNetwork") != "")                
                Dictionary.errorDownloadingWithNoNetwork = dictionary.IniReadValue("message", "errorDownloadingWithNoNetwork");
            else            
                Dictionary.errorDownloadingWithNoNetwork = "In 'offline mode' isn't possible to do any download.";
            
            if (dictionary.IniReadValue("message", "NoUpadteError") != "")
                Dictionary.NoUpdateError = dictionary.IniReadValue("message", "NoUpadteError");
            else
                Dictionary.NoUpdateError = "WiiDownloader will started, but probably there are some problems on code.google server, and I can't do anything for resolve this. Eventually updates of WiiDownloader will checked on next startup.";
            
            if (dictionary.IniReadValue("message", "NoFreeSpace1") != "")
                Dictionary.NoFreeSpace1 = dictionary.IniReadValue("message", "NoFreeSpace1");
            else            
                Dictionary.NoFreeSpace1 = "In the device \"";

            if (dictionary.IniReadValue("message", "NoFreeSpace2") != "")
                Dictionary.NoFreeSpace2 = dictionary.IniReadValue("message", "NoFreeSpace2");
            else            
                Dictionary.NoFreeSpace2 = "\" there isn't enough space available.";

            if (dictionary.IniReadValue("message", "GuideCreated") != "")
                Dictionary.GuideCreated = dictionary.IniReadValue("message", "GuideCreated");
            else
                Dictionary.GuideCreated = "Tutorial created: it will show on your browser; if that not happen, open the file 'tutorial.html' (that is in WiiDownloader folder)";
            
            if (dictionary.IniReadValue("message", "WhyMACaddress") != "")            
                Dictionary.WhyMACaddress = dictionary.IniReadValue("message", "WhyMACaddress");            
            else            
                Dictionary.WhyMACaddress = "Why MAC address?";            
            linkLabelWilbrand.Text = Dictionary.WhyMACaddress;

            if (dictionary.IniReadValue("message", "WhyWilbrand") != "")
                Dictionary.WhyWilbrand = dictionary.IniReadValue("message", "WhyWilbrand");
            else
                Dictionary.WhyWilbrand = "You need to put MAC address (that is necessary for use Wilbrand exploit), only if you HAVEN'T Homebrew Channel already installed on your Wii.";

            if (dictionary.IniReadValue("message", "HomebrewChannel") != "")
                Dictionary.HomebrewChannel = dictionary.IniReadValue("message", "HomebrewChannel");
            else
                Dictionary.HomebrewChannel = "Just for information, the Homebrew Channel is this one:";

            _tabControl.TabPages.Remove(_tabPage2);
            _tabControl.TabPages.Remove(_tabPage3);
            _tabControl.TabPages.Remove(_tabPage4);
        }


        private bool CreateBatchFileForUpdate()
        {
            string lines;
            
            string sleep = "database\\tools\\sleep.exe";
            string program = "WiiDownloader.exe";
            string new_program = "NewWiiDownloader.exe";

            FileDelete(CombinePath(STARTUP_PATH, "WiiDownloaderUpdater.bat"));            
             
            lines = "@echo off\r\n" +
                    "cls\r\n" +
                    "if EXIST " + program + " goto check1_ok\r\n" +
                    "echo.\r\n" +
                    "echo " + program + " not found.\r\n" +
                    "echo.\r\n" +
                    "echo WiiDownloaderUpdater.bat can't do anything and will be close.\r\n" +
                    "echo.\r\n" +
                    "pause\r\n" +
                    "goto end_of_batch\r\n" +
                    ":check1_ok\r\n" +
                    "if EXIST " + new_program + " goto check2_ok\r\n" +
                    "echo.\r\n" +
                    "echo " + new_program + " not found.\r\n" +
                    "echo.\r\n" +
                    "echo WiiDownloaderUpdater.bat can't do anything and will be close.\r\n" +
                    "echo.\r\n" +
                    "pause\r\n" +
                    "goto end_of_batch\r\n" +
                    ":check2_ok\r\n" +
                    "cls\r\n" +                    
                    "echo.\r\n" +
                    "echo ----------- Restaring WiiDownloader ---------\r\n" +
                    "echo.\r\n" +
                    "echo|set /p=Please wait a few seconds...\r\n" +
                    "echo|set /p=.\r\n" +
                    "if exist " + sleep + " " + sleep + " 1\r\n" +
                    "echo|set /p=.\r\n" +
                    "if exist " + sleep + " " + sleep + " 1\r\n" +
                    "echo|set /p=.\r\n" +
                    "if exist " + sleep + " " + sleep + " 1\r\n" +
                    "echo|set /p=.\r\n" +
                    "del " + program + " /q\r\n" +
                    "ren " + new_program + " " + program + "\r\n" +
                    "if exist " + sleep + " " + sleep + " 1\r\n" +
                    "echo|set /p=.\r\n" +
                    ":close_batch\r\n" +
                    "if EXIST " + program + " start " + program + "\r\n";            

            System.IO.StreamWriter WiiDownloaderUpdaterFile = new System.IO.StreamWriter(CombinePath(STARTUP_PATH, "WiiDownloaderUpdater.bat"));
            WiiDownloaderUpdaterFile.WriteLine(lines);
            WiiDownloaderUpdaterFile.Close();

            if (File.Exists(CombinePath(STARTUP_PATH, "WiiDownloaderUpdater.bat")))
                return true;
            else
                return false;
            
        }      

        private bool startAppUpdater()
        {           
            if (appAutomaticToolStrip.Checked != true)
            {
                if (WiiDownloaderWaitForm.IsHandleCreated)
                    WiiDownloaderWaitForm.Close();

                DialogResult ans = MessageBox.Show(Dictionary.AskAppUpdate,
                             this.Text + " - Info",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (ans == DialogResult.No)
                {
                    MessageBox.Show(Dictionary.UpdateSkipped,
                              this.Text + " - Info",
                             MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
            }
            
            FileDelete(CombinePath(STARTUP_PATH, "NewWiiDownloader.exe"));
            if (!DoDownloadForApplication("NewWiiDownloader.zip",
                             wiiDownloaderFilesLink + "WiiDownloader%20v" + FEATURED_APPLICATION_VERSION + ".zip",
                             TOOLS_PATH,
                             true))
                return false;

            DOWNLOAD_OR_PROGRAM_WORKING = true;

            if (!unZip("NewWiiDownloader.zip",
                               CombinePath(TOOLS_PATH, "NewWiiDownloader.zip"),
                               TOOLS_PATH, true))
            {
                FileDelete(CombinePath(TOOLS_PATH, "NewWiiDownloader.zip"));
                DOWNLOAD_OR_PROGRAM_WORKING = false;
                return false;
            }
            DOWNLOAD_OR_PROGRAM_WORKING = false;

            FileMove(CombinePath(TOOLS_PATH, "WiiDownloader", "WiiDownloader.exe"), CombinePath(STARTUP_PATH, "NewWiiDownloader.exe"), true);
            FileDelete(CombinePath(TOOLS_PATH, "NewWiiDownloader.zip"));
            DeleteFolder(CombinePath(TOOLS_PATH, "WiiDownloader"), true);            

            if(!CreateBatchFileForUpdate())
                return false;

            if (WiiDownloaderWaitForm.IsHandleCreated)
                WiiDownloaderWaitForm.Close();
            
            ProcessStartInfo p_updater;
            Process UpdateProcess;
            
            p_updater = new ProcessStartInfo("cmd.exe", "/c \"" + CombinePath(STARTUP_PATH, "WiiDownloaderUpdater.bat") + "\"");
            p_updater.UseShellExecute = true;
            p_updater.RedirectStandardOutput = false;
            p_updater.RedirectStandardInput = false;
            p_updater.RedirectStandardError = false;
            p_updater.CreateNoWindow = false;
            p_updater.WorkingDirectory = STARTUP_PATH;
            p_updater.WindowStyle = ProcessWindowStyle.Normal;   // qui la finestra di DOS DEVO vederla ^__^    
            
            UpdateProcess = Process.Start(p_updater);           
                             
            Environment.Exit(0);  
       
            return true;
        }

        private bool startModMiiUpdater()        
        {
            DOWNLOAD_OR_PROGRAM_WORKING = true;

            AppendText("Updating ModMii to v" + FEATURED_MODMII_VERSION + "\n");

            if (!Directory.Exists(MOD_MII_PATH))
                Directory.CreateDirectory(MOD_MII_PATH);
            
            FileDelete(CombinePath(MOD_MII_PATH, "ModMii.zip"));            

            if (!DoDownloadForApplication("ModMii.zip",
                              "http://modmii.googlecode.com/files/ModMii" + FEATURED_MODMII_VERSION + ".zip",
                              MOD_MII_PATH,
                              false))                           
                return false;   

            if (!File.Exists(CombinePath(MOD_MII_PATH, "ModMii.zip")))
                return false;   

            if (!unZip("ModMii.zip",
                                CombinePath(MOD_MII_PATH, "ModMii.zip"),
                                MOD_MII_PATH, true))
                return false;

            DOWNLOAD_OR_PROGRAM_WORKING = true;

            FileDelete(CombinePath(MOD_MII_PATH, "ModMii.zip"));

            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("ModMii", "version", FEATURED_MODMII_VERSION);
            
            FileDelete(CombinePath(MOD_MII_PATH, "Support", "settings.bat"));

            // Create a new config files
            string lines =  "::ModMii Settings " + "\r\n" +
                            "::ModMiiv" + FEATURED_MODMII_VERSION + "\r\n" +
                            "Set AudioOption=OFF";

            System.IO.StreamWriter file = new System.IO.StreamWriter(CombinePath(MOD_MII_PATH, "Support", "settings.bat"));
            file.WriteLine(lines);
            file.Close();


            return true;

        }

        private bool startDatabaseUpdater()
        {
            if (databaseAutomaticToolStrip.Checked != true)
            {
                if (WiiDownloaderWaitForm.IsHandleCreated)
                    WiiDownloaderWaitForm.Close();

                DialogResult ans = MessageBox.Show(Dictionary.AskDatabaseUpdate,
                             this.Text + " - Info",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (ans == DialogResult.No)
                {
                    MessageBox.Show(Dictionary.UpdateSkipped,
                              this.Text + " - Info",
                             MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
            }

            Bitmap blanKimage = new Bitmap(160, 60);
            Graphics flagGraphics = Graphics.FromImage(blanKimage);
            flagGraphics.FillRectangle(Brushes.White, 0, 0, 160, 60);
            pictureBoxIcon.Image = blanKimage;

            DOWNLOAD_OR_PROGRAM_WORKING = true;
            cleanTempFolder();
            AppendText("\n");
            
            if (!DoDownloadForApplication("WiiDownloaderDatabase.zip",
                              wiiDownloaderFilesLink + "WiiDownloaderDatabase%20v" + FEATURED_DATABASE_VERSION + ".zip",
                              DATABASE_PATH,
                              true))
                return false;

            if (!File.Exists(CombinePath(DATABASE_PATH, "WiiDownloaderDatabase.zip")))
                return false;  

            EnableSearch = false;
            comboBoxType.DataSource = null;
            comboBoxScriptName.DataSource = null;
            EnableSearch = true;
            
            DeleteFolder(STANDARD_SCRIPT_PATH, false);
            DeleteFolder(TUTORIAL_PATH, false);
            DeleteFolder(TUTORIAL_IMAGES_PATH, false);            
            DeleteFolder(LANGUAGES_PATH, false);
            FileDelete(CombinePath(NUS_PATH, "nusDatabase.ini"));

            DOWNLOAD_OR_PROGRAM_WORKING = true;

            if (WiiDownloaderWaitForm.IsHandleCreated)
                WiiDownloaderWaitForm.labelFirstTime.Text = "...extracting " + "WiiDownloaderDatabase.zip";

            if (!unZip("WiiDownloaderDatabase.zip",
                                CombinePath(DATABASE_PATH, "WiiDownloaderDatabase.zip"),
                                DATABASE_PATH, true))            
                return false;

            if (WiiDownloaderWaitForm.IsHandleCreated)
                WiiDownloaderWaitForm.labelFirstTime.Text = "...OK !";

            DOWNLOAD_OR_PROGRAM_WORKING = false;

            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("version", "database", FEATURED_DATABASE_VERSION);
            
            linkLabelVersion.Text = "WiiDownloader v." + WiiDownloader_version + " (database v." + FEATURED_DATABASE_VERSION + ")";
            
            FileDelete(CombinePath(DATABASE_PATH, "WiiDownloaderDatabase.zip"));

            createDictionary();

            if (WiiDownloaderWaitForm.IsHandleCreated)
                WiiDownloaderWaitForm.Close();

            comboBoxInit();           

            MessageBox.Show(Dictionary.DatabaseUpdatedTo + FEATURED_DATABASE_VERSION,
                              this.Text + " - Info",
                             MessageBoxButtons.OK, MessageBoxIcon.Information);

            return true;

        }


        private void readSettingsFile()
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);

            string applicationUpdateMode = ini.IniReadValue("AutomaticUpdate", "application");
            string databaseUpdateMode = ini.IniReadValue("AutomaticUpdate", "database");
            string Language = ini.IniReadValue("Language", "LanguageChoice");
            string standardDatabaseVersion = ini.IniReadValue("version", "database");       
           
            linkLabelVersion.Text = "WiiDownloader v." + WiiDownloader_version + " (database v." + standardDatabaseVersion + ")";

            switch (applicationUpdateMode)
            {
                case "False":
                    appAutomaticToolStrip.Checked = false;
                    appAskToolStrip.Checked = false;
                    appManualToolStrip.Checked = true;
                    break;
                case "AskBefore":
                    appAutomaticToolStrip.Checked = false;
                    appAskToolStrip.Checked = true;
                    appManualToolStrip.Checked = false;
                    break;
                case "True":
                default:
                    appAutomaticToolStrip.Checked = true;
                    appAskToolStrip.Checked = false;
                    appManualToolStrip.Checked = false;
                    break;
            }
            switch (databaseUpdateMode)
            {
                case "False":
                    databaseAutomaticToolStrip.Checked = false;
                    databaseAskToolStrip.Checked = false;
                    databaseManualToolStrip.Checked = true;
                    break;
                case "AskBefore":
                    databaseAutomaticToolStrip.Checked = false;
                    databaseAskToolStrip.Checked = true;
                    databaseManualToolStrip.Checked = false;
                    break;
                case "True":
                default:
                    databaseAutomaticToolStrip.Checked = true;
                    databaseAskToolStrip.Checked = false;
                    databaseManualToolStrip.Checked = false;
                    break;
            }

            if (ini.IniReadValue("cache", "enableCache") == "False")
            {
                enableCacheToolStripMenuItem.Checked = false;
                disableCacheToolStripMenuItem.Checked = true;
                CACHE_ENABLED = false;
            }
            else
            {
                enableCacheToolStripMenuItem.Checked = true;
                disableCacheToolStripMenuItem.Checked = false;
                if (ini.IniReadValue("cache", "enableCache").Trim() == "")                     
                    ini.IniWriteValue("cache", "enableCache", "True");
                CACHE_ENABLED = true;
            }

            if (ini.IniReadValue("options", "show_process") == "True")
            {
                hideShellToolStripMenuItem.Checked = false;
                showShellToolStripMenuItem.Checked = true;
                SHOW_PROCESS = true;
            }
            else
            {
                hideShellToolStripMenuItem.Checked = true;
                showShellToolStripMenuItem.Checked = false;
                if (ini.IniReadValue("options", "show_process").Trim() == "")
                    ini.IniWriteValue("options", "show_process", "False");
                SHOW_PROCESS = false;
            }


            
        }

        private void createSettingsFile()
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);

            switch (System.Globalization.RegionInfo.CurrentRegion.ThreeLetterISORegionName)
            {                
                case "ITA":
                    ini.IniWriteValue("Language", "LanguageChoice", "italiano");
                    break;
                case "ESP":
                case "ARG":
                case "BOL":
                case "CHL":
                case "COL":
                case "CRI":
                case "CUB":
                case "ECU":
                case "SLV":
                case "GTM":
                case "HND":
                case "MEX":
                case "NIC":
                case "PAN":
                case "PRY":
                case "PER":
                case "PRI":
                case "URY":
                case "VEN":                
                    ini.IniWriteValue("Language", "LanguageChoice", "español");
                    break;
                case "FRA":
                case "FXX":
                case "GUF":
                case "PYF":
                case "ATF":                
                case "MCO":
                case "LUX":
                case "HTI":
                case "VUT":
                case "MRT":
                case "MLI":
                case "NER":
                case "TCD":
                case "BFA":
                case "SEN":
                case "BEN":
                case "CIV":
                case "GIN":
                case "TGO":
                case "RWA":
                case "BDI":
                case "CMR":
                case "GAB":                
                case "COM":
                case "MDG":
                case "MUS":
                case "SYC":
                case "DZA":
                case "MAR":
                case "TUN":
                case "AND":
                case "VNM":
                case "KHM":
                case "LAO":
                    ini.IniWriteValue("Language", "LanguageChoice", "french");
                    break;
                case "AGO":
                case "BRA":
                case "PRT":
                case "CPV":
                case "MOZ":
                case "GNB":
                case "STP":
                case "MAC":                
                    ini.IniWriteValue("Language", "LanguageChoice", "português");
                    break;
                case "CHN":
                case "TWN":
                case "SGP":
                    ini.IniWriteValue("Language", "LanguageChoice", "chinese");
                    break;                
                case "NLD":
                case "BEL":
                case "SUR":
                case "ABW":                               
                    ini.IniWriteValue("Language", "LanguageChoice", "dutch");
                    break;                   
                default:
                    ini.IniWriteValue("Language", "LanguageChoice", "english");
                    break;
            }
            ini.IniWriteValue("AutomaticUpdate", "application", "AskBefore");
            ini.IniWriteValue("AutomaticUpdate", "database", "True");
            ini.IniWriteValue("version", "database", "0");            
            ini.IniWriteValue("AboutMerge", "Checked", "MergeAlways");
            ini.IniWriteValue("cache", "enableCache", "True");
            ini.IniWriteValue("options", "show_process", "False");

            linkLabelVersion.Text = "WiiDownloader v." + WiiDownloader_version + " (database v.0)";
            
        }       

        private bool callCheckSettings(string mode)
        {
            bool result;
            CursorInDefaultState(false);
            ButtonValueForDownload(false);
            result = checkSettings(mode);
            CursorInDefaultState(true);
            ButtonValueForDownload(true);
            return result;
        }

        private bool checkSettings(string mode)
        {
            readSettingsFile();

            if (appManualToolStrip.Checked == true && databaseManualToolStrip.Checked == true && mode == "Startup")
                return true;

            if (mode == "Startup" && !NetworkOk)
                return true;            

            infoTextBox.Clear();
            AppendText(Dictionary.CheckInfoLastVersion + "\n");

            if (databaseManualToolStrip.Checked == false || mode == "DatabaseUpdate")
            {
                if (mode != "AppUpdate")
                {
                    if (!checkUpdateDatabase())
                        return false;
                }
            }

            if (appManualToolStrip.Checked == false || mode == "AppUpdate")
            {
                if (mode != "DatabaseUpdate")
                {
                    if (!checkUpdateApp())
                        return false;
                }
            }

            return true;
        }

        private bool checkUpdateDatabase()
        {
            IniFile settingsFile = new IniFile(SETTINGS_INI_FILE); 

            AppendText(Dictionary.CheckInfoLastDBVersion + "..OK!\n");

            if (settingsFile.IniReadValue("version", "database") != FEATURED_DATABASE_VERSION)
            {
                if (!startDatabaseUpdater())
                    return false;
            }
            else
                AppendText(Dictionary.NoUpdateToDo + "\n");

            return true;
        }

        private bool checkUpdateApp()
        {           
            AppendText(Dictionary.CheckInfoLastAPPVersion + "..OK!\n");

            if (WiiDownloader_version != FEATURED_APPLICATION_VERSION)
            {
                if (!startAppUpdater())
                {
                    if (WiiDownloaderWaitForm.IsHandleCreated)
                        WiiDownloaderWaitForm.Close();
                    return false;
                }
            }
            else
                AppendText(Dictionary.NoUpdateToDo + "\n");

            return true;
        }

       // ButtonValueForStopping

        private void ButtonValueForDownload(bool value)
        {
            if (radioButtonStandard.Checked)
            {
                buttonNew.Enabled = false;
                buttonDelete.Enabled = false;
            }
            else
            {
                buttonNew.Enabled = value;
                buttonDelete.Enabled = value;
            }
            buttonEdit.Enabled = value;
            menuStrip1.Enabled = value;
            radioButtonStandard.Enabled = value;
            radioButtonCustom.Enabled = value;
            comboBoxType.Enabled = value;
            comboBoxScriptName.Enabled = value;
            checkBoxCopyToSD.Enabled = value;
            if (checkBoxCopyToSD.Checked == true && checkBoxCopyToSD.Enabled == true)
                comboBoxDevice.Enabled = true;
            else
                comboBoxDevice.Enabled = false;

            IntPtr hSystemMenu = GetSystemMenu(this.Handle, false);
            if (value)
                EnableMenuItem(hSystemMenu, SC_CLOSE, MF_ENABLED);
            else
                EnableMenuItem(hSystemMenu, SC_CLOSE, MF_GRAYED);
      
            this.Update();
        }

        private void EnableButton(bool value)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            Global.LanguageChoice = ini.IniReadValue("Language", "LanguageChoice");

            IniFile dictionary = new IniFile(CombinePath(LANGUAGES_PATH, Global.LanguageChoice + ".ini"));

            if (checkBoxCopyToSD.Checked == true)
                comboBoxDevice.Enabled = value;
            else
                comboBoxDevice.Enabled = false;
       
            if (comboBoxType.Text == "")
                comboBoxType.Enabled = value;
            else
                comboBoxType.Enabled = true;
            comboBoxScriptName.Enabled = value;

            if (radioButtonStandard.Checked == true)
            {
                buttonNew.Enabled = false;
                buttonDelete.Enabled = false;
                buttonEdit.Enabled = value;
                if(dictionary.IniReadValue("menu", "buttonView") != "")
                    buttonEdit.Text = Dictionary.button_edit = dictionary.IniReadValue("menu", "buttonView");
                else
                    buttonEdit.Text = Dictionary.button_edit = "View";
            }
            else
            {
                buttonNew.Enabled = true;
                buttonDelete.Enabled = value;
                buttonEdit.Enabled = value;
                if(dictionary.IniReadValue("menu", "buttonEdit") != "")
                    buttonEdit.Text = Dictionary.button_edit = dictionary.IniReadValue("menu", "buttonEdit");
                else
                    buttonEdit.Text = Dictionary.button_edit = "Edit";

            }
            if (value == false)
                LoadImageAndInfo();


        }        


        private bool comboBoxInit()
        {
            DirectoryInfo dir;
            bool scriptFound = false;

            if ((Global.editorMode == "View" || Global.editorMode == "Edit"))
                return true; // there isn't mod to name or type

            dir = new DirectoryInfo(CombinePath(SCRIPT_PATH, Global.group));

            EnableSearch = false;
            comboBoxType.DataSource = dir.GetDirectories();
            EnableSearch = true;

            if (comboBoxType.Items.Count > 0)
            {
                dir = new DirectoryInfo(CombinePath(SCRIPT_PATH, Global.group, comboBoxType.Text));

                EnableSearch = false;
                comboBoxScriptName.DataSource = dir.GetFiles();
                EnableSearch = true;

                if (comboBoxScriptName.Items.Count > 0)
                    scriptFound = true;
            }

            LoadImageAndInfo();

            return scriptFound;
        }

        private bool comboBoxReload()
        {
            DirectoryInfo dir;
            bool scriptFound = false;

            if ((Global.editorMode == "View" || Global.editorMode == "Edit"))
                return true; // there isn't mod to name or type

            dir = new DirectoryInfo(CombinePath(SCRIPT_PATH, Global.group));

            EnableSearch = false;
            comboBoxType.DataSource = dir.GetDirectories();
            comboBoxType.Text = WiiDownloader.ScriptEdit.Global.type;
            EnableSearch = true;

            if (comboBoxType.Items.Count > 0)
            {
                dir = new DirectoryInfo(CombinePath(SCRIPT_PATH, Global.group, comboBoxType.Text));

                EnableSearch = false;
                comboBoxScriptName.DataSource = dir.GetFiles();
                comboBoxScriptName.Text = WiiDownloader.ScriptEdit.Global.name;
                EnableSearch = true;

                if (comboBoxScriptName.Items.Count > 0)
                    scriptFound = true;
            }

            LoadImageAndInfo();

            return scriptFound;
        }


        private bool comboBoxGroupUpdate()
        {
            DirectoryInfo dir;
            bool scriptFound = false;

            if (Global.group == null)
                return false;

            dir = new DirectoryInfo(CombinePath(SCRIPT_PATH, Global.group));

            EnableSearch = false;
            comboBoxType.DataSource = dir.GetDirectories();
            EnableSearch = true;

            if (comboBoxType.Items.Count > 0)
            {
                dir = new DirectoryInfo(CombinePath(SCRIPT_PATH, Global.group, comboBoxType.Text));

                EnableSearch = false;
                comboBoxScriptName.DataSource = dir.GetFiles();
                EnableSearch = true;

                if (comboBoxScriptName.Items.Count > 0)
                    scriptFound = true;
            }
            else
            {
                EnableSearch = false;
                comboBoxScriptName.DataSource = null;
                EnableSearch = true;
            }

            LoadImageAndInfo();

            return scriptFound;
        }

        private bool comboBoxTypeUpdate()
        {
            DirectoryInfo dir;
            bool scriptFound = false;

            dir = new DirectoryInfo(CombinePath(SCRIPT_PATH, Global.group, comboBoxType.Text));

            EnableSearch = false;
            comboBoxScriptName.DataSource = dir.GetFiles();
            EnableSearch = true;

            if (comboBoxScriptName.Items.Count > 0)
                scriptFound = true;

            LoadImageAndInfo();

            return scriptFound;
        }

        
        private bool DownloadImage(string imageFile)
        {
            this.Enabled = false;
            infoTextBox.Clear();
            if (DoDownloadForApplication(imageFile,
                             imageFolderLink + imageFile,
                             IMAGES_PATH,
                             true))
            {
                this.Enabled = true;
                return true;
            }
            else
            {
                this.Enabled = true;
                return false;
            }
                               
        }      


        private void LoadImageAndInfo()
        {
            IniFile script;
            script = new IniFile(CombinePath(SCRIPT_PATH, Global.group, comboBoxType.Text, comboBoxScriptName.Text));

            labelDownloadToDo.Text = "";

            string imageFile = "";
            string source = script.IniReadValue("info", "source");
            switch (source)
            {
                case "Download from URL":
                    imageFile = script.IniReadValue("info", "imageFileLink");
                    break;
                case "Other features":
                    if (script.IniReadValue("info", "OtherFeatures") == "useWilbrand")
                        imageFile = "wilbrand.png";
                    break;
                case "Combine existing script":
                    imageFile = script.IniReadValue("info", "imageFileCombine");
                    break;
                default:
                    break;
            }

            if (imageFile.Trim() == "")
                imageFile = "no_images.png";
            else if ((radioButtonCustom.Checked == true) && !File.Exists(CombinePath(IMAGES_PATH, imageFile)))
                imageFile = "no_images.png";

            if (File.Exists(CombinePath(IMAGES_PATH, imageFile)))
                pictureBoxIcon.Image = new Bitmap(CombinePath(IMAGES_PATH, imageFile));
            else
            {
                DownloadImage(imageFile);
                if(File.Exists(CombinePath(IMAGES_PATH, imageFile)))            
                    pictureBoxIcon.Image = new Bitmap(CombinePath(IMAGES_PATH, imageFile));               
                else
                {
                    if (!File.Exists(CombinePath(IMAGES_PATH, "no_images.png")))
                    {
                        if (!DownloadImage("no_images.png"))
                        {
                            // Not possible to go here..ma for be sure a blank image..
                            Bitmap blanKimage = new Bitmap(160, 60);
                            Graphics flagGraphics = Graphics.FromImage(blanKimage);
                            flagGraphics.FillRectangle(Brushes.White, 0, 0, 160, 60);
                            pictureBoxIcon.Image = blanKimage;
                        }
                        else
                        {
                            pictureBoxIcon.Image = new Bitmap(CombinePath(IMAGES_PATH, "no_images.png"));
                            script.IniWriteValue("info", "imageFileLink", "");
                            script.IniWriteValue("info", "imageFileCombine", "");
                        }
                    }
                    else
                    {
                        pictureBoxIcon.Image = new Bitmap(CombinePath(IMAGES_PATH, "no_images.png"));
                        script.IniWriteValue("info", "imageFileLink", "");
                        script.IniWriteValue("info", "imageFileCombine", "");
                    }
                }
            }


            string scriptString = script.IniReadValue("info", "officialSite");
            if (scriptString == "")
                linkLabelOfficialSite.Enabled = false;
            else
                linkLabelOfficialSite.Enabled = true;
           
            infoTextBox.Text = script.IniReadValue("info", "description." + Global.LanguageChoice) + "\n";
            if (infoTextBox.Text.Trim() == "")
                infoTextBox.Text = script.IniReadValue("info", "description.english") + "\n";
            if (infoTextBox.Text.Trim() == "")
                infoTextBox.Text = Dictionary.NoDescription + "\n";           
        }

        private void AppendText(string message)
        {
            infoTextBox.AppendText(message);
            infoTextBox.SelectionStart = infoTextBox.Text.Length;
            infoTextBox.ScrollToCaret();            
        }

        private bool ScriptCheck(string value)
        {
            infoTextBox.Text = "";
            if (value != "")
                return true;

            AppendText(Dictionary.InvalidScript + "\n");
            return false;
        }

        private bool DoDownload(string filename, string filePath, string link)
        {
            if (!executeDownload(
                                Dictionary.Downloading + " " + filename + "...",
                                link,
                                filePath,
                                Dictionary.ErrorDownloadingFrom + " '" + link + "'" + " : "))
                return false;
            return true;
        }

        private bool DoDownloadForApplication(string filename, string link, string newfolder, bool changeDownloadValue )
        {
            if(changeDownloadValue)
                DOWNLOAD_OR_PROGRAM_WORKING = true;
            cleanTempFolder();

            if (WiiDownloaderWaitForm.IsHandleCreated && filename != "lastVersion.txt")
                WiiDownloaderWaitForm.labelFirstTime.Text = "...downloading " + filename;

            if (!executeDownload(
                                Dictionary.Downloading + " " + filename + "...",
                                link,
                                CombinePath(DOWNLOAD_PATH, filename),
                                Dictionary.ErrorDownloadingFrom + " '" + link + "'" + " : "))
            {
                if (changeDownloadValue)
                    DOWNLOAD_OR_PROGRAM_WORKING = false;
                return false;
            }
            else
            {                
                FileDelete(CombinePath(newfolder, filename));
                if (!File.Exists(CombinePath(newfolder, filename)))
                    FileMove(CombinePath(DOWNLOAD_PATH, filename), CombinePath(newfolder, filename), true);
                if (changeDownloadValue)
                    DOWNLOAD_OR_PROGRAM_WORKING = false;
                return true;
            }
        }

        static bool FileDelete(string file_to_delete)
        {
            bool fisrtTry = true;
        retry_to_delete:   
            if (!File.Exists(file_to_delete))
                return true;

            if (!FileInUse(file_to_delete))
            {
                File.SetAttributes(file_to_delete, FileAttributes.Normal);
                File.Delete(file_to_delete);
                return true;
            }
            else
            {
                if (fisrtTry)
                {
                    Thread.Sleep(2000);
                    fisrtTry = false;
                    goto retry_to_delete;
                }
                
                string errorMsg;
                if(Dictionary.FileInUse == "")
                    errorMsg = "This file is being used by another program: if is possible, close that process.";
                else
                    errorMsg = Dictionary.FileInUse;           
           
                DialogResult myDialogResult;
                myDialogResult = MessageBox.Show(file_to_delete + "\n\r\n\r" + errorMsg, "WiiDownloader - Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
    
                if (myDialogResult == DialogResult.Retry)
                    goto retry_to_delete;
                else
                    return false;
            }
        }

        static bool FileMove(string file_orig, string file_dest, bool deleteAfterCopy)
        {
        retry_to_copy:            
            if(!FileDelete(file_dest))
                return false;
            if (!File.Exists(file_orig))
                return false;

            File.Copy(file_orig, file_dest);

            if (!FileCompare(file_orig, file_dest))
            {
                if (AddFileCompareError())
                    goto retry_to_copy;
                else
                    return false;
            }
            else
                fileCompareError = 0;

            if (deleteAfterCopy == true)
                FileDelete(file_orig);

            return true;
        }

        private void cleanTempFolder()
        {
            DeleteFolder(DOWNLOAD_PATH, false);
            DeleteFolder(EXTRACTED_FILES_PATH, false);

            if (!Directory.Exists(TEMP_PATH))
                Directory.CreateDirectory(TEMP_PATH);
            if (!Directory.Exists(DOWNLOAD_PATH))
                Directory.CreateDirectory(DOWNLOAD_PATH);
            if (!Directory.Exists(EXTRACTED_FILES_PATH))
                Directory.CreateDirectory(EXTRACTED_FILES_PATH);
        }


        public void ProgressBarForDirectLink(Object myObject, EventArgs myEventArgs)
        {
            if (timerWorking)
                return;

            timerWorking = true;
        
            if (DOWNLOAD_OR_PROGRAM_WORKING == true && timeOut == false)
            {
                long dimensione = 0;

                foreach (string s in System.IO.Directory.GetFiles(DOWNLOAD_PATH))
                {
                    System.IO.FileInfo f = new System.IO.FileInfo(s);
                    dimensione += f.Length;
                }

                if (fileJustDownloaded == dimensione)
                    passageFreezed++;
                else
                {
                    passageFreezed = 0;
                    fileJustDownloaded = dimensione;
                }


                if (passageFreezed * TYMER_INTERVAL > MAX_DELAY_FOR_TYMER)
                {   
                    timeOut = true;
                    return;
                }

                if (dimensione > 0 && fileSize != 0)
                {
                    long longPercent = (dimensione * 100) / fileSize;
                    int percent = unchecked((int)longPercent);

                    if (percent < 101 && percent > 0)
                    {
                        progressBarDownload.Value = percent;

                        labelProgressBar1.Text = (dimensione / 1024).ToString("0,0", System.Globalization.CultureInfo.CreateSpecificCulture("el-GR")) + " KB / " + (fileSize / 1024).ToString("0,0", System.Globalization.CultureInfo.CreateSpecificCulture("el-GR")) + " KB";
                        labelProgressBar2.Text = percent + "%";

                        if (progressBarDownload.Value == 100)  
                            passageFreezed = 0;                        
                    }
                }
            }
            timerWorking = false;
            
        }

        private string getKeyLink(string stringToParse, string parsingKeyString, char finalChar)
        {
            int i;
            int index = stringToParse.LastIndexOf(parsingKeyString);
            string tempString = "";

            for (i = index + parsingKeyString.Length; i < stringToParse.Length; i++)
            {
                if(stringToParse[i] == ' ')
                    break;
                if (stringToParse[i] == finalChar)
                    break;
                tempString = tempString + stringToParse[i];
            }


            return tempString;
        }

        
        private string CreateActualLink(string ulrToParse, string downloadFrom)
        {
            string downloadUrl = "", parsingKeyString = "", stringToAdd = "";
            char finalChar = '"';

            int timer;

            switch (downloadFrom)
            {
                case "mediafire":
                    parsingKeyString = "kNO = \"";
                    finalChar = '"';                    
                    stringToAdd = "";
                    break;
                case "bootmii.org":
                    if (comboBoxScriptName.Text.Contains(hackmii_installer_old_version))
                    {
                        parsingKeyString = "hackmii_installer_v" + hackmii_installer_old_version + ".zip&amp;key=";
                        stringToAdd = "http://bootmii.org/get.php?file=hackmii_installer_v" + hackmii_installer_old_version + ".zip&key=";
                    }
                    else
                    {
                        parsingKeyString = "hackmii_installer_v" + hackmii_installer_version + ".zip&amp;key=";
                        stringToAdd = "http://bootmii.org/get.php?file=hackmii_installer_v" + hackmii_installer_version + ".zip&key=";
                    }

                    finalChar = '"';                    

                    break;
                default:
                    return "";
            }
        
            string program = "\"" + CombinePath(TOOLS_PATH, "wget.exe") + "\"";           
          
            Directory.CreateDirectory(WIIDOWNLOADER_TEMP_FOLDER);
            
            if (!executeCommand( "",
                                 TOOLS_PATH,
                                 program,
                                 "wget",
                                 "--output-document=" + CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "wiidownloaderTempLink.txt") + " --cookies=off " + ulrToParse,
                                 "\n" + WiiDownloader.WiiDownloader_Form.Dictionary.error + " using searchActualLink.bat" + "..."))
                return "";

            timer = 0;
            while (!File.Exists(CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "wiidownloaderTempLink.txt")))
            {
                timer++;
                if (timer > 20)
                    return "";

                MySleep(200);
                continue;
            }            

            Thread.Sleep(0);

            StreamReader file = null;
            string line;

            file = new StreamReader(CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "wiidownloaderTempLink.txt"));

            if (file == null)
                return "";

            while ((line = file.ReadLine()) != null)
            {
                if (!line.Contains(parsingKeyString))
                    continue;

                downloadUrl = getKeyLink(line, parsingKeyString, finalChar);

                if (file != null)
                    file.Close();

                if (downloadUrl == "")
                    return "";

                downloadUrl = stringToAdd + downloadUrl;

                return downloadUrl;
            }
            if (file != null)
                file.Close();
            return "";
        }


        public static bool SetAllowUnsafeHeaderParsing(bool value)
        {
            //Get the assembly that contains the internal class
            Assembly aNetAssembly = Assembly.GetAssembly(typeof(System.Net.Configuration.SettingsSection));
            if (aNetAssembly != null)
            {
                //Use the assembly in order to get the internal type for the internal class
                Type aSettingsType = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                if (aSettingsType != null)
                {
                    //Use the internal static property to get an instance of the internal settings class.
                    //If the static instance isn't created allready the property will create it for us.
                    object anInstance = aSettingsType.InvokeMember("Section",
                    BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] { });
                    if (anInstance != null)
                    {
                        //Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
                        FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (aUseUnsafeHeaderParsing != null)
                        {
                            aUseUnsafeHeaderParsing.SetValue(anInstance, value);
                            return true;
                        }
                    }
                }
            }
            return false;
        }


        public bool executeDownload(string Text4Label, string url, string file_name, string Text4LabelError)
        {
            AppendText(Text4Label);

            MySleep(100);            
              
            if(!NetworkOk)
            {
                AppendText("\n" + Dictionary.errorDownloadingWithNoNetwork + "\n" + Text4LabelError + " (er:04)" + "\n");
                DOWNLOAD_OR_PROGRAM_WORKING = false;
                return false;
            }

            if (!WiiDownloaderWaitForm.IsHandleCreated)
            {
                try
                {
                    HttpWebRequest httpWReq = (HttpWebRequest)HttpWebRequest.Create(url);
                    HttpWebResponse httpWRes = (HttpWebResponse)httpWReq.GetResponse();
                    fileSize = httpWRes.ContentLength;

                    httpWReq.Abort();
                    httpWRes.Close();
                }
                catch (Exception ex)
                {
                    AppendText("\n" + Dictionary.ErrorOccurred + " " + ex.Message + "\n" + Text4LabelError + " (er:05)" + "\n");
                    return false;
                }

                MAX_DELAY_FOR_TYMER = 60000; // 60 secondi di inattività prima del timeout
                TYMER_INTERVAL = 100; // controllo i dati scaricati ogni 10 centesimi di secondo                              
                /* if (fileSize > 10000000)
                     TYMER_INTERVAL = 200; // controllo i dati scaricati ogni 20 centesimi di secondo
                 else if (fileSize > 5000000)
                     TYMER_INTERVAL = 100; // controllo i dati scaricati ogni 10 centesimi di secondo
                 else
                     TYMER_INTERVAL = 50; // controllo i dati scaricati ogni 5 centesimi di secondo
                 */                       
            }
            else
            {
                MAX_DELAY_FOR_TYMER = 60000; // 60 secondi di inattività prima del timeout
                TYMER_INTERVAL = 1000; // controllo i dati scaricati ogni secondo
                fileSize = 0;
            }
            
            wadDownloadComplete = false;
            timeOut = false;
            timerWorking = true;

            WebClient client = new WebClient();            

            labelProgressBar1.Text = labelProgressBar2.Text = "";
            labelProgressBar1.Visible = true;
            labelProgressBar2.Visible = true;

            System.Windows.Forms.Timer myStandardDowloadTimer = new System.Windows.Forms.Timer();

            if (!Directory.Exists(DOWNLOAD_PATH))
                Directory.CreateDirectory(DOWNLOAD_PATH);            

            try
            {
                Uri URL = new Uri(url);

                myStandardDowloadTimer.Tick += new EventHandler(ProgressBarForDirectLink);
                myStandardDowloadTimer.Interval = TYMER_INTERVAL;
                passageFreezed = 0;
                fileJustDownloaded = 0;                

                client.DownloadFileAsync(URL, file_name);

                timerWorking = false;              
                myStandardDowloadTimer.Start();

                while (DOWNLOAD_OR_PROGRAM_WORKING == true && timeOut == false && client.IsBusy)
                    Application.DoEvents();

                timerWorking = true;
                myStandardDowloadTimer.Stop();
                
            }
            catch (Exception ex)
            {
                timerWorking = true;
                myStandardDowloadTimer.Stop();                
     
                while (client.IsBusy == true)
                    client.CancelAsync();
                AppendText("\n" + Dictionary.ErrorOccurred + " " + ex.Message + "\n" + Text4LabelError + " (er:06)" + "\n");
                                
                labelProgressBar1.Visible = false;
                labelProgressBar2.Visible = false;
                return false;
            }

            timerWorking = true;

            if (DOWNLOAD_OR_PROGRAM_WORKING == false)
            {
                while (client.IsBusy == true)
                {
                    if (client.IsBusy) // to be sure...
                        client.CancelAsync();
                }
                progressBarDownload.Value = 0;
                labelProgressBar1.Visible = false;
                labelProgressBar2.Visible = false;            
                return false;
            }

            if (timeOut == true)
            {
                while (client.IsBusy == true)
                    client.CancelAsync();
                AppendText("\n" + Dictionary.ErrorOccurred + " " + "Time out.\n" + Text4LabelError + " (er:07)" + "\n");

                if (WiiDownloaderWaitForm.IsHandleCreated)
                {
                    WiiDownloaderWaitForm.Close();
                    MessageBox.Show("Time out." + "\n" + Text4LabelError, "WiiDownloader - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }                

                labelProgressBar1.Visible = false;
                labelProgressBar2.Visible = false;
                return false;
            }
            
           // MySleep(200);
            if (FileInUse(file_name))
                return false;

            progressBarDownload.Value = 0;
            labelProgressBar1.Visible = false;
            labelProgressBar2.Visible = false;

            // check for fileszie
            System.IO.FileInfo fileinfo = new System.IO.FileInfo(file_name);
            if (fileinfo.Length == 0)
            {
                string err_msg = "\n" + Dictionary.ErrorOccurred + " '" + Path.GetFileName(file_name) + "' NOT valid. (er:97)\n";

                AppendText(err_msg);

                if (WiiDownloaderWaitForm.IsHandleCreated)
                {
                    WiiDownloaderWaitForm.Close();
                    MessageBox.Show("Warning." + "\n" + err_msg, "WiiDownloader - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }             
                return false;
            }

            AppendText("..OK!\n");          
           
            return true;
        }

        public void ExternalProgramWorking(Object myObject, EventArgs myEventArgs)
        {
            AppendText(".");
            passageFreezed++;
            if (passageFreezed % 5 == 0)
                AppendText(" ");
            if (passageFreezed * TYMER_INTERVAL > MAX_DELAY_FOR_TYMER)
                timeOut = true;
        }       
              

        private bool executeCommand(string Text4Label, string WorkingDirectory, string program, string short_program, string arguments, string Text4LabelError)
        {          
            ProcessStartInfo p_command;
            Process CommandProcess;

            timeOut = false;
            passageFreezed = 0;

            if (program.Contains("EditedModMii.bat"))
                MAX_DELAY_FOR_TYMER = 600000; // 600 secondi (10 minuti) per creare UN cIOS mi sembra abbastanza, cazzo manco un 286.....
            else if (program.Contains("wget.exe"))
                MAX_DELAY_FOR_TYMER = 60000; // 60 secondi (1 minuto) per wget è davvero abbastanza
            else
                MAX_DELAY_FOR_TYMER = 180000; // 180 secondi (3 minuti) per gli altri programmi bastano (a meno che non si scompatti uno zip da 5 GB..=P )

            TYMER_INTERVAL = 1000;  // faccio semplicemente un puntino ogni secondo...             

            System.Windows.Forms.Timer executingTimer = new System.Windows.Forms.Timer();

            executingTimer.Tick += new EventHandler(ExternalProgramWorking);
            executingTimer.Interval = TYMER_INTERVAL;

            p_command = new ProcessStartInfo("cmd.exe", "/c \"" + program + " " + arguments + "\"");
            p_command.UseShellExecute = true;
            p_command.RedirectStandardOutput = false;
            p_command.RedirectStandardInput = false;
            p_command.RedirectStandardError = false;
            p_command.CreateNoWindow = true;
            p_command.WorkingDirectory = WorkingDirectory;
            if(SHOW_PROCESS)
                p_command.WindowStyle = ProcessWindowStyle.Normal;
            else
                p_command.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                if (Text4Label.Trim() != "")
                    AppendText(Text4Label);                
                
                CommandProcess = Process.Start(p_command);
               
                executingTimer.Start();

                while (DOWNLOAD_OR_PROGRAM_WORKING == true && !CommandProcess.HasExited && timeOut == false)
                    Application.DoEvents();
            }
            catch (Exception ex)
            {
                executingTimer.Stop();

                if (DOWNLOAD_OR_PROGRAM_WORKING == true)
                {
                    AppendText("\n" + Dictionary.ErrorOccurred + " " + ex.Message + "\n" + Text4LabelError + " (er:09)" + "\n");

                    if (WiiDownloaderWaitForm.IsHandleCreated)
                    {
                        WiiDownloaderWaitForm.Close();
                        MessageBox.Show(ex.Message + "\n" + Text4LabelError, "WiiDownloader - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }                    
                }

                stopUsedProcess(short_program);

                return false;
            }

            executingTimer.Stop();

            // serve un altro controllo... in quanto il processo a volte rimane appeso.. ci va solo mezzo secondo.. 
            if (timeOut == false && DOWNLOAD_OR_PROGRAM_WORKING == true)
            {
                if(!CommandProcess.HasExited)
                    CommandProcess.WaitForExit();

                if (!waitForAppClose(short_program))
                {
                    stopUsedProcess(short_program);
                    AppendText(".." + Dictionary.ErrorOccurred + " unexpcted error (er:48) using '" + short_program + "'\n");
                    return false;
                }
            }


            if (timeOut == true || DOWNLOAD_OR_PROGRAM_WORKING == false)
            {
                if (timeOut == true)
                    AppendText(".." + Dictionary.ErrorOccurred + " " + "Time out." + Text4LabelError + " (er:08)" + "\n");

                if (!CommandProcess.HasExited)
                    CommandProcess.Kill();

                stopUsedProcess(short_program);

                return false;
            }

            stopUsedProcess(short_program);
            
            return true;
        }
        
        public void ProgressBarForNusd(Object myObject, EventArgs myEventArgs)
        {
            if (timerWorking)
                return;

            timerWorking = true;

            if (wadDownloadComplete == false && DOWNLOAD_OR_PROGRAM_WORKING == true && timeOut == false)
            {
                long dimensione = 0;

                foreach (string s in System.IO.Directory.GetFiles(CombinePath(TOOLS_PATH, DIR_FOR_NUSD)))
                {
                    if (s.Contains(".wad"))
                    {
                        passageFreezed = 0;
                        wadDownloadComplete = true;
                    }
                    else
                    {
                        System.IO.FileInfo f = new System.IO.FileInfo(s);
                        dimensione += f.Length;
                    }
                }

                if (!wadDownloadComplete)
                {
                    if (fileJustDownloaded == dimensione)
                        passageFreezed++;
                    else
                    {
                        passageFreezed = 0;
                        fileJustDownloaded = dimensione;
                    }

                    if (passageFreezed * TYMER_INTERVAL > MAX_DELAY_FOR_TYMER)
                    {
                        timeOut = true;
                        return;
                    }
                }
                //  I use this for add info on "nusdatabase", now it isn't necessary, but don't delete it.

                if (wadDownloadComplete == true && UPDATE_MD5_INI_FILE)
                {
                    long dimensioneTemp = 0;
                    IniFile nus_inifile = new IniFile(CombinePath(NUS_PATH, "nusDatabase.ini"));

                    foreach (string s in System.IO.Directory.GetFiles(CombinePath(TOOLS_PATH, DIR_FOR_NUSD)))
                    {
                        if (!s.Contains(".app") && !s.Contains(".wad") && !s.Contains("tmd.") && !s.Contains("cetk"))
                        {
                            System.IO.FileInfo f = new System.IO.FileInfo(s);
                            dimensioneTemp += f.Length;
                        }
                    }

                    string dimensioneString = dimensioneTemp.ToString();
                    nus_inifile.IniWriteValue("sizeToDownload", DIR_FOR_NUSD, dimensioneString);
                }

                if (dimensione > 0 && fileSize != 0 && wadDownloadComplete != true)
                {
                    int percent;
                    long longPercent = (dimensione * 100) / fileSize;

                    percent = unchecked((int)longPercent);

                    if (percent > 99 && ContentDownloaded == false)
                    {                                               
                        passageFreezed = 0;                        
                        ContentDownloaded = true;
                    }
                    else if (ContentDownloaded == false)
                    {
                        labelProgressBar1.Text = (dimensione / 1024).ToString("0,0", System.Globalization.CultureInfo.CreateSpecificCulture("el-GR")) + " KB / " + (fileSize / 1024).ToString("0,0", System.Globalization.CultureInfo.CreateSpecificCulture("el-GR")) + " KB";
                        labelProgressBar2.Text = percent + "%";

                        if (percent < 101 && percent > 0)
                            progressBarDownload.Value = percent;
                    }
                }
                if (wadDownloadComplete == true && ContentDownloaded == false)
                {                   
                    passageFreezed = 0;                                
                    ContentDownloaded = true;
                }
            }
            timerWorking = false;
        }


        void deleteAllFile(string folder, string type, bool forCache)
        {
            if (!Directory.Exists(folder))
                return;            

            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                if ((type == "") || name.Contains(type))
                {
                    FileDelete(file);
                    if (forCache == true)
                        AppendText(".");
                }               
            }
        }

        

        private bool ExecuteCommandForNusd(string Text4Label, string path_for_wad, string ID, string version, string title_for_Wad, string Text4LabelError)
        {
            string arguments = ID + " " + version;
            string sFileToDownload;
            string databaseFolder = ID + "v" + version;
            ProcessStartInfo p_nusd;
            Process Nusd_Process;
            IniFile nus_inifile = new IniFile(CombinePath(NUS_PATH, "nusDatabase.ini"));

            string program = "\"" + CombinePath(TOOLS_PATH, "nusd.exe") + "\"";

            p_nusd = new ProcessStartInfo("cmd.exe", "/c \"" + program + " " + arguments + "\"");
            p_nusd.UseShellExecute = true;
            p_nusd.RedirectStandardOutput = false;
            p_nusd.RedirectStandardInput = false;
            p_nusd.RedirectStandardError = false;            
            p_nusd.CreateNoWindow = true;
            p_nusd.WorkingDirectory = TOOLS_PATH;
            if (SHOW_PROCESS)
                p_nusd.WindowStyle = ProcessWindowStyle.Normal;
            else
                p_nusd.WindowStyle = ProcessWindowStyle.Hidden;            
            
            sFileToDownload = (nus_inifile.IniReadValue("sizeToDownload", databaseFolder));
            if (sFileToDownload != "")
                fileSize = long.Parse(sFileToDownload);
            else
                fileSize = 0;

            MAX_DELAY_FOR_TYMER = 60000; // 20 secondi  di inattività prima del timeout
            TYMER_INTERVAL = 100; // controllo i dati scaricati ogni 10 centesimi di secondo                              
           /* if (fileSize > 10000000)
                TYMER_INTERVAL = 200; // controllo i dati scaricati ogni 20 centesimi di secondo
            else if (fileSize > 5000000)
                TYMER_INTERVAL = 100; // controllo i dati scaricati ogni 10 centesimi di secondo
            else
                TYMER_INTERVAL = 50; // controllo i dati scaricati ogni 5 centesimi di secondo
            */
                
            DIR_FOR_NUSD = ID;

            wadDownloadComplete = false;
            timeOut = false;

            progressBarDownload.Value = 0;

            AppendText(Text4Label);

            /// prima eliminiamo vecchi file appesi...            
            DeleteFolder(CombinePath(TOOLS_PATH, ID), true);
            Directory.CreateDirectory(CombinePath(TOOLS_PATH, ID));

            DeleteFolder(CombinePath(NUS_PATH, ID + "v" + version), true);

            stopUsedProcess("nusd");

            labelProgressBar1.Text = labelProgressBar2.Text = "";
            labelProgressBar1.Visible = true;
            labelProgressBar2.Visible = true;
            ContentDownloaded = false;
            timerWorking = true;

            System.Windows.Forms.Timer myNUSDTimer = new System.Windows.Forms.Timer();            

            passageFreezed = 0;
            fileJustDownloaded = 0;

            myNUSDTimer.Tick += new EventHandler(ProgressBarForNusd);
            myNUSDTimer.Interval = TYMER_INTERVAL;
            try
            {
                
                Nusd_Process = Process.Start(p_nusd);

                timerWorking = false;
                myNUSDTimer.Start();

                while (wadDownloadComplete == false && DOWNLOAD_OR_PROGRAM_WORKING == true && timeOut == false)
                    Application.DoEvents();                
            }
            catch (Exception ex)
            {
                timerWorking = true;
                myNUSDTimer.Stop();
                labelProgressBar1.Visible = false;
                labelProgressBar2.Visible = false;

                if (DOWNLOAD_OR_PROGRAM_WORKING == true && wadDownloadComplete == false)
                {
                    AppendText("\n" + Dictionary.ErrorOccurred + " " + ex.Message + Text4LabelError + " (er:11)" + "\n");

                    MessageBox.Show(ex.Message + "\n" + Text4LabelError, "WiiDownloader - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                stopUsedProcess("nusd");

                return false;
            }

            timerWorking = true;
            myNUSDTimer.Stop();
            labelProgressBar1.Visible = false;
            labelProgressBar2.Visible = false;

            // ok il download del WAD è finito.... 
            if (wadDownloadComplete == true && DOWNLOAD_OR_PROGRAM_WORKING == true && timeOut == false)
            {                
                AppendText("...OK!\n" + Dictionary.CreatingWadAndcopy);                
                progressBarDownload.Value = 0;
                labelProgressBar1.Text = labelProgressBar2.Text = "";

                if (!waitForAppClose("nusd"))
                {
                    stopUsedProcess("nusd");
                    AppendText(".." + Dictionary.ErrorOccurred + " unexpcted error (er:78) using 'nusd'\n");
                    return false;
                }
            }

            // interruzione dell'utente
            if (DOWNLOAD_OR_PROGRAM_WORKING == false)
            {
                progressBarDownload.Value = 0;

                if (!Nusd_Process.HasExited)
                    Nusd_Process.Kill();

                stopUsedProcess("nusd");

                return false;
            }

            // timeout!
            if (timeOut == true)
            {
                if (!Nusd_Process.HasExited)
                    Nusd_Process.Kill(); 

                AppendText(Dictionary.ErrorOccurred + " " + "Time out. " + Text4LabelError + " (er:12)" + "\n");

                if (WiiDownloaderWaitForm.IsHandleCreated)
                {
                    WiiDownloaderWaitForm.Close();
                    MessageBox.Show("Time out." + "\n" + Text4LabelError, "WiiDownloader - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }                

                Thread.Sleep(0);

                stopUsedProcess("nusd");

                return false;
            }                           
                     
            // copiamo il file scaricato
            foreach (string file in System.IO.Directory.GetFiles(CombinePath(TOOLS_PATH, ID)))
            {
                if (file.Contains(".wad"))
                {                  
                    Directory.CreateDirectory(CombinePath(NUS_PATH, ID + "v" + version));
                                  
                    FileMove(file, CombinePath(NUS_PATH, ID + "v" + version, title_for_Wad), false);
                    
                    DeleteFolder(CombinePath(TOOLS_PATH, ID), true);

                    if (!MD5_check_for_downloaded_wad(ID, version, title_for_Wad, CombinePath(NUS_PATH, ID + "v" + version, title_for_Wad)))
                    {
                        AppendText(Dictionary.ErrorOccurred + " " + "MD5 check failed. " + Text4LabelError + " (er:34)" + "\n");
                        return false;
                    }
                    
                    return true;
                }
            }

            return false;
        }


        private bool applyPatch(string fileDownloaded, string patch, string newSlot, string newVersion, string originalIOS, string newWadName)
        {
            string originalIOS_name = "", originalIOS_version = "";
            bool nameFound = false;

            for (int i = 3; i < originalIOS.Length; i++)
            {
                if (originalIOS[i] != 'v' && !nameFound)
                    originalIOS_name = originalIOS_name + originalIOS[i];
                else
                    nameFound = true;

                if (originalIOS[i] != 'v' && nameFound)
                    originalIOS_version = originalIOS_version + originalIOS[i];
            }


            string patch1 = "", patch2 = "", patch3 = "", patch4 = "";
            if (patch.Length > 1)
                patch1 = " -" + patch.Substring(0, 2);
            if (patch.Length > 3)
                patch2 = " -" + patch.Substring(2, 2);
            if (patch.Length > 5)
                patch3 = " -" + patch.Substring(4, 2);
            if (patch.Length > 7)
                patch4 = " -" + patch.Substring(6, 2);

            if (newSlot.Trim() != "")
                newSlot = " -slot " + newSlot;
            else
                newSlot = " -slot " + originalIOS_name;


            if (newVersion.Trim() != "")
                newVersion = " -v " + newVersion;
            else
                newVersion = " -v " + originalIOS_version;

            cleanTempFolder();
            
            string outputFile = CombinePath(DOWNLOAD_PATH, newWadName);
            string program = "\"" + CombinePath(TOOLS_PATH, "PatchIOS.exe") + "\"";
            
            if (!executeCommand(Dictionary.Patching + " " + originalIOS + " >> " + newWadName + "...",
                                    TOOLS_PATH,
                                    program,
                                    "patchios",
                                    '\"' + fileDownloaded + '\"' + patch1 + patch2 + patch3 + patch4 + newSlot + newVersion + " -o " + '\"' + outputFile + '\"',
                                    "\n" + Dictionary.error + " " + Dictionary.Patching + " " + originalIOS + " : "))                
                return false;

            return true;

        }

        static bool FileInUse(string path)
        {
            int error=0;
            retryFileCheckState:
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    Thread.Sleep(0);
                    return false;
                }

            }
            catch
            {
                try
                {                                    
                    File.SetAttributes(path, FileAttributes.Normal);
                    Thread.Sleep(0);
                    Thread.Sleep(200);
                    error++;
                    if(error == 1)
                        goto retryFileCheckState;
                    else
                        return true;
                }
                catch
                {
                    return true;
                }                
            }
        }

        private void CreateModMiiBatch(string wad_name, string cios_version, string wad_slot, string cios_base, string wad_version, string wad_base)
        {
            if (!Directory.Exists(MOD_MII_DOWNLOAD_QUEUES_PATH))
                Directory.CreateDirectory(MOD_MII_DOWNLOAD_QUEUES_PATH);           
            
            FileDelete(CombinePath(MOD_MII_DOWNLOAD_QUEUES_PATH, "WiiDownloaderBatch.bat"));

            // Creo la stringa
            string lines = "set AdvNumber=0" + "\r\n" +
                            "if /i \"%GetAdvNumberOnly%\" EQU \"Y\" goto:endofqueue" + "\r\n" +
                            "Set ROOTSAVE=off" + "\r\n" +
                            "Set Option1=off" + "\r\n" +
                            "SET " + wad_base + "=*" + "\r\n" +
                            ":endofqueue" + "\r\n";


            // e la sbatto nel file
            System.IO.StreamWriter file = new System.IO.StreamWriter(CombinePath(MOD_MII_DOWNLOAD_QUEUES_PATH, "WiiDownloaderBatch.bat"));
            file.WriteLine(lines);

            file.Close();
        }



        private bool Download_d2x_Modules(string cios_version)
        {
            string link = "";

            switch (cios_version)
            {
                case "d2x-v1-final":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v1-final4321.zip";
                    break;
                case "d2x-v2-final":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v2-final.zip";
                    break;
                case "d2x-v3-final":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v3-final.zip";
                    break;
                case "d2x-v4-final":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v4-final.zip";
                    break;
                case "d2x-v5-final":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v5-final.zip";
                    break;
                case "d2x-v6-final":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v6-final.zip";
                    break;
                case "d2x-v7-final":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v7-final.zip";
                    break;
                case "d2x-v8-final":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v8-final.zip";
                    break;
                case "d2x-v9-beta(r47)":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v9-beta%28r47%29.zip";
                    break;
                case "d2x-v9-beta(r49)":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v9-beta%28r49%29FIX.zip";
                    break;
                case "d2x-v10-beta52":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v10-beta52.zip";
                    break;
                case "d2x-v10-beta53-alt":
                    link = "http://d2x-cios.googlecode.com/files/d2x-v10-beta53-alt.zip";
                    break;
                default:
                    return true;

            }

            if (Directory.Exists(CombinePath(MOD_MII_PATH, "Support", "d2x-beta")))
                DeleteFolder(CombinePath(MOD_MII_PATH, "Support", "d2x-beta"), false);
            if (!Directory.Exists(CombinePath(MOD_MII_PATH, "Support", "d2x-beta")))
                Directory.CreateDirectory(CombinePath(MOD_MII_PATH, "Support", "d2x-beta"));


            if (!File.Exists(CombinePath(MOD_MII_PATH, "Support", "More-cIOSs", cios_version + ".zip")))
            {
                if (!Directory.Exists(CombinePath(MOD_MII_PATH, "Support", "More-cIOSs", cios_version)))
                    Directory.CreateDirectory(CombinePath(MOD_MII_PATH, "Support", "More-cIOSs", cios_version));

                if (!DoDownloadForApplication(cios_version + ".zip",
                              link,
                              CombinePath(MOD_MII_PATH, "Support", "More-cIOSs"),
                              false))                
                    return false;                

                if (!File.Exists(CombinePath(MOD_MII_PATH, "Support", "More-cIOSs", cios_version + ".zip")))
                    return false;

                if (!unZip(cios_version + ".zip",
                            CombinePath(MOD_MII_PATH, "Support", "More-cIOSs", cios_version + ".zip"),
                            CombinePath(MOD_MII_PATH, "Support", "More-cIOSs", cios_version), false))
                    return false;
            }

            CopyFolder(CombinePath(MOD_MII_PATH, "Support", "More-cIOSs", cios_version), CombinePath(MOD_MII_PATH, "Support", "d2x-beta"));
            return true;


        }        

        private bool Download_Modmii_base(string cios_version, string cios_base)
        {
            string ID = "", ID_version = "", wad_name = "";
            switch (cios_base)
            {
                case "37":
                    ID = "0000000100000025";
                    switch (cios_version)
                    {
                        case "Hermes-v4":
                            ID_version = "3612";
                            break;
                        case "Hermes-v5":
                        case "HermesRodries-v5.1":
                        case "Waninkoko-v19":
                            ID_version = "3869";
                            break;
                        default:
                            ID_version = "5662";
                            break;
                    }
                    break;
                case "38":
                    ID = "0000000100000026";
                    switch (cios_version)
                    {
                        case "Hermes-v4":
                        case "Waninkoko-v14":
                            ID_version = "3610";
                            break;
                        case "Hermes-v5":
                        case "HermesRodries-v5.1":
                        case "Waninkoko-v17b":
                        case "Waninkoko-v19":
                            ID_version = "3867";
                            break;
                        default:
                            ID_version = "4123";
                            break;
                    }
                    break;
                case "53":
                    ID = "0000000100000035";
                    ID_version = "5662";
                    break;
                case "55":
                    ID = "0000000100000037";
                    ID_version = "5662";
                    break;
                case "56":
                    ID = "0000000100000038";
                    ID_version = "5661";
                    break;
                case "57":
                    ID = "0000000100000039";
                    switch (cios_version)
                    {
                        case "Hermes-v4":
                        case "Hermes-v5":
                        case "HermesRodries-v5.1":
                        case "Waninkoko-v19":
                            ID_version = "5661";
                            break;
                        default:
                            ID_version = "5918";
                            break;
                    }
                    break;
                case "58":
                    ID = "000000010000003A";
                    ID_version = "6175";
                    break;
                case "60":
                    ID = "000000010000003C";
                    ID_version = "6174";
                    break;
                case "70":
                    ID = "0000000100000046";
                    ID_version = "6687";
                    break;
                case "80":
                    ID = "0000000100000050";
                    ID_version = "6943";
                    break;
                default:
                    return false;

            }

            wad_name = "IOS" + cios_base + "-64-v" + ID_version + ".wad";
            string name_for_text_box = "IOS" + cios_base + " v" + ID_version + " [base cIOS]" ;
            if (File.Exists(CombinePath(MOD_MII_TEMP_PATH, wad_name)))
                return true;

            if (!NetworkOk)
            {
                AppendText("\n" + Dictionary.error + " " + Dictionary.Downloading + " " + name_for_text_box + " : " + Dictionary.errorDownloadingWithNoNetwork + " (er:15)" + "\n");
                DOWNLOAD_OR_PROGRAM_WORKING = false;
                return false;
            }

            string databaseFolder = ID + 'v' + ID_version;
            string arguments = ID + " " + ID_version;       

            bool skip_basewad_download = false;            

            if (wadInCache(ID + "v" + ID_version, wad_name, true))
            {
                if (!Directory.Exists(CombinePath(NUS_PATH, databaseFolder)))
                    Directory.CreateDirectory(CombinePath(NUS_PATH, databaseFolder));

                FileMove(CombinePath(CACHE_PATH, wad_name), CombinePath(NUS_PATH, databaseFolder, wad_name), false);

                skip_basewad_download = true;

            }

            if (!NetworkOk && skip_basewad_download == false)
            {
                AppendText("\n" + Dictionary.error + " " + Dictionary.Downloading + " " + wad_name + " : " + Dictionary.errorDownloadingWithNoNetwork + " (er:22)" + "\n");
                DOWNLOAD_OR_PROGRAM_WORKING = false;
                return false;
            }

            bool result = false;

            if (!skip_basewad_download)
            {
                result = ExecuteCommandForNusd(Dictionary.Downloading + " " + name_for_text_box + "...",                       
                       CombinePath(NUS_PATH, databaseFolder),
                       ID,
                       ID_version,
                       wad_name,                       
                       "\n" + Dictionary.error + " " + Dictionary.Downloading + " " + wad_name + " : ");
            }

            if (result || skip_basewad_download)
            {
                // controllo il file sia stato creato, e quindi lo sposto nella giusta cartella
                foreach (string filepath in System.IO.Directory.GetFiles(CombinePath(NUS_PATH, ID + "v" + ID_version)))
                {
                    if (filepath.Contains(".wad"))
                    {
                        if (!skip_basewad_download)
                            AppendText("..OK!\n");
                        progressBarDownload.Value = 0;
                        if (!FileMove(filepath, CombinePath(MOD_MII_TEMP_PATH, wad_name), true))
                        {
                            DOWNLOAD_OR_PROGRAM_WORKING = false;
                            return false;
                        }
                        DeleteFolder(CombinePath(NUS_PATH, ID + "v" + ID_version, ID), true);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool createCios(string group, string scriptType, string scriptName, string dest)
        {
            IniFile script = new IniFile(CombinePath(SCRIPT_PATH, group, scriptType, scriptName));
            string downloadList = script.IniReadValue("info", "cios_list");
            int cont;
            DOWNLOAD_OR_PROGRAM_WORKING = true;

            string wad_name = "";
            string wad_version = "";
            string wad_slot = "";
            string cios_base = "";
            string wad_base = "";
            string cios_version = "";
            
            char[] delimitTitle = new char[] { ';' };
            char[] delimitTitleInfo = new char[] { ',' };

            foreach (string title in downloadList.Split(delimitTitle))
            {
                if ((title != null) && (title.Trim() != ""))
                {
                    cont = 1;
                    foreach (string titleInfo in title.Split(delimitTitleInfo))
                    {
                        if (titleInfo != null)
                        {
                            if (cont == 1)
                                wad_name = titleInfo.Trim();
                            else if (cont == 2)
                                cios_version = titleInfo.Trim();
                            else if (cont == 3)
                                wad_slot = titleInfo.Trim();
                            else if (cont == 4)
                                cios_base = titleInfo.Trim();
                            else if (cont == 5)
                                wad_version = titleInfo.Trim();
                            else if (cont == 6)
                                wad_base = titleInfo.Trim();

                            cont++;
                        }
                    }

                    if (wad_name != "" && cios_version != "" && wad_slot != "" && cios_base != "" && wad_version != "")
                    {
                        bool CreateCiosErrorCheck;
                        bool two_bases = false;
                        string wad_name_created_by_modmii = "";

                        if (!DOWNLOAD_OR_PROGRAM_WORKING)
                            return false;

                        downloadJustStarted++;

                        if (downloadToDo > 1)
                            labelDownloadToDo.Text = downloadJustStarted.ToString() + "/" + downloadToDo.ToString();

                        if (wadInCache("", wad_name, false))
                        {
                            AppendText(wad_name + " " + Dictionary.FoundInCache);
                            MySleep(100);
                            if (!Directory.Exists(dest))
                                Directory.CreateDirectory(dest);
                            if (!FileMove(CombinePath(CACHE_PATH, wad_name), CombinePath(dest, wad_name), false))
                            {
                                DOWNLOAD_OR_PROGRAM_WORKING = false;
                                return false;
                            }

                            if (dest.Contains("WAD for Modding"))
                                comboBoxWADList.Items.Add(wad_name);

                            AppendText("..OK!\n");
                            continue;
                        }

                        int RETRY_TO_DO = 2;

                    turn_back_base_download:
                        CreateCiosErrorCheck = false;

                        if (two_bases)
                            cios_base = "37-38";

                        if (!Download_d2x_Modules(cios_version))
                        {
                            if (DOWNLOAD_OR_PROGRAM_WORKING == false)
                                return false;

                            CreateCiosErrorCheck = true;
                            goto DownloadBaseError;
                        }

                        CreateModMiiBatch(wad_name, cios_version, wad_slot, cios_base, wad_version, wad_base);

                        // only for hermes v4 that have two bases
                        if (cios_base == "37-38")
                        {
                            two_bases = true;
                            cios_base = "37";
                        }

                    turn_back:

                        if (!Download_Modmii_base(cios_version, cios_base))
                        {
                            if (DOWNLOAD_OR_PROGRAM_WORKING == false)
                                return false;

                            CreateCiosErrorCheck = true;
                            goto DownloadBaseError;
                        }

                        if (two_bases)
                        {
                            if (cios_base == "37")
                            {
                                cios_base = "38";
                                goto turn_back;
                            }
                            else
                                cios_base = "37-38";
                        }

                    DownloadBaseError:
                        if (CreateCiosErrorCheck)
                        {
                            if (RETRY_TO_DO > 0)
                            {
                                RETRY_TO_DO--;
                                AppendText("..retrying..\n");
                                goto turn_back_base_download;
                            }
                            
                            DialogResult myDialogResult;

                            myDialogResult = MessageBox.Show(Dictionary.AskForRepeatSingleCiosDownload, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                            if (myDialogResult == DialogResult.No)
                            {
                                errorCount++;
                                AppendText("... skipped.\n");
                                DialogResult myDialogResult2;

                                myDialogResult2 = MessageBox.Show(Dictionary.AskForAbortCiosDownload, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                if (myDialogResult2 == DialogResult.Yes)                                {
                            
                                    return false;
                                }
                            }
                            else
                            {                            
                                goto turn_back_base_download;
                            }

                        }

                        RETRY_TO_DO = 2;

                    turn_back_modmii:
                        CreateCiosErrorCheck = false;
                    if (!LaunchModMiiBatch(wad_name))
                        CreateCiosErrorCheck = true;
                        
                        string mod_mii_cios_version;
                        switch (cios_version)
                        {
                            case "Hermes-v4":
                                mod_mii_cios_version = "v4";
                                wad_name_created_by_modmii = wad_base.Substring(0, 7) + '[' + cios_base + "]-" + mod_mii_cios_version + ".wad";
                                break;
                            case "Hermes-v5":
                                mod_mii_cios_version = "v5";
                                wad_name_created_by_modmii = wad_base.Substring(0, 7) + '[' + cios_base + "]-" + mod_mii_cios_version + ".wad";
                                break;
                            case "HermesRodries-v5.1":
                                mod_mii_cios_version = "v5.1R";
                                wad_name_created_by_modmii = wad_base.Substring(0, 7) + '[' + cios_base + "]-" + mod_mii_cios_version + ".wad";
                                break;
                            case "Waninkoko-v14":
                                mod_mii_cios_version = "v14";
                                wad_name_created_by_modmii = wad_base.Substring(0, 7) + '-' + mod_mii_cios_version + ".wad";
                                break;
                            case "Waninkoko-v17b":
                                mod_mii_cios_version = "v17b";
                                wad_name_created_by_modmii = wad_base.Substring(0, 7) + '-' + mod_mii_cios_version + ".wad";
                                break;
                            case "Waninkoko-v19":
                                mod_mii_cios_version = "v19";
                                wad_name_created_by_modmii = wad_base.Substring(0, 7) + '[' + cios_base + "]-" + mod_mii_cios_version + ".wad";
                                break;
                            case "Waninkoko-v20":
                                mod_mii_cios_version = "v20";
                                wad_name_created_by_modmii = wad_base.Substring(0, 7) + '[' + cios_base + "]-" + mod_mii_cios_version + ".wad";
                                break;
                            case "Waninkoko-v21":
                                mod_mii_cios_version = "v21";
                                wad_name_created_by_modmii = wad_base.Substring(0, 7) + '[' + cios_base + "]-" + mod_mii_cios_version + ".wad";
                                break;
                            case "System menu patched IOS":                                
                                wad_name_created_by_modmii = wad_name;
                                break;
                            default:// d2x cIOS
                                wad_name_created_by_modmii = wad_base.Substring(0, 7) + '[' + cios_base + "]-" + cios_version + ".wad";
                                break;
                        }

                        if (DOWNLOAD_OR_PROGRAM_WORKING == false)
                            return false;

                        if (!File.Exists(CombinePath(MOD_MII_OUTPUT_PATH, wad_name_created_by_modmii)))
                            CreateCiosErrorCheck = true;                      

                        if (CreateCiosErrorCheck)
                        {
                            if (RETRY_TO_DO > 0)
                            {
                                RETRY_TO_DO--;
                                AppendText("..retrying..\n");
                                goto turn_back_modmii;
                            }

                            DialogResult myDialogResult;

                            myDialogResult = MessageBox.Show(Dictionary.AskForRepeatSingleCiosDownload, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                            if (myDialogResult == DialogResult.No)
                            {
                                errorCount++;

                                DialogResult myDialogResult2;

                                myDialogResult2 = MessageBox.Show(Dictionary.AskForAbortCiosDownload, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                if (myDialogResult2 == DialogResult.Yes)
                                {
                                    return false;
                                }

                            }
                            else
                            {
                                //AppendText("\n");
                                goto turn_back_modmii;
                            }
                        }
                        else
                        {
                            AppendText(Dictionary.Copying);

                            if (!Directory.Exists(dest))
                                Directory.CreateDirectory(dest);
                            

                            string outputFile = CombinePath(dest, wad_name);
                            string localOutputFile = CombinePath(DOWNLOAD_PATH, wad_name);
                            string fileDownloaded = CombinePath(MOD_MII_OUTPUT_PATH, wad_name_created_by_modmii);
                            
                            FileDelete(outputFile);                                                    

                            if (cios_version == "System menu patched IOS")
                            {
                                FileMove(fileDownloaded, localOutputFile, false);

                                if (!checkForFreeSpace(fileDownloaded, dest, true))
                                {
                                    DOWNLOAD_OR_PROGRAM_WORKING = false;
                                    return false;
                                }

                                if(!FileMove(fileDownloaded, outputFile, false))
                                {
                                    DOWNLOAD_OR_PROGRAM_WORKING = false;
                                    return false;
                                }                        
                                AppendText("..OK!\n");                                                               
                            }
                            else
                            {
                                string program = "\"" + CombinePath(TOOLS_PATH, "PatchIOS.exe") + "\"";
                                if (!executeCommand("",
                                                    TOOLS_PATH,
                                                    program,
                                                    "patchios",
                                                    '\"' + fileDownloaded + '\"' + " -slot " + wad_slot + " -v " + wad_version + " -o " + '\"' + localOutputFile + '\"',
                                                    "\n" + Dictionary.error + " " + Dictionary.Patching + " " + fileDownloaded + " : "))
                                    return false;
                              
                                else
                                {
                                    if (!checkForFreeSpace(localOutputFile, dest, true))
                                    {
                                        DOWNLOAD_OR_PROGRAM_WORKING = false;
                                        return false;
                                    }
                                    
                                    if (!FileMove(localOutputFile, outputFile, false))
                                    {
                                        DOWNLOAD_OR_PROGRAM_WORKING = false;
                                        return false;
                                    }     
                                    AppendText("..OK!\n");
                                }
                            }

                            if (outputFile.Contains("WAD for Modding"))
                                comboBoxWADList.Items.Add(wad_name);
                            addWadToCache(localOutputFile, wad_name, "");
                            DeleteFolder(DOWNLOAD_PATH, false);
                          
                        }
                    }
                }
            }
            return true;
        }

        private bool LaunchModMiiBatch(string wad_name)
        {
            string program = "\"" + CombinePath(MOD_MII_PATH, "support", "EditedModMii.bat") + "\"";
            if (executeCommand(    Dictionary.Creating + " " + wad_name + "..",
                                    CombinePath(MOD_MII_PATH, "support"),
                                    program,
                                    "editedmodmii",
                                    "L WiiDownloaderBatch",
                                    "\n" + Dictionary.error + " " + Dictionary.Creating + " " + wad_name + " : " ))
                return true;
            else
                return false;
        }


        private string correctTitle(string originalIOS)
        {
            string title_to_show;

            int i, cont = 0, len = originalIOS.Length;
            for (i = len; i > 1; i--)
            {
                cont++;
                if (originalIOS[i - 1] == 'v')
                    break;
            }

            if (i < 2)
                return originalIOS;

            if (!originalIOS.Contains("System Menu"))
                title_to_show = originalIOS.Substring(0, i - 1) + " " + originalIOS.Substring(i - 1, cont);
            else
            {
                string fw;
                switch (originalIOS.Substring(originalIOS.Length - 3, 3))
                {
                    case "97":
                        fw = " [2.0U]";
                        break;
                    case "128":
                        fw = " [2.0J]";
                        break;
                    case "130":
                        fw = " [2.0E]";
                        break;
                    case "162":
                        fw = " [2.1E]";
                        break;
                    case "192":
                        fw = " [2.2J]";
                        break;
                    case "193":
                        fw = " [2.2U]";
                        break;
                    case "194":
                        fw = " [2.2E]";
                        break;
                    case "224":
                        fw = " [3.0J]";
                        break;
                    case "225":
                        fw = " [3.0U]";
                        break;
                    case "226":
                        fw = " [3.0E]";
                        break;
                    case "256":
                        fw = " [3.1J]";
                        break;
                    case "257":
                        fw = " [3.1U]";
                        break;
                    case "258":
                        fw = " [3.1E]";
                        break;
                    case "288":
                        fw = " [3.2J]";
                        break;
                    case "289":
                        fw = " [3.2U]";
                        break;
                    case "290":
                        fw = " [3.2E]";
                        break;
                    case "352":
                        fw = " [3.3J]";
                        break;
                    case "353":
                        fw = " [3.3U]";
                        break;
                    case "354":
                        fw = " [3.3E]";
                        break;
                    case "384":
                        fw = " [3.4J]";
                        break;
                    case "385":
                        fw = " [3.4U]";
                        break;
                    case "386":
                        fw = " [3.4E]";
                        break;
                    case "390":
                        fw = " [3.5K]";
                        break;
                    case "416":
                        fw = " [4.0J]";
                        break;
                    case "417":
                        fw = " [4.0U]";
                        break;
                    case "418":
                        fw = " [4.0E]";
                        break;
                    case "448":
                        fw = " [4.1J]";
                        break;
                    case "449":
                        fw = " [4.1U]";
                        break;
                    case "450":
                        fw = " [4.1E]";
                        break;
                    case "454":
                        fw = " [4.1K]";
                        break;
                    case "480":
                        fw = " [4.2J]";
                        break;
                    case "481":
                        fw = " [4.2U]";
                        break;
                    case "482":
                        fw = " [4.2E]";
                        break;
                    case "486":
                        fw = " [4.2K]";
                        break;
                    case "512":
                        fw = " [4.3J]";
                        break;
                    case "513":
                        fw = " [4.3U]";
                        break;
                    case "514":
                        fw = " [4.3E]";
                        break;
                    case "518":
                        fw = " [4.3K]";
                        break;                    
                    default:
                        fw = "";
                        break;
                }
                title_to_show = originalIOS.Substring(0, i - 1) + " " + originalIOS.Substring(i - 1, cont) + fw;
            }

            return title_to_show;

        }


        private void addWadToCache(string wadForCache, string titleToDownload, string long_name)
        {
            if (!CACHE_ENABLED)
                return;

            IniFile md5_wad_list = new IniFile(CombinePath(CACHE_PATH, "md5_wad_list.ini"));            
            string iniMD5;

            if (File.Exists(wadForCache))
            {
                if (!Directory.Exists(CACHE_PATH))
                    Directory.CreateDirectory(CACHE_PATH);                
                FileMove(wadForCache, CombinePath(CACHE_PATH, titleToDownload), false);
                iniMD5 = GetMD5HashFromFile(wadForCache);
            }
            else
                iniMD5 = "";

            string name_to_add;
            if (long_name == "")
                name_to_add = titleToDownload;
            else
                name_to_add = long_name + "->" + titleToDownload;

            md5_wad_list.IniWriteValue("md5_wad_list", name_to_add, iniMD5);
        }
        

        private bool wadInCache(string long_name, string titleToDownload, bool nusDatabaseCheck)
        {
            if (!CACHE_ENABLED)
                return false;
            
            if (!File.Exists(CombinePath(CACHE_PATH, titleToDownload)))
                return false;

            string name_to_search;
            if (long_name == "")
                name_to_search = titleToDownload;
            else
                name_to_search = long_name + "->" + titleToDownload;

            string iniMD5;
            IniFile md5_wad_list = new IniFile(CombinePath(CACHE_PATH, "md5_wad_list.ini"));
            iniMD5 = md5_wad_list.IniReadValue("md5_wad_list", name_to_search);

            string nusDatabaseMD5;
            IniFile nusDatabase_list = new IniFile(CombinePath(NUS_PATH, "nusDatabase.ini"));
            nusDatabaseMD5 = nusDatabase_list.IniReadValue("md5_wad_list", name_to_search);


            if (nusDatabaseMD5 != "" && nusDatabaseCheck)
            {
                if (iniMD5 != nusDatabaseMD5)
                {
                    md5_wad_list.IniWriteValue("md5_wad_list", name_to_search, "");
                    FileDelete(CombinePath(CACHE_PATH, titleToDownload));
                    return false;
                }
            }

            if (iniMD5 == "")
                return false;

            if (iniMD5 == GetMD5HashFromFile(CombinePath(CACHE_PATH, titleToDownload)))
                return true;
            else
                FileDelete(CombinePath(CACHE_PATH, titleToDownload));

            return false;
        }
               
        
        private bool MD5_check_for_downloaded_wad(string ID, string version, string fileName, string filePath)
        {           
            string nusDatabaseMD5;
            string name_to_search = ID + "v" + version + "->" + fileName;
            
            IniFile nus_inifile = new IniFile(CombinePath(NUS_PATH, "nusDatabase.ini"));
           
            nusDatabaseMD5 = nus_inifile.IniReadValue("md5_wad_list", name_to_search);

            if (UPDATE_MD5_INI_FILE == false && nusDatabaseMD5 == "")
                return true;

            string MD5_from_file = GetMD5HashFromFile(filePath);

            if (nusDatabaseMD5 == MD5_from_file)
                return true;

            if (!UPDATE_MD5_INI_FILE || nusDatabaseMD5 != "")
            {
                FileDelete(filePath);
                return false;
            }
            else
            {                
                 nus_inifile.IniWriteValue("md5_wad_list", name_to_search, MD5_from_file);
                 return true;
            }
            
        }


        private bool useNUSD(string group, string scriptType, string scriptName, string dest)
        {
            IniFile script = new IniFile(CombinePath(SCRIPT_PATH, group, scriptType, scriptName));
            string downloadList = script.IniReadValue("info", "nus_list");
            int cont;
            string ID = "";
            string filename = "";
            string version = "";
            string patch = "";
            string newSlot = "";
            string newVersion = "";
            string originalIOS = "";

            char[] delimitTitle = new char[] { ';' };
            char[] delimitTitleInfo = new char[] { ',' };

            foreach (string title in downloadList.Split(delimitTitle))
            {
                if ((title != null) && (title.Trim() != ""))
                {
                    cont = 0;
                    foreach (string titleInfo in title.Split(delimitTitleInfo))
                    {
                        if (titleInfo != null)
                        {
                            if (cont == 1)
                                filename = titleInfo.Trim();
                            else if (cont == 2)
                                ID = titleInfo.Trim();
                            else if (cont == 3)
                                version = titleInfo.Trim();
                            else if (cont == 4)
                                patch = titleInfo.Trim();
                            else if (cont == 5)
                                newVersion = titleInfo.Trim();
                            else if (cont == 6)
                                newSlot = titleInfo.Trim();
                            else if (cont == 7)
                                originalIOS = titleInfo.Trim();

                            cont++;
                        }
                    }
                    if (filename != "" && ID != "" && version != "")
                    {                        
                        bool continueSingleNusDownload = true;
                        string databaseFolder = ID + 'v' + version;
                        Directory.CreateDirectory(CombinePath(NUS_PATH, databaseFolder));                        

                        downloadJustStarted++;

                        if (downloadToDo > 1)
                            labelDownloadToDo.Text = downloadJustStarted.ToString() + "/" + downloadToDo.ToString();

                        int RETRY_TO_DO = 2;

                        while (continueSingleNusDownload == true)
                        {
                            bool skip_basewad_download = false;
                            if (DOWNLOAD_OR_PROGRAM_WORKING == false)
                                return false;

                            string titleToDownload;                  

                            if (patch != "" || newSlot != "" || newVersion != "")
                                titleToDownload = originalIOS + ".wad";
                            else
                                titleToDownload = filename;

                            string title_to_show = correctTitle(originalIOS);
                                                 
                            if (wadInCache(ID + "v" + version, filename, true))
                            {
                                AppendText(filename + " " + Dictionary.FoundInCache);
                                MySleep(100);
                                if (!Directory.Exists(dest))
                                    Directory.CreateDirectory(dest);

                                if (!checkForFreeSpace(CombinePath(CACHE_PATH, filename), dest, true))
                                {
                                    DOWNLOAD_OR_PROGRAM_WORKING = false;
                                    return false;
                                }

                                if (!FileMove(CombinePath(CACHE_PATH, filename), CombinePath(dest, filename), false))
                                {
                                    DOWNLOAD_OR_PROGRAM_WORKING = false;
                                    return false;
                                }
                                if (dest.Contains("WAD for Modding"))
                                    comboBoxWADList.Items.Add(filename);
                                AppendText("..OK!\n");                               
                                goto gotoNextScript;
                            }

                            if (patch != "" || newSlot != "" || newVersion != "")
                            {
                                if (wadInCache(ID + "v" + version, originalIOS.Replace(@"v", @"-64-v") + ".wad", false))
                                {
                                    if (!Directory.Exists(CombinePath(NUS_PATH, databaseFolder)))
                                        Directory.CreateDirectory(CombinePath(NUS_PATH, databaseFolder));

                                    FileMove(CombinePath(CACHE_PATH, originalIOS.Replace(@"v", @"-64-v") + ".wad"), CombinePath(NUS_PATH, databaseFolder, originalIOS.Replace(@"v", @"-64-v") + ".wad"), false);
                                    
                                    skip_basewad_download = true;

                                }
                            }

                            if (!NetworkOk)
                            {
                                AppendText("\n" + Dictionary.error + " " + Dictionary.Downloading + " " + titleToDownload + " : " + Dictionary.errorDownloadingWithNoNetwork + " (er:23)" + "\n");
                                DOWNLOAD_OR_PROGRAM_WORKING = false;
                                return false;
                            }

                            bool result = true;

                            if (!skip_basewad_download)                                                            
                                result = ExecuteCommandForNusd(Dictionary.Downloading + " " + title_to_show + "...",                                       
                                       CombinePath(NUS_PATH, databaseFolder),
                                       ID,
                                       version,
                                       titleToDownload,                                       
                                       "\n" + Dictionary.error + " " + Dictionary.Downloading + " " + titleToDownload + " : ");


                            if (DOWNLOAD_OR_PROGRAM_WORKING == false)
                                return false;                                                       

                            if (result != true)
                            {
                                if (RETRY_TO_DO > 0)
                                {
                                    RETRY_TO_DO--;
                                    AppendText("..retrying..\n");
                                    continue;
                                }
                                
                                DialogResult myDialogResult;

                                myDialogResult = MessageBox.Show(Dictionary.AskForRepeatSingleNusDownload, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                if (myDialogResult == DialogResult.No)
                                {
                                    errorCount++;
                                    continueSingleNusDownload = false;
                                    DialogResult myDialogResult2;

                                    myDialogResult2 = MessageBox.Show(Dictionary.AskForAbortNusDownload, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                    if (myDialogResult2 == DialogResult.Yes)
                                        return false;
                                    else
                                        goto gotoNextScript;
                                }
                                else
                                    continue;
                            }
                            else
                            {
                                string fileDownloaded = "";

                               // string[] files = Directory.GetFiles(CombinePath(NUS_PATH, databaseFolder));
                                string[] files = Directory.GetFiles(CombinePath(NUS_PATH, databaseFolder));
                                foreach (string file in files)
                                {
                                    if (file.Contains(".wad"))
                                        fileDownloaded = file;
                                }

                                if (fileDownloaded.Trim() == "")
                                {
                                    AppendText("\n" + Dictionary.error + " " + Dictionary.Downloading + " " + filename + " (er:24)" + "...\n");
                                    DialogResult myDialogResult;

                                    myDialogResult = MessageBox.Show(Dictionary.AskForRepeatSingleNusDownload, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                    if (myDialogResult == DialogResult.No)
                                    {
                                        AppendText("... skipped.\n");
                                        continueSingleNusDownload = false;
                                        DialogResult myDialogResult2;

                                        myDialogResult2 = MessageBox.Show(Dictionary.AskForAbortNusDownload, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                        if (myDialogResult2 == DialogResult.Yes)
                                            return false;
                                        else
                                            goto gotoNextScript;
                                    }
                                    else
                                        continue;
                                }
                                
                                if (!Directory.Exists(dest))
                                    Directory.CreateDirectory(dest);

                                string wadForCache = "";
                           
                                if (patch != "" || newSlot != "" || newVersion != "")
                                {
                                    if (!skip_basewad_download)
                                        AppendText("..OK!\n");
                                
                                    if (!applyPatch(fileDownloaded, patch, newSlot, newVersion, originalIOS, filename))
                                        return false;

                                    string filePatched = "";

                                    if (Directory.Exists(DOWNLOAD_PATH))
                                    {
                                        string[] wadFiles = Directory.GetFiles(DOWNLOAD_PATH);
                                        foreach (string wadFile in wadFiles)
                                        {
                                            if (wadFile.Contains(".wad"))
                                                filePatched = wadFile;
                                        }
                                    }

                                    if (filePatched.Trim() == "")
                                    {
                                        AppendText("\n" + Dictionary.error + " " + Dictionary.Patching + " " + originalIOS + " (er:25)" + "...\n");
                                        errorCount++;

                                        DialogResult myDialogResult2;

                                        myDialogResult2 = MessageBox.Show(Dictionary.AskForAbortNusDownload, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                        if (myDialogResult2 == DialogResult.Yes)
                                            return false;
                                        else
                                            goto gotoNextScript;
                                    }
                                    else
                                    {
                                        AppendText(Dictionary.Copying);

                                        if (!checkForFreeSpace(filePatched, dest, true))
                                        {
                                            DOWNLOAD_OR_PROGRAM_WORKING = false;
                                            return false;
                                        }

                                        if (!FileMove(filePatched, CombinePath(dest, filename), false))
                                        {
                                            DOWNLOAD_OR_PROGRAM_WORKING = false;
                                            return false;
                                        }
                                        wadForCache = filePatched;
                                        continueSingleNusDownload = false;
                                        AppendText("..OK!\n");
                                    }
                                }
                                else
                                {
                                    FileDelete(CombinePath(dest, titleToDownload));

                                    if (!checkForFreeSpace(fileDownloaded, dest, true))
                                    {
                                        DOWNLOAD_OR_PROGRAM_WORKING = false;
                                        return false;
                                    }

                                    if (!FileMove(fileDownloaded, CombinePath(dest, titleToDownload), false))
                                    {
                                        DOWNLOAD_OR_PROGRAM_WORKING = false;
                                        return false;
                                    }
                                    wadForCache = fileDownloaded;
                                    
                                    continueSingleNusDownload = false;
                                    AppendText("..OK!\n");
                                }

                                if (dest.Contains("WAD for Modding"))
                                    comboBoxWADList.Items.Add(filename);
                                addWadToCache(wadForCache, filename, ID + "v" + version);
                                FileDelete(wadForCache);                                
                                
                            }                                                                        
                        }
                    }
                }
            gotoNextScript:
                continue;
            }
            return true;
        }


        private void checkBoxCopyToSD_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxCopyToSD.Checked == true)
            {               
                this.Cursor = Cursors.WaitCursor;                
                this.Refresh();
                this.Enabled = false;
            
                bool DeviceFound = false;
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo d in drives)
                {
                    if (d.IsReady)
                    {
                        switch (d.DriveType)
                        {
                            case DriveType.Fixed:
                                break;
                            case DriveType.Removable:
                                break;
                            default:
                                continue;
                        }
                        if (Environment.SystemDirectory.Substring(0, 1) == d.Name.Substring(0, 1)) // no write in he root of SO device
                            continue;

                        if(d.VolumeLabel.Trim() == "")
                            comboBoxDevice.Items.Add(d.Name);
                        else
                            comboBoxDevice.Items.Add(d.Name + " [" + d.VolumeLabel + ']');

                        DeviceFound = true;
                    }
                }
                if (!DeviceFound)
                {
                    checkBoxCopyToSD.Checked = false;
                    MessageBox.Show(Dictionary.NoDevice, this.Text + " - " + Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                    comboBoxDevice.Enabled = true;

                this.Enabled = true;
                this.Cursor = Cursors.Default;
                this.Refresh();
            }
            else
            {
                comboBoxDevice.Enabled = false;
                comboBoxDevice.Items.Clear();
            }
            comboBoxDevice.Update();

        }

        private bool MultiDownload(string group, string scriptType, string scriptName)
        {
            IniFile scriptINI = new IniFile(CombinePath(SCRIPT_PATH, group, scriptType, scriptName));
            char[] delimitScript = new char[] { ';' };
            char[] delimitScriptInfo = new char[] { ',' };
            int cont;
            string tempGroup = "", tempType = "", tempName = "";

            string downloadList = scriptINI.IniReadValue("info", "script_list");

            foreach (string script in downloadList.Split(delimitScript))
            {
                if ((script != null) && (script.Trim() != ""))
                {                                      
                    cont = 0;
                    foreach (string scriptInfo in script.Split(delimitScriptInfo))
                    {
                        if ((scriptInfo != null) && (scriptInfo.Trim() != ""))
                        {
                            if (cont == 0)
                                tempGroup = scriptInfo.Trim();
                            else if (cont == 1)
                                tempType = scriptInfo.Trim();
                            else if (cont == 2)
                                tempName = scriptInfo.Trim();

                            cont++;
                        }
                    }
                    if (tempGroup != "" && tempType != "" && tempName != "")
                    {
                        bool continueMultiDownload = true;
                        while (continueMultiDownload == true)
                        {
                            IniFile tempScript;
                            bool result;
                            bool forceFolderChange;
                            string new_folder, scriptString;
                            tempScript = new IniFile(CombinePath(SCRIPT_PATH, tempGroup, tempType, tempName));
                            if (File.Exists(CombinePath(SCRIPT_PATH, tempGroup, tempType, tempName)))
                                scriptString = tempScript.IniReadValue("info", "source");
                            else
                                scriptString = "";
                            switch (scriptString)
                            {
                                case "Download from URL":
                                    result = StandardDownload(tempGroup, tempType, tempName);
                                    break;
                                case "Other features":
                                    result = useOtherFeatures(tempGroup, tempType, tempName);
                                    break;                                    
                                case "Download from NUS":

                                    if ((scriptINI.IniReadValue("info", "forceToFolder") != ""))
                                    {
                                        forceFolderChange = Convert.ToBoolean(scriptINI.IniReadValue("info", "forceToFolder"));
                                        new_folder = scriptINI.IniReadValue("info", "newFolder");
                                    }
                                    else
                                    {
                                        forceFolderChange = false;
                                        new_folder = "";
                                    }

                                    result = NUSDownload(tempGroup, tempType, tempName, forceFolderChange, new_folder);

                                    break;
                                case "Download using ModMii":
                                    if ((scriptINI.IniReadValue("info", "forceToFolder") != ""))
                                    {
                                        forceFolderChange = Convert.ToBoolean(scriptINI.IniReadValue("info", "forceToFolder"));
                                        new_folder = scriptINI.IniReadValue("info", "newFolder");
                                    }
                                    else
                                    {
                                        forceFolderChange = false;
                                        new_folder = "";
                                    }
                                    result = cIOSDownload(tempGroup, tempType, tempName, forceFolderChange, new_folder);
                                    break;
                                default:
                                    result = false;
                                    return false;
                            }

                            if (result == true)
                                continueMultiDownload = false;
                            else
                            {
                                if (DOWNLOAD_OR_PROGRAM_WORKING == false)
                                    return false;
                                
                                DialogResult myDialogResult;

                                myDialogResult = MessageBox.Show(Dictionary.AskForAbortDownload, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                if (myDialogResult == DialogResult.Yes)
                                    return false;
                                else
                                {
                                    continueMultiDownload = false;
                                    DOWNLOAD_OR_PROGRAM_WORKING = true;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        // funciotn for copy a folder and all files, to another path 
        static public bool CopyFolder(string sourcePath, string targetPath)
        {            
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);            
            string[] files = Directory.GetFiles(sourcePath);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(targetPath, name);
            
                if(!FileMove(file, dest, false))     
                    return false;                
            }
            string[] folders = Directory.GetDirectories(sourcePath);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(targetPath, name);
                CopyFolder(folder, dest);
            }
            return true;
        }

        static public bool CleanFolder(string targetPath)
        {
            Thread.Sleep(0);
            string[] files = Directory.GetFiles(targetPath);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(targetPath, name);
                if (!FileDelete(dest))                
                    return false;
            }
            string[] folders = Directory.GetDirectories(targetPath);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(targetPath, name);
                Thread.Sleep(0);
                if (CleanFolder(dest))
                    return true;
                else
                    return false;
            }
            return true;
        }

        
        static public void DeleteOnlySubFolder(string targetPath)
        {
            string[] folders = Directory.GetDirectories(targetPath);
            foreach (string dir in folders)
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                { }
            }
        }

        static public void DeleteFolder(string targetPath, bool removeFirstDir)
        {
            if (!Directory.Exists(targetPath))
                return;

            if (CleanFolder(targetPath))
            {
                if (removeFirstDir == true)
                {
                    try
                    {
                        Directory.Delete(targetPath, true);
                    }
                    catch
                    { }
                }
                else
                    DeleteOnlySubFolder(targetPath);
            }
        }


        private void gbatempLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://gbatemp.net/topic/331626-wiidownloader/");

        }


        private bool NusDownloaderIniFileCheck()
        {
            if (UPDATE_MD5_INI_FILE)
                return true; 
            
            if(File.Exists(CombinePath(NUS_PATH, "nusDatabase.ini")))
            {
                string correct_MD5 = "698a217e5f4c21ca2eabed12d3259b0b";

                string md5_from_file = GetMD5HashFromFile(CombinePath(NUS_PATH, "nusDatabase.ini"));

                if (md5_from_file == correct_MD5)
                    return true;                
            }

            FileDelete(CombinePath(NUS_PATH, "nusDatabase.ini"));

            if (!DoDownloadForApplication("nusDatabase.ini",
                                          wiiDownloaderFilesLink + "nusDatabase.ini",                 
                                          NUS_PATH,
                                          true))                            
                return false;

            DOWNLOAD_OR_PROGRAM_WORKING = true;
            return true;
        }

        private bool otherCheckBeforeStart()
        {
            if (!NusDownloaderIniFileCheck())
                return false;

            if (!Directory.Exists(CACHE_PATH))
                Directory.CreateDirectory(CACHE_PATH);
            
            if (checkBoxCopyToSD.Checked == true && comboBoxDevice.Text == "")
            {
                MessageBox.Show(Dictionary.NoDeviceSelected,
                              this.Text,
                             MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return false;
            }

            if (checkBoxCopyToSD.Checked == false)
            {
                if (!Directory.Exists(CombinePath(STARTUP_PATH, folderForFiles)))
                {
                    Directory.CreateDirectory(CombinePath(STARTUP_PATH, folderForFiles));

                    return true;
                }

                IniFile ini = new IniFile(SETTINGS_INI_FILE);
                string AboutMerge = ini.IniReadValue("AboutMerge", "Checked");

                if (AboutMerge == "MergeAlways")
                    return true;

                if (AboutMerge == "AskAlways")
                {
                    // then ask what to do
                    DialogResult myDialogResult;
                    myDialogResult = MessageBox.Show(Dictionary.AskForMerge, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (myDialogResult == DialogResult.Yes)
                        return true;
                }

                bool lastDirectoryCreateFound = false;
                int cont = 0;
                string new_folder = "";
                while (!lastDirectoryCreateFound)
                {
                    cont++;
                    new_folder = "COPY_TO_DEVICE(" + cont + ")";
                    if (!Directory.Exists(CombinePath(STARTUP_PATH, new_folder)))
                        lastDirectoryCreateFound = true;
                }
                folderForFiles = new_folder;
            }
            else if (!Directory.Exists(comboBoxDevice.Text.Substring(0, 3)))
            {
                MessageBox.Show("Device '" + comboBoxDevice.Text.Substring(0, 3) + "' NOT found!",
                         this.Text,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);

              
                return false;
            }
                
            
            return true;
        }

        private void CursorInDefaultState(bool value)
        {
            if (value == true)
                this.Cursor = Cursors.Default;
            else
            {
                this.Cursor = Cursors.WaitCursor;
                buttonDonwload.Cursor = Cursors.Default;
                infoTextBox.Cursor = Cursors.Default;                
            }
            this.Refresh();

        }       

        private bool moveFileToRoot(string scriptName, string fileToTheRoot)
        {
            string dest;
            if (checkBoxCopyToSD.Checked == false)
                dest = CombinePath(STARTUP_PATH, folderForFiles);
            else
                dest = comboBoxDevice.Text.Substring(0, 3);

            AppendText(Dictionary.CopyingFile + " " + scriptName + " " + Dictionary.ToTheRoot + "..");

            if (!File.Exists(CombinePath(dest, fileToTheRoot)))
            {
                AppendText(" " + Dictionary.error + ": " + Dictionary.FileNotFound + " " + fileToTheRoot + " (er:26)");
                return false;
            }

            
            FileDelete(CombinePath(dest, "boot.elf"));
            
            FileDelete(CombinePath(dest, "boot.dol"));

            string filename = fileToTheRoot.Substring(fileToTheRoot.Length - 8, 8);                   
            
            FileDelete(CombinePath(dest, filename));

            if(!FileMove(CombinePath(dest, fileToTheRoot), CombinePath(dest, filename), false))
            {
                DOWNLOAD_OR_PROGRAM_WORKING = false;
                return false;
            }          

            AppendText("..OK!\n");

            return true;
        }       

        private bool useWilbrand()
        {
            if (!File.Exists(CombinePath(TOOLS_PATH, "wilbrand.exe")))
            {
                AppendText("\n" + "Wilbrand.exe not found in tools folder! " + "\n");
                return false;  
            }
            
            // creating Date...
            string exploitDate = "";
            {
                DateTime yesterday = DateTime.Now.AddDays(-1);
                int theDay = yesterday.Day;
                int theMonth = yesterday.Month;
                int theYear = yesterday.Year;
                if (theMonth < 10)
                    exploitDate = "0";
                if (theDay < 10)
                    exploitDate = exploitDate + theMonth + "/0" + theDay + "/" + theYear;
                else
                    exploitDate = exploitDate + theMonth + "/" + theDay + "/" + theYear;
            }

            // creating arguments for Exploit...           
          //  string startupPath = System.IO.Directory.GetCurrentDirectory();

            IniFile settingsFile = new IniFile(SETTINGS_INI_FILE);
            string arguments_4_Exploit =    settingsFile.IniReadValue("WiiInfo", "mac_address") +
                                            " " +
                                            exploitDate +
                                            " " +
                                            settingsFile.IniReadValue("WiiInfo", "exploitFW") +
                                            " " +
                                            STARTUP_PATH.Substring(0, 2);
                                         //   START.Substring(0, 2);

            // preparo la "temp folder"
            Directory.CreateDirectory(WIIDOWNLOADER_TEMP_FOLDER);
            FileMove(CombinePath(TOOLS_PATH, "wilbrand.exe"), CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "wilbrand.exe"), false);
            if (!Directory.Exists(WIIDOWNLOADER_TEMP_FOLDER))
            {
                AppendText("\nError creating temporary folder for wilbrand ...\n");
                return false;
            }           

            // ant then start it...    
            string program = "\"" + CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "wilbrand.exe") + "\"";
            if (!executeCommand(    Dictionary.UsingWilbrand,
                                    WIIDOWNLOADER_TEMP_FOLDER,
                                    program,
                                    "wilbrand",
                                    arguments_4_Exploit,
                                    "\n" + Dictionary.error + " " + Dictionary.UsingWilbrand + " : "))
                    return false;

            AppendText("..");
            MySleep(500);
            AppendText("..");

            if (!Directory.Exists(CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "private")))
            {
                AppendText("\nError creating exploit: private folder not found ...\n");
                return false;
            }

            string dest;
            if (checkBoxCopyToSD.Checked == false)
                dest = CombinePath(STARTUP_PATH, folderForFiles);
            else
                dest = comboBoxDevice.Text.Substring(0, 3);

            if (!Directory.Exists(CombinePath(dest, "private")))
                Directory.CreateDirectory(CombinePath(dest, "private"));

            if (!CopyFolder(CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "private"), CombinePath(dest, "private")))
            {
                DeleteFolder(WIIDOWNLOADER_TEMP_FOLDER, true);
                DOWNLOAD_OR_PROGRAM_WORKING = false;
                return false;
            }

            DeleteFolder(WIIDOWNLOADER_TEMP_FOLDER, true);                       

            AppendText("..OK!\n");

            return true;
        }

        private bool useOtherFeatures(string group, string scriptType, string scriptName)
        {
            IniFile scriptIniFile = new IniFile(CombinePath(SCRIPT_PATH, group, scriptType, scriptName));
           
            switch (scriptIniFile.IniReadValue("info", "OtherFeatures"))
            {
                case "moveToRoot":
                    downloadJustStarted++;
                    if (!moveFileToRoot(scriptName, scriptIniFile.IniReadValue("info", "fileToTheRoot")))
                        return false;
                    break;
                case "useWilbrand":
                    downloadJustStarted++;
                    if (!useWilbrand())
                        return false;
                    break;
                    
            }
            
            
            return true;
        }

        private bool CountDownloadToDo(string group, string scriptType, string scriptName)
        {
            if (!File.Exists(CombinePath(SCRIPT_PATH, group, scriptType, scriptName)))
            {
                AppendText("\n" + Dictionary.error + ": " + Dictionary.InvalidScript + "\n" + '"' + group + "\" > \"" + scriptType + "\" > \"" + scriptName + "\" not found." + " (er:28)");
                return false;
            }

            IniFile scriptIniFile = new IniFile(CombinePath(SCRIPT_PATH, group, scriptType, scriptName));
            string scriptSource = scriptIniFile.IniReadValue("info", "source");
            string downloadList="";

            switch (scriptSource)
            {
                case "Download from URL":
                    downloadToDo++;
                    return true;
                case "Other features":
                    downloadToDo++;
                    if (scriptIniFile.IniReadValue("info", "OtherFeatures") == "useWilbrand")
                    {                        
                        IniFile settingsFile = new IniFile(SETTINGS_INI_FILE);
                        if ((settingsFile.IniReadValue("WiiInfo", "exploitFW") == "") || (settingsFile.IniReadValue("WiiInfo", "mac_address") == ""))
                        {
                            MessageBox.Show(Dictionary.WiiInfoNotFound, "WiiDownloader - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                    }

                    return true;                    
                case "Download from NUS":
                    downloadList = scriptIniFile.IniReadValue("info", "nus_list");
                    break;
                case "Download using ModMii":
                    downloadList = scriptIniFile.IniReadValue("info", "cios_list");
                    break;
                case "Combine existing script":
                    downloadList = scriptIniFile.IniReadValue("info", "script_list");
                    break;
            }            
                              

            char[] delimitTitle = new char[] { ';' };
            char[] delimitTitleInfo = new char[] { ',' };

            switch (scriptSource)
            {                
                case "Download from NUS":
                case "Download using ModMii":
                    foreach (string title in downloadList.Split(delimitTitle))
                    {
                        if ((title != null) && (title.Trim() != ""))                        
                            downloadToDo++;                        
                    }
                    break;
                case "Combine existing script":
                    string tempGroup = "", tempType = "", tempName = "";
                    foreach (string script in downloadList.Split(delimitTitle))
                    {
                        if ((script != null) && (script.Trim() != ""))
                        {
                            int tempCont = 0;
                            foreach (string scriptInfo in script.Split(delimitTitleInfo))
                            {
                                if ((scriptInfo != null) && (scriptInfo.Trim() != ""))
                                {
                                    if (tempCont == 0)
                                        tempGroup = scriptInfo.Trim();
                                    else if (tempCont == 1)
                                        tempType = scriptInfo.Trim();
                                    else if (tempCont == 2)
                                        tempName = scriptInfo.Trim();

                                    tempCont++;
                                }
                            }
                            if (tempGroup != "" && tempType != "" && tempName != "")
                            {
                                if(!CountDownloadToDo(tempGroup, tempType, tempName))
                                    return false;            
                            }
                        }
                    }
                    break;
            }

            return true;
        }

        private void cleanWadFolder() 
        {            
            this.Enabled = false;                       

            string Wad_for_Modding_folder;
            if (checkBoxCopyToSD.Checked == false)
                Wad_for_Modding_folder = CombinePath(STARTUP_PATH, folderForFiles, "wad", "Wad for Modding");
            else
                Wad_for_Modding_folder = CombinePath(comboBoxDevice.Text.Substring(0, 3), "wad", "Wad for Modding");

            AppendText("Initial check..");
            deleteAllFile(Wad_for_Modding_folder, "", true);            
            AppendText("..OK!\n");
            MySleep(1000);            
            this.Enabled = true;

        }


        private void CreateErrorLogFile()
        {
            System.IO.StreamWriter errorLogFile = new System.IO.StreamWriter(CombinePath(STARTUP_PATH, "error_log.txt"), true, Encoding.Unicode);                      

            errorLogFile.WriteLine("\r\n*******************************************************************************\r\n"
                                + "* This file is automatically created by WiiDownloader on " + DateTime.Now
                                + " *\r\n*******************************************************************************\r\n\r\n");

            errorLogFile.WriteLine("SYSTEM INFORMATION\r");
            errorLogFile.WriteLine("------------------\r\n");
            errorLogFile.WriteLine("OSVersion = {0}", System.Environment.OSVersion + "\r");            
            errorLogFile.WriteLine("ServicePack = {0}", System.Environment.OSVersion.ServicePack + "\r");         
            errorLogFile.WriteLine("Working Directory = {0}", STARTUP_PATH + "\r");
           
            errorLogFile.WriteLine("\r\n\r\n");

            errorLogFile.WriteLine("TEXT BOX HISTORY\r");
            errorLogFile.WriteLine("----------------\r\n");
            errorLogFile.WriteLine(infoTextBox.Text.Replace("\n", "\r\n"));

            if(File.Exists(CombinePath(CACHE_PATH, "md5_wad_list.ini")))
            {
                errorLogFile.WriteLine("\r\n\r\n");
                errorLogFile.WriteLine("MD5 HISTORY\r");
                errorLogFile.WriteLine("-----------\r\n");
                string[] cachefile = File.ReadAllLines(CombinePath(CACHE_PATH, "md5_wad_list.ini"));

                foreach (string line in cachefile)    
                    errorLogFile.WriteLine(line + "\r"); 
            }

            errorLogFile.Close();
            
        }

        private void buttonDonwload_Click(object sender, EventArgs e)
        {
            if (DOWNLOAD_OR_PROGRAM_WORKING == true)
            {
                ButtonValueForDownload(false);
                buttonDonwload.Enabled = false;
                DOWNLOAD_OR_PROGRAM_WORKING = false;
                return;
            }

            buttonDonwload.Enabled = false;
            buttonDonwload.Text = Dictionary.StopDownload;
            DOWNLOAD_OR_PROGRAM_WORKING = true;
            ButtonValueForDownload(false);
            FileDelete(CombinePath(STARTUP_PATH, "error_log.txt"));
            errorCount = 0;            
            downloadToDo = 0;
            downloadJustStarted = 0;            
            comboBoxWADList.Items.Clear();

            infoTextBox.Clear();
            bool result;

            if (!otherCheckBeforeStart())
            {
                DOWNLOAD_OR_PROGRAM_WORKING = false;                
                ButtonValueForDownload(true);
                buttonDonwload.Enabled = true;
                buttonDonwload.Text = Dictionary.StartDownload; 
                return;
            }
            buttonDonwload.Enabled = true;

            if (Global.group == "Standard" && comboBoxType.Text == "Hacking" && comboBoxScriptName.Text == "Hack My Wii")            
                cleanWadFolder();
            

            IniFile script;
            script = new IniFile(CombinePath(SCRIPT_PATH, Global.group, comboBoxType.Text, comboBoxScriptName.Text));
            string scriptString = script.IniReadValue("info", "source");

            CursorInDefaultState(false);
            SendKeys.Send("{TAB}");

            switch (scriptString)
            {
                case "Download from URL":                    
                    if (!CountDownloadToDo(Global.group, comboBoxType.Text, comboBoxScriptName.Text))                    
                        result = false;
                    else
                        result = StandardDownload(Global.group, comboBoxType.Text, comboBoxScriptName.Text);
                    break;
                case "Download from NUS":                    
                    if (!CountDownloadToDo(Global.group, comboBoxType.Text, comboBoxScriptName.Text))
                        result = false;
                    else
                        result = NUSDownload(Global.group, comboBoxType.Text, comboBoxScriptName.Text, false, "");                    
                    break;
                    
                case "Download using ModMii":
                    if (!CountDownloadToDo(Global.group, comboBoxType.Text, comboBoxScriptName.Text))
                        result = false;
                    else
                        result = cIOSDownload(Global.group, comboBoxType.Text, comboBoxScriptName.Text, false, "");
                    break;
                case "Other features":
                    if (!CountDownloadToDo(Global.group, comboBoxType.Text, comboBoxScriptName.Text))
                        result = false;
                    else
                        result = useOtherFeatures(Global.group, comboBoxType.Text, comboBoxScriptName.Text);
                    break;
                case "Combine existing script":
                    if (!CountDownloadToDo(Global.group, comboBoxType.Text, comboBoxScriptName.Text))
                        result = false;
                    else
                        result = MultiDownload(Global.group, comboBoxType.Text, comboBoxScriptName.Text);
                    break;
                default:
                    result = false;
                    break; //impossible case
            }

            MySleep(100);

            CursorInDefaultState(true);

            buttonDonwload.Enabled = true; // in case user stop download, i freeze the button for a little

            buttonDonwload.Text = Dictionary.StartDownload;          
            
            // deletig old files
            cleanTempFolder();

            ButtonValueForDownload(true);
            buttonDonwload.Enabled = true;


            if (DOWNLOAD_OR_PROGRAM_WORKING == false && fileCompareError > max_compare_error)
            {
                fileCompareError = 0;
                AppendText("\n\n" + Dictionary.TooCompareError + "\n");
                CreateErrorLogFile();
                return;
            }

            if (DOWNLOAD_OR_PROGRAM_WORKING == false && !NetworkOk)
            {
                AppendText("\n" + Dictionary.DownloadNotComplete + "\n");
                CreateErrorLogFile();
                return;    
            }
            if (DOWNLOAD_OR_PROGRAM_WORKING == false && NoFreeSpace)
            {
                AppendText("\n" + Dictionary.DownloadNotComplete + "\n");
                CreateErrorLogFile();
                return;    
            }

            if (DOWNLOAD_OR_PROGRAM_WORKING == false)
            {
                AppendText("\n\n" + Dictionary.DownloadStopped + "\n");
                CreateErrorLogFile();
                return;    
            }

            DOWNLOAD_OR_PROGRAM_WORKING = false;

            if (result == false)
            {
                AppendText("\n" + Dictionary.DownloadNotComplete + "\n");

                CreateErrorLogFile();

                
                
               // if (scriptString == "Download from URL" || scriptString == "Other features")
               //     return;
            }
            else if (errorCount > 0)
            {
                AppendText("\n" + Dictionary.DownloadNotComplete + "\n");

                CreateErrorLogFile();
            }
            else
            {
                labelDownloadToDo.Text = "";
                AppendText("\n" + Dictionary.DownloadComplete + "\n");

                if (Global.group == "Standard" && comboBoxType.Text == "Hacking" && comboBoxScriptName.Text == "Hack My Wii")
                {
                    // do soto messaggio solo dopo un luuungo download
                    MessageBox.Show(Dictionary.DownloadComplete + "\n", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);                    
                }

                if (!checkBoxCopyToSD.Checked == true)
                {
                    // then ask to user for open COPY_TO_SD folder

                    DialogResult myDialogResult;
                    myDialogResult = MessageBox.Show(Dictionary.AskForSeeFiles, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (myDialogResult == DialogResult.Yes)
                    {
                        Process.Start(CombinePath(STARTUP_PATH, folderForFiles));
                    }
                }
                if (Global.group == "Standard" && comboBoxType.Text == "Hacking" && comboBoxScriptName.Text == "Hack My Wii")
                {
                    // infine creo la guida
                    createGuide();
                    AppendText("\n" + Dictionary.GuideCreated + "\n");
                }
            }
        }      
        
        private void comboBoxType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (EnableSearch == true)
                EnableButton(comboBoxTypeUpdate());
        }

        private void comboBoxScriptName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (EnableSearch == true)
                LoadImageAndInfo();
        }

        private bool NUSDownload(string group, string scriptType, string scriptName, bool forceFolderChange, string folderForWAD)
        {
            IniFile script;
            script = new IniFile(CombinePath(SCRIPT_PATH, group, scriptType, scriptName));

            string copyWadTo = script.IniReadValue("info", "copyWadTo");

            // creation "destination"
            if (forceFolderChange == true)
                copyWadTo = folderForWAD;

            string dest;
            if (checkBoxCopyToSD.Checked == false)
                dest = CombinePath(STARTUP_PATH, folderForFiles, copyWadTo);
            else
                dest = CombinePath(comboBoxDevice.Text.Substring(0, 3), copyWadTo);

            // deletig old files
            cleanTempFolder();           

            if (!useNUSD(group, scriptType, scriptName, dest))
                return false;

            // deletig old files
            cleanTempFolder();

            return true;
        }

        private bool unZip(string filename, string filePath, string extractTo, bool createSubDir)
        {
            Directory.CreateDirectory(extractTo);

            if (createSubDir)
            {
                string program = "\"" + CombinePath(TOOLS_PATH, "7za920.exe") + "\"";
                if (!executeCommand(Dictionary.Extracting + " " + filename + "...",
                                    TOOLS_PATH,
                                    program,
                                    "7za920",
                                    "x -y -o" + '\u0022' + extractTo + '\u0022' + " " + '\u0022' + filePath + '\u0022',
                                    "\n" + Dictionary.error + " " + Dictionary.Extracting + " " + filename + " : "))
                {
                    return false;
                }                                              
            }
            else
            {
                string program = "\"" + CombinePath(TOOLS_PATH, "7za920.exe") + "\"";
                if (!executeCommand(Dictionary.Extracting + " " + filename + "...",
                                    TOOLS_PATH,
                                    program,
                                    "7za920",
                                    "e -y -o" + '\u0022' + extractTo + '\u0022' + " " + '\u0022' + filePath + '\u0022',
                                    "\n" + Dictionary.error + " " + Dictionary.Extracting + " " + filename + " : "))
                {
                    return false;
                }                
            }

        
            AppendText("..OK!\n");
            return true;
        }        

        private bool unRar(string filename, string filePath, string extractTo)
        {
            Directory.CreateDirectory(extractTo);

            string program = "\"" + CombinePath(TOOLS_PATH, "unrar.exe") + "\"";
            if (!executeCommand(Dictionary.Extracting + " " + filename + "...",
                                   TOOLS_PATH,
                                   program,
                                   "unrar",
                                   "x " + '\u0022' + filePath + '\u0022' + " " + '\u0022' + extractTo + '\u0022',
                                   "\n" + Dictionary.error + " " + Dictionary.Extracting + " " + filename + " : "))
            {

                return false;
            }
                   
            AppendText("..OK!\n");
            return true;
        }              

        private bool renameFile(string sourcePath, string fileToRename, string renameIn)
        {
            string newFile = Path.Combine(sourcePath, renameIn);                        

            string[] files = Directory.GetFiles(sourcePath);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string fileFound = Path.Combine(sourcePath, name);
                if (name == fileToRename)
                {                                                                            
                    if(!FileDelete(newFile))
                        return false;

                    if(!FileMove(fileFound, newFile, true))
                    {
                        DOWNLOAD_OR_PROGRAM_WORKING = false;
                        return false;
                    }    

                    return true;
                }
            }

            string[] folders = Directory.GetDirectories(sourcePath);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string directoryFound = Path.Combine(sourcePath, name);
                
                renameFile(directoryFound, fileToRename, renameIn);   
            }
            return true;
        }

        private void takeImage(string folderForSerach, IniFile script, string scriptName, string newImageFile)
        {
            string[] files = Directory.GetFiles(folderForSerach);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string fileFound = Path.Combine(folderForSerach, name);
                if (name.ToLower() == "icon.png")
                {
                    if (!File.Exists(newImageFile))
                        File.Copy(fileFound, newImageFile, true);
                    script.IniWriteValue("info", "searchImage", "");
                    script.IniWriteValue("info", "imageFileLink", scriptName + ".png");
                    if (File.Exists(newImageFile)) // I hope yes.. =P
                        pictureBoxIcon.Image = new Bitmap(newImageFile);
                    return;
                }
            }

            string[] folders = Directory.GetDirectories(folderForSerach);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string directoryFound = Path.Combine(folderForSerach, name);
                takeImage(directoryFound, script, scriptName, newImageFile);
            }
        }

        private bool CopyFolderFunction(string source, string dest)
        {           
            if (!CopyFolder(source, dest))            
                return false;           

            return true;
        }

        private int isAnArchive(string downloadedFile)
        {
            string tempFileExtension = "";
            for (int i = downloadedFile.Length; i > 1; i--)
            {
                tempFileExtension = tempFileExtension + downloadedFile[i - 1];
                if (downloadedFile[i - 1] == '.')
                    break;
            }

            string fileExtension = "";
            for (int i = tempFileExtension.Length; i > 0; i--)
                fileExtension = fileExtension + tempFileExtension[i - 1];

            if (fileExtension.ToLower() == ".zip" || fileExtension == ".7z" || fileExtension == ".tar")
                return 1;
            else if (fileExtension == ".rar")
                return 2;
            else if (fileExtension == ".gz")
                return 3;
            else
                return 0;
        }

        private bool checkModMiiVersion()
        {
            IniFile settingsFile = new IniFile(SETTINGS_INI_FILE);

            if (settingsFile.IniReadValue("version", "modmii") != FEATURED_MODMII_VERSION || !File.Exists(CombinePath(MOD_MII_PATH, "ModMii.exe")))
            {
                if(!NetworkOk && File.Exists(CombinePath(MOD_MII_PATH, "support", "EditedModMii.bat")))
                    return true;

                FileDelete(CombinePath(MOD_MII_PATH, "support", "EditedModMii.bat"));
                if (!startModMiiUpdater())
                    return false;
            }

            if(!File.Exists(CombinePath(MOD_MII_PATH, "support", "EditedModMii.bat")))
            {
                if(!EditingModMiiBatch())
                    return false;
            }

            settingsFile.IniWriteValue("version", "modmii", FEATURED_MODMII_VERSION);

            return true;
        }

        private bool EditingModMiiBatch()
        {
            if(!File.Exists(CombinePath(MOD_MII_PATH, "support", "ModMii.bat")))
                return false;

            AppendText("Editing Modmii..");

            StringBuilder newFile = new StringBuilder();

            string temp = "";

            string[] file = File.ReadAllLines(CombinePath(MOD_MII_PATH, "support", "ModMii.bat"));

            bool lineFound = false;

            foreach (string line in file)
            {

                if (line.Contains("::.NET Framework 3.5 check+installation"))
                {

                    temp = line.Replace("::.NET Framework 3.5 check+installation", "goto:skipframeworkinstallation");

                    newFile.Append(temp + "\r\n");

                    lineFound = true;

                    continue;

                }

                newFile.Append(line + "\r\n");

            }

            File.WriteAllText(CombinePath(MOD_MII_PATH, "support", "EditedModMii.bat"), newFile.ToString());

            if(lineFound == true)
                AppendText("..OK!\n");

            return lineFound;
        }


        private bool cIOSDownload(string group, string scriptType, string scriptName, bool forceFolderChange, string folderForWAD)
        {
            IniFile script;
            script = new IniFile(CombinePath(SCRIPT_PATH, group, scriptType, scriptName));

            string copyWadTo = script.IniReadValue("info", "copyCiosTo");

            // creation "destination"
            if (forceFolderChange == true)
                copyWadTo = folderForWAD;

            string dest;
            if (checkBoxCopyToSD.Checked == false)
                dest = CombinePath(STARTUP_PATH, folderForFiles, copyWadTo);
            else
                dest = CombinePath(comboBoxDevice.Text.Substring(0, 3), copyWadTo);

            // deletig old files
            cleanTempFolder();            

            // check for ModMii Update
            if (!checkModMiiVersion())
                return false;

            if (!createCios(group, scriptType, scriptName, dest))
                return false;

            // deletig old files
            cleanTempFolder();
           
            return true;
        }

        protected string GetMD5HashFromFile(string fileName)
        {
            if (FileJustOpen(fileName))
                return "";
            FileStream file = new FileStream(fileName, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        private bool MD5fileCheck(string downloadedFile, string iniMD5)
        {
            if(iniMD5 == "")
                return false;
            
            if(!File.Exists(CombinePath(CACHE_PATH, downloadedFile)))
                return false;

            if (iniMD5 == GetMD5HashFromFile(CombinePath(CACHE_PATH, downloadedFile)) )
                return true;
            else
                FileDelete(CombinePath(CACHE_PATH, downloadedFile));

            return false;

        }

        static long DirSize(DirectoryInfo directory)
        {
            long size = 0;

            FileInfo[] files = directory.GetFiles();
            foreach (FileInfo file in files)
            {
                size += file.Length;
            }

            DirectoryInfo[] dirs = directory.GetDirectories();

            foreach (DirectoryInfo dir in dirs)
            {
                size += DirSize(dir);
            }

            return size;
        }


        private bool checkForFreeSpace(string source, string destination, bool is_only_a_file)
        {
            string destination_for_file = destination.Substring(0, 1);
            NoFreeSpace = false;            
            
            long size;
            if(is_only_a_file)
            {                
                FileInfo f = new FileInfo(source);	            
                size = f.Length;
            }
            else
            {
                var folder = new DirectoryInfo(source);
                size = DirSize(folder);
            }            

            DriveInfo c = new DriveInfo(destination_for_file);
            long free_space = c.AvailableFreeSpace;
            
            if (free_space <= size)
            {
                NoFreeSpace = true;
                AppendText("\n" + Dictionary.ErrorOccurred + " " + Dictionary.NoFreeSpace1 + destination_for_file + Dictionary.NoFreeSpace2 + " (er:31)" + "\n");

                MessageBox.Show(Dictionary.NoFreeSpace1 + destination_for_file + Dictionary.NoFreeSpace2, "WiiDownloader - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else
                return true;
        }

        private bool MD5_check_for_downloaded_file(string MD5_to_check, string file_to_check)
        {
            if (MD5_to_check == "")
                return true;

            if (UPDATE_MD5_INI_FILE)
                return true;

            string MD5_from_file = GetMD5HashFromFile(file_to_check);

            if (MD5_to_check == MD5_from_file)
                return true;

            FileDelete(file_to_check);
            return false;
        }

        

        private bool StandardDownload(string group, string scriptType, string scriptName)
        {
            IniFile script;
            script = new IniFile(CombinePath(SCRIPT_PATH, group, scriptType, scriptName));

            string urlType = script.IniReadValue("info", "urlType");
            string link = script.IniReadValue("info", "link");
            string downloadedFile = script.IniReadValue("info", "downloadedFile");
            string folderToTake = script.IniReadValue("info", "folderToTake");
            string copyTo = script.IniReadValue("info", "copyTo");
            string fileToRename = script.IniReadValue("info", "fileToRename");
            string renameIn = script.IniReadValue("info", "renameIn");
            string searchImage = script.IniReadValue("info", "searchImage");
            string downloadFrom = script.IniReadValue("info", "downloadFrom");
            string iniMD5="";

            bool NoUseOfLocalFiles;
            if (!CACHE_ENABLED)
                NoUseOfLocalFiles = true;
            else
            {
                if ((script.IniReadValue("info", "noUseOfLocalFiles") != "True"))
                {
                    NoUseOfLocalFiles = false;
                    iniMD5 = script.IniReadValue("info", "MD5");
                }
                else               
                    NoUseOfLocalFiles = true;
            }

            string linkToProcess = "";

            // creation "destination"
            string dest;
            if (checkBoxCopyToSD.Checked == false)
                dest = CombinePath(STARTUP_PATH, folderForFiles, copyTo); //, folderToTake);
            else
                dest = CombinePath(comboBoxDevice.Text.Substring(0, 3), copyTo); //, folderToTake);

            downloadJustStarted++;

            if (downloadToDo > 1)
                labelDownloadToDo.Text = downloadJustStarted.ToString() + "/" + downloadToDo.ToString();

            // Directory.CreateDirectory(CombinePath(dest, folderToTake));

            bool continueDownload = true;
            int RETRY_TO_DO = 2;
            while (continueDownload == true)
            {
                bool MD5Check = false;
                
                // deletig old files
                cleanTempFolder();                              

                // ### QUI VERIFICO SE USARE I FILE LOCALI
                if (!NoUseOfLocalFiles && !UPDATE_MD5_INI_FILE)
                {
                    MD5Check = MD5fileCheck(downloadedFile, iniMD5);
                    
                    if (MD5Check == true)
                    {                        
                        AppendText(downloadedFile + " " + Dictionary.FoundInCache);
                        MySleep(100);
                        FileMove(CombinePath(CACHE_PATH, downloadedFile), CombinePath(DOWNLOAD_PATH, downloadedFile), false);
                        AppendText("..OK!\n");
                        goto skipDoDownload;
                    }
                }

                if (urlType == "dinamic") 
                {
                    AppendText(Dictionary.CreatingLink + " " + scriptName + "...");
                    linkToProcess = CreateActualLink(link, downloadFrom);

                    if (DOWNLOAD_OR_PROGRAM_WORKING == false)
                    {
                        MySleep(500);
                        FileDelete(CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "wiidownloaderTempLink.txt"));                        
                        DeleteFolder(WIIDOWNLOADER_TEMP_FOLDER, true); 
                        return false;
                    }

                    FileDelete(CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "wiidownloaderTempLink.txt"));                  ;
                    DeleteFolder(WIIDOWNLOADER_TEMP_FOLDER, true); 

                    if (linkToProcess == "")
                    {
                        AppendText("\n" + Dictionary.error + ": " + Dictionary.NoResponse + " " + link + " (er:32)" + "\n");
                        goto Error;
                    }
                    AppendText("...OK!\n");
                }
                else
                    linkToProcess = link;

                if (downloadedFile == "")
                    goto Error;               

                // downloading file
                if (!DoDownload(downloadedFile,
                                CombinePath(DOWNLOAD_PATH, downloadedFile),
                                linkToProcess))
                {

                    if (DOWNLOAD_OR_PROGRAM_WORKING == true)
                        goto Error;

                    errorCount++;
                    return false;
                }

                if(!MD5_check_for_downloaded_file(script.IniReadValue("info", "MD5"), CombinePath(DOWNLOAD_PATH, downloadedFile)))
                {
                    AppendText(Dictionary.ErrorOccurred + " " + "MD5 check failed for " + downloadedFile + " (er:34)");                    
                    goto Error;
                }

            skipDoDownload:
                if (!NoUseOfLocalFiles && MD5Check == false)                                    
                    FileMove(CombinePath(DOWNLOAD_PATH, downloadedFile), CombinePath(CACHE_PATH, downloadedFile), false);                               

                int fileType = isAnArchive(downloadedFile);
                string pathForDownloadedFiles="";

                if (fileType == 0) // ins't an archive                                    
                    pathForDownloadedFiles = DOWNLOAD_PATH;                                   
                else if (fileType == 1) // zip file
                {
                    if (!unZip(downloadedFile,
                                CombinePath(DOWNLOAD_PATH, downloadedFile),
                                EXTRACTED_FILES_PATH, true))
                    {
                        if (DOWNLOAD_OR_PROGRAM_WORKING == false)
                            return false;
                        else
                            goto Error;
                    }
                    
                    pathForDownloadedFiles = EXTRACTED_FILES_PATH;                   
                    
                }
                else if (fileType == 2) // rar file
                {
                    if (!unRar(downloadedFile,
                                CombinePath(DOWNLOAD_PATH, downloadedFile),
                                EXTRACTED_FILES_PATH))
                    {
                        if (DOWNLOAD_OR_PROGRAM_WORKING == false)
                            return false;
                        else
                            goto Error;
                    }

                    pathForDownloadedFiles = EXTRACTED_FILES_PATH;                                      
                }
                else if (fileType == 3) // gz file ( devo tirare fuori il .tar, poi lo decomprimo di nuovo)
                {
                    if (!unZip(downloadedFile,
                                CombinePath(DOWNLOAD_PATH, downloadedFile),
                                EXTRACTED_FILES_PATH, true))
                    {
                        if (DOWNLOAD_OR_PROGRAM_WORKING == false)
                            return false;
                        else
                            goto Error;
                    }

                    string[] files = Directory.GetFiles(EXTRACTED_FILES_PATH);
                    foreach (string tar_archive_path in files)
                    {
                        if (tar_archive_path.Contains(".tar"))
                        {
                            string tar_archive = Path.GetFileName(tar_archive_path);

                            if (!unZip(tar_archive,
                                tar_archive_path,
                                EXTRACTED_FILES_PATH, true))
                            {
                                if (DOWNLOAD_OR_PROGRAM_WORKING == false)
                                    return false;
                                else
                                    goto Error;
                            }
                            FileDelete(tar_archive_path);
                            break;
                        }
                    }   

                    pathForDownloadedFiles = EXTRACTED_FILES_PATH;  
                }

                // this is a particoular case for USB Loader GX
                if((group == "Standard") && (scriptType == "Homebrew - Loader - GX") && (scriptName == "USB Loader GX (last beta version)"))
                {
                    FileDelete(CombinePath(EXTRACTED_FILES_PATH, folderToTake, "boot.elf"));
                }    

                if (!NoUseOfLocalFiles && MD5Check == false)
                {
                    if (File.Exists(CombinePath(DOWNLOAD_PATH, downloadedFile)))
                        iniMD5 = GetMD5HashFromFile(CombinePath(DOWNLOAD_PATH, downloadedFile));
                    else
                        iniMD5 = "";

                    if (UPDATE_MD5_INI_FILE && (script.IniReadValue("info", "MD5") != iniMD5))
                        MessageBox.Show("MD5 changed!!", "WiiDownloader", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    script.IniWriteValue("info", "MD5", iniMD5);
                }

                if (fileToRename.Trim() != "")
                {                    
                    if (!renameFile(pathForDownloadedFiles, fileToRename, renameIn))
                    {
                        DOWNLOAD_OR_PROGRAM_WORKING = false;
                        return false;
                    }
                }

                if (!Directory.Exists(CombinePath(pathForDownloadedFiles, folderToTake)))
                {
                    MessageBox.Show(Dictionary.error + ": \"" + folderToTake + "\" not forund..", "WiiDownloader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    folderToTake = "";                    
                    return false;
                }

                AppendText( ".." + Dictionary.Copying);

                if (!checkForFreeSpace(CombinePath(pathForDownloadedFiles, folderToTake), dest, false))
                {
                    //downloadStarted = false;
                    return false;
                }
                  
                if (!CopyFolderFunction(CombinePath(pathForDownloadedFiles, folderToTake), dest))
                {
                    //downloadStarted = false;
                    return false;
                }

                if (dest.Contains("WAD for Modding"))
                {
                    string[] files = Directory.GetFiles(CombinePath(pathForDownloadedFiles, folderToTake));
                    foreach (string file in files)
                    {
                        if (file.Contains(".wad"))
                            comboBoxWADList.Items.Add(Path.GetFileName(file));                           
                    }                    
                }

                AppendText("..OK!\n");


                if ((urlType == "dinamic") && (downloadFrom == "bootmii.org") && (group == "Standard"))
                {
                    // qua non posso usare la FileMove, perchè l'immagine potrebbe essere già in uso.
                    if (!File.Exists(CombinePath(IMAGES_PATH, "hackmii installer.png")))
                    {
                        if (DoDownloadForApplication("hackmii installer.png",
                                imageFolderLink + "hackmii installer.png",
                                IMAGES_PATH,
                                false))
                        {
                            if (!File.Exists(CombinePath(dest, "icon.png")))                            
                                File.Copy(CombinePath(IMAGES_PATH, "hackmii installer.png"), CombinePath(dest, "icon.png"), true);
                        }                        
                    }
                    else if (!File.Exists(CombinePath(dest, "icon.png")))                    
                        File.Copy(CombinePath(IMAGES_PATH, "hackmii installer.png"), CombinePath(dest, "icon.png"), true);
                }                

                if (searchImage.Trim() == "True")
                {
                    string newImageFile = Path.Combine(IMAGES_PATH, scriptName + ".png");
                    takeImage(EXTRACTED_FILES_PATH, script, scriptName, newImageFile);
                }                

                continueDownload = false;
                break;

            Error:
                {
                    if (RETRY_TO_DO > 0)
                    {
                        RETRY_TO_DO --;
                        AppendText("..retrying..\n");
                        continue;
                    }
                
                DialogResult myDialogResult;

                    myDialogResult = MessageBox.Show(Dictionary.AskForRepeatDownload, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (myDialogResult == DialogResult.No)
                    {
                        errorCount++;
                        AppendText("... skipped.\n");
                        return false;
                    }
                    else
                        AppendText("\n");
                }
            }

            // deletig old files
            cleanTempFolder();

            return true;

        }


        private void linkLabelOfficialSite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            IniFile scriptInfo;
            scriptInfo = new IniFile(CombinePath(SCRIPT_PATH, Global.group, comboBoxType.Text, comboBoxScriptName.Text));

            string homebrewOfficialSite = scriptInfo.IniReadValue("info", "officialSite");
            if (homebrewOfficialSite != "")
                Process.Start(homebrewOfficialSite);
        }


        private void appAutomaticToolStrip_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("AutomaticUpdate", "application", "True");
            readSettingsFile();
        }

        private void appAskToolStrip_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("AutomaticUpdate", "application", "AskBefore");
            readSettingsFile();
        }

        private void appManualToolStrip_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("AutomaticUpdate", "application", "False");
            readSettingsFile();
        }

        private void databaseAutomaticToolStrip_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("AutomaticUpdate", "database", "True");
            readSettingsFile();
        }

        private void databaseAskToolStrip_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("AutomaticUpdate", "database", "AskBefore");
            readSettingsFile();
        }

        private void databaseManualToolStrip_Click(object sender, EventArgs e)
        {
            databaseAutomaticToolStrip.Checked = false;
            databaseAskToolStrip.Checked = false;
            updateNowDatabaseToolStrip.Enabled = true;
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("AutomaticUpdate", "database", "False");
            readSettingsFile();
        }

        private void updateNowDatabaseToolStrip_Click(object sender, EventArgs e)
        {
            buttonDonwload.Enabled = false;
            callCheckSettings("DatabaseUpdate");
            buttonDonwload.Enabled = true;            
        }

        private void updateNowAppToolStrip_Click(object sender, EventArgs e)
        {
            buttonDonwload.Enabled = false;
            callCheckSettings("AppUpdate");
            buttonDonwload.Enabled = true;  
        }
        
        private void buttonEdit_Click(object sender, EventArgs e)
        {
            if (radioButtonCustom.Checked == true)
                Global.editorMode = "Edit";
            else if (radioButtonStandard.Checked == true)
                Global.editorMode = "View";
            else // ???? impossible case
                return;

            Global.name = comboBoxScriptName.Text;
            Global.type = comboBoxType.Text;
            
            ScriptEdit ScriptEditForm = new ScriptEdit();
            ScriptEditForm.Owner = this;
            this.Enabled = false;
            ScriptEditForm.Show();
        }

        private void buttonNew_Click(object sender, EventArgs e)
        {
            Global.editorMode = "New";
            ScriptEdit ScriptEditForm = new ScriptEdit();
            ScriptEditForm.Owner = this;
            this.Enabled = false;
            ScriptEditForm.Show();
        }

        private void italianoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("Language", "LanguageChoice", "italiano");
            createDictionary();
            LoadImageAndInfo();
        }

        private void españolToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("Language", "LanguageChoice", "español");
            createDictionary();
            LoadImageAndInfo();
        }

        private void chineseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("Language", "LanguageChoice", "chinese");
            createDictionary();
            LoadImageAndInfo();
        }

        private void dutchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("Language", "LanguageChoice", "dutch");
            createDictionary();
            LoadImageAndInfo();
        }

       

        private void frenchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("Language", "LanguageChoice", "french");
            createDictionary();
            LoadImageAndInfo();
        }

        private void portuguêsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("Language", "LanguageChoice", "português");
            createDictionary();
            LoadImageAndInfo();
        }  
                

        private void englishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("Language", "LanguageChoice", "english");
            createDictionary();
            LoadImageAndInfo();
        }

        private void radioButtonStandard_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonStandard.Checked == true)
            {
                Global.group = "Standard";
                EnableButton(comboBoxGroupUpdate());
            }
        }

        private void radioButtonCustom_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonCustom.Checked == true)
            {
                Global.group = "Custom";
                EnableButton(comboBoxGroupUpdate());
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            DialogResult ans = MessageBox.Show(Dictionary.AskForDelete,
                             this.Text + " - Info",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ans == DialogResult.Yes)
            {
                FileDelete(CombinePath(CUSTOM_SCRIPT_PATH, comboBoxType.Text, comboBoxScriptName.Text));
                var folder = new DirectoryInfo(CombinePath(CUSTOM_SCRIPT_PATH, comboBoxType.Text));

                if (folder.GetFileSystemInfos().Length == 0)
                {
                    DialogResult ans2 = MessageBox.Show(Dictionary.AskForDeleteType,
                             this.Text + " - Info",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (ans2 == DialogResult.Yes)
                    {                        
                        DeleteFolder(CombinePath(CUSTOM_SCRIPT_PATH, comboBoxType.Text), true);
                        radioButtonCustom.Checked = false;
                        radioButtonCustom.Checked = true;
                        return;
                    }
                }

                comboBoxType_SelectedIndexChanged(sender, e);
            }
        }

        private void overwriteAlwaysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("AboutMerge", "Checked", "MergeAlways");
            createDictionary();
        }

        private void askAlwaysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("AboutMerge", "Checked", "AskAlways");
            createDictionary();
        }

        private void renameAlwaysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("AboutMerge", "Checked", "RenameAlways");
            createDictionary();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _tabControl.TabPages.Remove(_tabPage1);
            _tabControl.TabPages.Add(_tabPage4);
            MenuItemSetEnabledValue(false);
        }
        
        private void buttonCloseHelp_Click(object sender, EventArgs e)
        {
            _tabControl.TabPages.Remove(_tabPage4);
            _tabControl.TabPages.Add(_tabPage1);

            MenuItemSetEnabledValue(true);
            
           // this.panelAbout.Size = new Size(0, 0);
           // this.infoTextBox.Visible = true;
        }

              

        private void buttonCancelWiiInfo_Click(object sender, EventArgs e)
        {
            _tabControl.TabPages.Remove(_tabPage2);
            _tabControl.TabPages.Add(_tabPage1);

            MenuItemSetEnabledValue(true);          
        }

        private bool checkMacAdress(char value)
        {
            if ((value != '0') &&
                (value != '1') &&
                (value != '2') &&
                (value != '3') &&
                (value != '4') &&
                (value != '5') &&
                (value != '6') &&
                (value != '7') &&
                (value != '8') &&
                (value != '9') &&
                (value != 'a') &&
                (value != 'b') &&
                (value != 'c') &&
                (value != 'd') &&
                (value != 'e') &&
                (value != 'f'))
                return false;
            else
                return true;
        }
        

        private void LoadWiiInfo(bool onlyForCreateGuide)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            if (!onlyForCreateGuide)
            {
                string macAddress = ini.IniReadValue("WiiInfo", "mac_address");
                
                if (macAddress.Length == 12)
                {
                    macAddress1.Text = macAddress.Substring(0, 2);
                    macAddress2.Text = macAddress.Substring(2, 2);
                    macAddress3.Text = macAddress.Substring(4, 2);
                    macAddress4.Text = macAddress.Substring(6, 2);
                    macAddress5.Text = macAddress.Substring(8, 2);
                    macAddress6.Text = macAddress.Substring(10, 2);
                }

                comboBoxFirmware.Text = "  " + ini.IniReadValue("WiiInfo", "firmware");
                comboBoxRegion.Text = ini.IniReadValue("WiiInfo", "region");

                if (comboBoxFirmware.Text.Trim() != "")
                {
                    checkBoxCreateScript.Visible = true;
                }
            }
            
            if (ini.IniReadValue("WiiInfo", "checkBoxDownload41") == "True")
                checkBoxDownload41.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownload42") == "True")
                checkBoxDownload42.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownload43") == "True")
                checkBoxDownload43.Checked = true;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadActiveIOS") == "True")
                checkBoxDownloadActiveIOS.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadActiveIOS") == "False")
                checkBoxDownloadActiveIOS.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadMoreWADmanager") == "True")
                checkBoxDownloadMoreWADmanager.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadMoreWADmanager") == "False")
                checkBoxDownloadMoreWADmanager.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadGX") == "True")
                checkBoxDownloadGX.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadGX") == "False")
                checkBoxDownloadGX.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadWiiFlow") == "True")
                checkBoxDownloadWiiFlow.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadWiiFlow") == "False")
                checkBoxDownloadWiiFlow.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadCFG") == "True")
                checkBoxDownloadCFG.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadCFG") == "False")
                checkBoxDownloadCFG.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadHackmiiInstallerWAD") == "True")
                checkBoxDownloadHackmiiInstallerWAD.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadHackmiiInstallerWAD") == "False")
                checkBoxDownloadHackmiiInstallerWAD.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadHackmiiInstaller") == "True")
                checkBoxDownloadHackmiiInstaller.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadHackmiiInstaller") == "False")
                checkBoxDownloadHackmiiInstaller.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadCIOS") == "True")
                checkBoxDownloadCIOS.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadCIOS") == "False")
                checkBoxDownloadCIOS.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadIOS236") == "True")
                checkBoxDownloadIOS236.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadIOS236") == "False")
                checkBoxDownloadIOS236.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadOfficialChannel") == "True")
                checkBoxDownloadOfficialChannel.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadOfficialChannel") == "False")
                checkBoxDownloadOfficialChannel.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadPriiloader") == "True")
                checkBoxDownloadPriiloader.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadPriiloader") == "False")
                checkBoxDownloadPriiloader.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxDownloadPatchedSystemIOS") == "True")
                checkBoxDownloadPatchedSystemIOS.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxDownloadPatchedSystemIOS") == "False")
                checkBoxDownloadPatchedSystemIOS.Checked = false;

            if (ini.IniReadValue("WiiInfo", "checkBoxUseWilbrand") == "True")
                checkBoxUseWilbrand.Checked = true;
            else if (ini.IniReadValue("WiiInfo", "checkBoxUseWilbrand") == "False")
                checkBoxUseWilbrand.Checked = false;

            if (!checkBoxUseWilbrand.Checked)
            {
                macAddress1.Text = "";
                macAddress2.Text = "";
                macAddress3.Text = "";
                macAddress4.Text = "";
                macAddress5.Text = "";
                macAddress6.Text = "";
            }

            if (ini.IniReadValue("WiiInfo", "comboBoxAppsForWilbrand") != "")
                comboBoxAppsForWilbrand.Text = ini.IniReadValue("WiiInfo", "comboBoxAppsForWilbrand");
        }


        private bool WiiInfoSave()
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            string exploitFW, exploitRegion, exploitFirmware;
            // check if Mac Address, region and firmware are correct           

            if (macAddress1.Text.Length != 2 ||
                macAddress2.Text.Length != 2 ||
                macAddress3.Text.Length != 2 ||
                macAddress4.Text.Length != 2 ||
                macAddress5.Text.Length != 2 ||
                macAddress6.Text.Length != 2)
            {
                ini.IniWriteValue("WiiInfo", "mac_address", "");
                if (checkBoxUseWilbrand.Checked == true && checkBoxUseWilbrand.Visible == true)
                {
                    this.richTextBoxWiiInfo.Text = Dictionary.NoMac;
                    this.richTextBoxWiiInfo.ForeColor = Color.Red;
                    return false;
                }
            }
            else if (!checkMacAdress(macAddress1.Text[0]) ||
                !checkMacAdress(macAddress1.Text[1]) ||
                !checkMacAdress(macAddress2.Text[0]) ||
                !checkMacAdress(macAddress2.Text[1]) ||
                !checkMacAdress(macAddress3.Text[0]) ||
                !checkMacAdress(macAddress3.Text[1]) ||
                !checkMacAdress(macAddress4.Text[0]) ||
                !checkMacAdress(macAddress4.Text[1]) ||
                !checkMacAdress(macAddress5.Text[0]) ||
                !checkMacAdress(macAddress5.Text[1]) ||
                !checkMacAdress(macAddress6.Text[0]) ||
                !checkMacAdress(macAddress6.Text[1]))
            {
                ini.IniWriteValue("WiiInfo", "mac_address", "");
                if (checkBoxUseWilbrand.Checked == true && checkBoxUseWilbrand.Visible == true)
                {
                    this.richTextBoxWiiInfo.Text = Dictionary.NoMac;
                    this.richTextBoxWiiInfo.ForeColor = Color.Red;
                    return false;
                }
            }
            else
                ini.IniWriteValue("WiiInfo", "mac_address", macAddress1.Text + macAddress2.Text + macAddress3.Text + macAddress4.Text + macAddress5.Text + macAddress6.Text);

            if (comboBoxRegion.Text == "")
            {
                this.richTextBoxWiiInfo.Text = Dictionary.NoRegion;
                this.richTextBoxWiiInfo.ForeColor = Color.Red;
                ini.IniWriteValue("WiiInfo", "exploitFW", "");
                return false;
            }
            if (comboBoxFirmware.Text == "")
            {
                this.richTextBoxWiiInfo.Text = Dictionary.NoFirmware;
                this.richTextBoxWiiInfo.ForeColor = Color.Red;
                ini.IniWriteValue("WiiInfo", "exploitFW", "");
                return false;
            }

            // verify for not existing firmware...                 

            exploitRegion = comboBoxRegion.Text.Substring(1, 1);
            exploitFirmware = comboBoxFirmware.Text.Substring(2, 3);
            exploitFW = exploitFirmware + exploitRegion;

            if ((exploitFW == "3.0K") ||
                (exploitFW == "3.1K") ||
                (exploitFW == "3.2K") ||
                (exploitFW == "3.3K") ||
                (exploitFW == "3.4K") ||
                (exploitFW == "3.5U") ||
                (exploitFW == "3.5E") ||
                (exploitFW == "3.5J") ||
                (exploitFW == "4.0K"))
            {
                this.richTextBoxWiiInfo.Text = Dictionary.NoValidFirmware;
                this.richTextBoxWiiInfo.ForeColor = Color.Red;
                ini.IniWriteValue("WiiInfo", "exploitFW", "");
                return false;
            }

            if (checkBoxCreateScript.Checked == true)
            {
                if (checkBoxDownload41.Checked == false &&
                    checkBoxDownload42.Checked == false &&
                    checkBoxDownload43.Checked == false &&
                    checkBoxUseWilbrand.Checked == false &&
                    checkBoxDownloadHackmiiInstaller.Checked == false &&
                    checkBoxDownloadPatchedSystemIOS.Checked == false &&
                    checkBoxDownloadPriiloader.Checked == false &&
                    checkBoxDownloadOfficialChannel.Checked == false &&
                    checkBoxDownloadIOS236.Checked == false &&
                    checkBoxDownloadCIOS.Checked == false &&
                    checkBoxDownloadHackmiiInstallerWAD.Checked == false &&
                    checkBoxDownloadCFG.Checked == false &&
                    checkBoxDownloadWiiFlow.Checked == false &&
                    checkBoxDownloadGX.Checked == false &&
                    checkBoxDownloadMoreWADmanager.Checked == false &&
                    checkBoxDownloadActiveIOS.Checked == false)
                {
                    this.richTextBoxWiiInfo.Text = Dictionary.NoCheckBox;
                    this.richTextBoxWiiInfo.ForeColor = Color.Red;
                    return false;
                }
            }
           
            ini.IniWriteValue("WiiInfo", "exploitFW", exploitFW);
            ini.IniWriteValue("WiiInfo", "firmware", comboBoxFirmware.Text.Trim());
            ini.IniWriteValue("WiiInfo", "region", comboBoxRegion.Text);
            ini.IniWriteValue("WiiInfo", "checkBoxDownload41", Convert.ToString(checkBoxDownload41.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownload42", Convert.ToString(checkBoxDownload42.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownload43", Convert.ToString(checkBoxDownload43.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadActiveIOS", Convert.ToString(checkBoxDownloadActiveIOS.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadMoreWADmanager", Convert.ToString(checkBoxDownloadMoreWADmanager.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadGX", Convert.ToString(checkBoxDownloadGX.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadWiiFlow", Convert.ToString(checkBoxDownloadWiiFlow.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadCFG", Convert.ToString(checkBoxDownloadCFG.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadHackmiiInstallerWAD", Convert.ToString(checkBoxDownloadHackmiiInstallerWAD.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadHackmiiInstaller", Convert.ToString(checkBoxDownloadHackmiiInstaller.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadCIOS", Convert.ToString(checkBoxDownloadCIOS.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadIOS236", Convert.ToString(checkBoxDownloadIOS236.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadOfficialChannel", Convert.ToString(checkBoxDownloadOfficialChannel.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadPriiloader", Convert.ToString(checkBoxDownloadPriiloader.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxDownloadPatchedSystemIOS", Convert.ToString(checkBoxDownloadPatchedSystemIOS.Checked));
            ini.IniWriteValue("WiiInfo", "checkBoxUseWilbrand", Convert.ToString(checkBoxUseWilbrand.Checked));
            ini.IniWriteValue("WiiInfo", "comboBoxAppsForWilbrand", comboBoxAppsForWilbrand.Text);              

            return true;
        }

        private bool CreateScriptForHack()        {           
            
            FileDelete(CombinePath(STANDARD_SCRIPT_PATH, "Hack My Wii", "Script for hack"));

            if (!Directory.Exists(CombinePath(TEMP_SCRIPT_PATH, "temp_script")))
                Directory.CreateDirectory(CombinePath(TEMP_SCRIPT_PATH, "temp_script"));
            

            IniFile settingsFile = new IniFile(SETTINGS_INI_FILE);

            // serching Firmware and Region
            string exploitFW = settingsFile.IniReadValue("WiiInfo", "exploitFW");
            string region = exploitFW.Substring(3, 1);
            string firmware = exploitFW.Substring(0, 1) + exploitFW.Substring(2, 1);
            
            // Compose a string.
            string firmware_script_lines, move_to_sd_script_lines, download_system_ios_lines;
            string lines = "";

            string hacking_script = "[info]" + "\r\n" +
                            "name=Hack My Wii" + "\r\n" +
                            "source=Combine existing script" + "\r\n" +
                            "description.english=" + Dictionary.HackScriptDisclaimer + "\r\n" +
                            "script_list=";
                          //  "script_list=";



            if (checkBoxDownloadPatchedSystemIOS.Checked == true)
            {
                FileDelete(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Brick protection"));
                FileMove(CombinePath(STANDARD_SCRIPT_PATH, "WAD - IOS", "Brick protection"), CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Brick protection"), false);
                IniFile scriptFile = new IniFile(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Brick protection"));                
                scriptFile.IniWriteValue("info", "copyCiosTo", CombinePath("WAD", "WAD for Modding"));                
                lines = lines + "temp, temp_script, Brick protection;";
            }
            else if (checkBoxDownload41.Checked == true || checkBoxDownload42.Checked == true || checkBoxDownload43.Checked == true)
            {
                string tmp="";
                if (checkBoxDownload41.Checked)
                    tmp = "4.1";
                else if (checkBoxDownload42.Checked)
                    tmp = "4.2";
                if (checkBoxDownload43.Checked)
                    tmp = "4.3";
                
                
                download_system_ios_lines = "[info]" + "\r\n" +
                               "name=" + "system_IOS_for_" + tmp + "\r\n" +
                               "source=Download using ModMii" + "\r\n" +
                               "copyCiosTo=" + CombinePath("WAD", "WAD for Modding") + "\r\n" +
                               "cios_list=";

                if (checkBoxDownload41.Checked)
                    download_system_ios_lines = download_system_ios_lines + "IOS60v16174(IOS60v6174[FS-ES-NP-VP-DIP]).wad				, System menu patched IOS, 60, 60, 16174, IOS60P;";
                else if (checkBoxDownload42.Checked)
                    download_system_ios_lines = download_system_ios_lines + "IOS70v16174(IOS60v6174[FS-ES-NP-VP-DIP]).wad				, System menu patched IOS, 70, 60, 16174, IOS70K;";
                else if (checkBoxDownload43.Checked)
                    download_system_ios_lines = download_system_ios_lines + "IOS80v16174(IOS60v6174[FS-ES-NP-VP-DIP]).wad				, System menu patched IOS, 80, 60, 16174, IOS80K;";

                lines = lines + "temp, temp_script, download_system_ios_script;";

                if (!Directory.Exists(TEMP_SCRIPT_PATH))
                    Directory.CreateDirectory(TEMP_SCRIPT_PATH);               
                
                Directory.CreateDirectory(CombinePath(TEMP_SCRIPT_PATH, "temp_script"));

                FileDelete(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "download_system_ios_script"));

                System.IO.StreamWriter download_system_ios_files = new System.IO.StreamWriter(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "download_system_ios_script"), true, Encoding.Unicode);
                download_system_ios_files.WriteLine(download_system_ios_lines);
                download_system_ios_files.Close();
            }


            if (checkBoxDownloadHackmiiInstaller.Checked == true)
            {
                lines = lines + "Standard, Homebrew - Hacking, Hackmii Installer " + hackmii_installer_version + ";";
                lines = lines + "Standard, Homebrew - Hacking, BootMii SD Files;";
            }

            if (checkBoxDownloadHackmiiInstallerWAD.Checked == true)
                lines = lines + "Standard, WAD - IOS, IOS for Hackmii Installer;";

            if (checkBoxDownloadMoreWADmanager.Checked == true)            
            {
                if(Global.LanguageChoice == "italiano")
                    lines = lines + "Standard, Homebrew - WAD and APPS Manager, WAM.it;";
                else
                    lines = lines + "Standard, Homebrew - WAD and APPS Manager, WAM;";             
            }            
            
            if (checkBoxUseWilbrand.Checked == true)
            {
                lines = lines + "Standard, Wii Exploit, Wilbrand;";

                move_to_sd_script_lines = "[info]" + "\r\n" +
                               "name=" + comboBoxAppsForWilbrand.Text + "\r\n" +
                               "source=Other features" + "\r\n" +
                               "OtherFeatures=moveToRoot" + "\r\n" +                               
                               "fileToTheRoot=";                

                switch (comboBoxAppsForWilbrand.Text)
                {
                    case "Hackmii Installer":
                        move_to_sd_script_lines = move_to_sd_script_lines + CombinePath("apps", "Hackmii Installer", "boot.elf");
                        break;
                    case "MultiModManager":
                        move_to_sd_script_lines = move_to_sd_script_lines + CombinePath("apps", "mmm", "boot.dol");
                        break;
                    case "WAM":
                        move_to_sd_script_lines = move_to_sd_script_lines + CombinePath("apps", "WAM", "boot.dol");
                        break;
                    case "YAWMM":
                        move_to_sd_script_lines = move_to_sd_script_lines + CombinePath("apps", "YAWMM", "boot.dol");
                        break;
                    case "WiiMod":
                        move_to_sd_script_lines = move_to_sd_script_lines + CombinePath("apps", "WiiMod", "boot.elf");
                        break;
                    default: // ossia "- none -"
                        move_to_sd_script_lines = "";
                        break;
                }

                if (move_to_sd_script_lines != "")
                {

                    lines = lines + "temp, temp_script, " + comboBoxAppsForWilbrand.Text + ";";
                    if (!Directory.Exists(TEMP_SCRIPT_PATH))
                        Directory.CreateDirectory(TEMP_SCRIPT_PATH);                    

                    Directory.CreateDirectory(CombinePath(TEMP_SCRIPT_PATH, "temp_script"));

                    FileDelete(CombinePath(TEMP_SCRIPT_PATH, "temp_script", comboBoxAppsForWilbrand.Text));

                    System.IO.StreamWriter move_to_sd_script_files = new System.IO.StreamWriter(CombinePath(TEMP_SCRIPT_PATH, "temp_script", comboBoxAppsForWilbrand.Text), true, Encoding.Unicode);
                    move_to_sd_script_files.WriteLine(move_to_sd_script_lines);
                    move_to_sd_script_files.Close();
                }
            }


            if (checkBoxDownloadActiveIOS.Checked == true)
            {
                FileDelete(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Active IOS"));
                FileMove(CombinePath(STANDARD_SCRIPT_PATH, "WAD - IOS", "Active IOS"), CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Active IOS"), false);
                IniFile scriptFile = new IniFile(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Active IOS"));
                scriptFile.IniWriteValue("info", "copyWadTo", CombinePath("WAD", "WAD for Modding"));
                lines = lines + "temp, temp_script, Active IOS;";
            }

            if (checkBoxDownloadCIOS.Checked == true)
            {
                FileDelete(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Recommended cIOSs"));
                FileMove(CombinePath(STANDARD_SCRIPT_PATH, "WAD - cIOS", "Recommended cIOSs"), CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Recommended cIOSs"), false);
                IniFile scriptFile = new IniFile(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Recommended cIOSs"));
                scriptFile.IniWriteValue("info", "copyCiosTo", CombinePath("WAD", "WAD for Modding"));
                lines = lines + "temp, temp_script, Recommended cIOSs;";                              
            }            
           
            if (checkBoxDownloadIOS236.Checked == true)
            {
                lines = lines + "Standard, Homebrew - Hacking, IOS236 Installer;";
                lines = lines + "Standard, WAD - IOS, IOS36-64-v3351 (for IOS236 Installer);";                
            }            
          
            if (checkBoxDownloadPriiloader.Checked == true)
                lines = lines + "Standard, Homebrew - Hacking, Priiloader;";

            if ((checkBoxDownloadCFG.Checked == true) || (checkBoxDownloadGX.Checked == true) || (checkBoxDownloadWiiFlow.Checked == true))
            {
               
                if (checkBoxDownloadCFG.Checked == true)
                {
                    // prima ero così, ora dalla v 3.2 ho cambiato nel caso escano nuove relase complete e finali (e siano necessari altri script)
                    // lines = lines + "Standard, Homebrew - Loader - CFG, CFG USB Loader (last official version);Standard, Homebrew - Loader - CFG, CFG USB Loader (last featured version);Standard, Homebrew - Loader - CFG, CFG USB Loader (settings for new install);Standard, Homebrew - Loader - CFG, CFG USB Loader (last beta xml);Standard, Homebrew - Loader - CFG, CFG USB Loader (gametdb package);";
                    
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - CFG", "CFG USB Loader (last official version)")))
                        lines = lines + "Standard, Homebrew - Loader - CFG, CFG USB Loader (last official version);";
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - CFG", "CFG USB Loader (last featured version)")))
                        lines = lines + "Standard, Homebrew - Loader - CFG, CFG USB Loader (last featured version);";
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - CFG", "CFG USB Loader (settings for new install)")))
                        lines = lines + "Standard, Homebrew - Loader - CFG, CFG USB Loader (settings for new install);";
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - CFG", "CFG USB Loader (last beta xml)")))
                        lines = lines + "Standard, Homebrew - Loader - CFG, CFG USB Loader (last beta xml);";
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - CFG", "CFG USB Loader (gametdb package)")))
                        lines = lines + "Standard, Homebrew - Loader - CFG, CFG USB Loader (gametdb package);";

                    FileDelete(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "- CFG USB Loader - (FORWARDER)"));
                    FileMove(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - CFG", "- CFG USB Loader - (FORWARDER)"), CombinePath(TEMP_SCRIPT_PATH, "temp_script", "- CFG USB Loader - (FORWARDER)"), false);
                    IniFile forwarderFile = new IniFile(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "- CFG USB Loader - (FORWARDER)"));
                    forwarderFile.IniWriteValue("info", "copyTo", CombinePath("WAD", "WAD for Modding"));
                    lines = lines + "temp, temp_script, - CFG USB Loader - (FORWARDER);";                   
                }

                if (checkBoxDownloadGX.Checked == true)
                {
                    // prima ero così, ora dalla v 3.2 ho cambiato nel caso escano nuove relase complete e finali (e siano necessari altri script)
                    // lines = lines + "Standard, Homebrew - Loader - GX, USB Loader GX (last official version);Standard, Homebrew - Loader - GX, USB Loader GX (last beta version);Standard, Homebrew - Loader - GX, USB Loader GX (last beta xml);Standard, Homebrew - Loader - GX, USB Loader GX (languages);Standard, Homebrew - Loader - GX, USB Loader GX (gametdb package);Standard, Homebrew - Loader - GX, USB Loader GX (settings for new install);";

                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - GX", "USB Loader GX (last official version)")))
                        lines = lines + "Standard, Homebrew - Loader - GX, USB Loader GX (last official version);";
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - GX", "USB Loader GX (last beta version)")))
                        lines = lines + "Standard, Homebrew - Loader - GX, USB Loader GX (last beta version);";
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - GX", "USB Loader GX (last beta xml)")))
                        lines = lines + "Standard, Homebrew - Loader - GX, USB Loader GX (last beta xml);";
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - GX", "USB Loader GX (languages)")))
                        lines = lines + "Standard, Homebrew - Loader - GX, USB Loader GX (languages);";                    
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - GX", "USB Loader GX (settings for new install)")))
                        lines = lines + "Standard, Homebrew - Loader - GX, USB Loader GX (settings for new install);";
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - GX", "USB Loader GX (gametdb package)")))
                        lines = lines + "Standard, Homebrew - Loader - GX, USB Loader GX (gametdb package);";

                    FileDelete(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "- USB Loader GX - (FORWARDER)"));
                    FileMove(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - GX", "- USB Loader GX - (FORWARDER)"), CombinePath(TEMP_SCRIPT_PATH, "temp_script", "- USB Loader GX - (FORWARDER)"), false);
                    IniFile forwarderFile = new IniFile(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "- USB Loader GX - (FORWARDER)"));
                    forwarderFile.IniWriteValue("info", "copyTo", CombinePath("WAD", "WAD for Modding"));
                    lines = lines + "temp, temp_script, - USB Loader GX - (FORWARDER);";
                }

                if (checkBoxDownloadWiiFlow.Checked == true)
                {
                    // prima ero così, ora dalla v 3.2 ho cambiato nel caso escano nuove relase complete e finali (e siano necessari altri script)
                    // lines = lines + "Standard, Homebrew - Loader - WiiFlow, Wiiflow (gametdb package);Standard, Homebrew - Loader - WiiFlow, Wiiflow (last featured version);Standard, Homebrew - Loader - WiiFlow, Wiiflow (settings for new install);Standard, Homebrew - Loader - WiiFlow, Wiiflow (languages);";
                    
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - WiiFlow", "Wiiflow (last featured version)")))
                        lines = lines + "Standard, Homebrew - Loader - WiiFlow, Wiiflow (last featured version);";
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - WiiFlow", "Wiiflow (settings for new install)")))
                        lines = lines + "Standard, Homebrew - Loader - WiiFlow, Wiiflow (settings for new install);";                  
                  // languahes pack is already present in "setting for new install"  
                  //  if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - WiiFlow", "Wiiflow (languages);")))
                  //      lines = lines + "Standard, Homebrew - Loader - WiiFlow, Homebrew - Loader - WiiFlow, Wiiflow (languages);";                    
                    if (File.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - WiiFlow", "Wiiflow (gametdb package)")))
                        lines = lines + "Standard, Homebrew - Loader - WiiFlow, Wiiflow (gametdb package);";

                    FileDelete(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "- Wiiflow - (FORWARDER)"));
                    FileMove(CombinePath(STANDARD_SCRIPT_PATH, "Homebrew - Loader - Wiiflow", "- Wiiflow - (FORWARDER)"), CombinePath(TEMP_SCRIPT_PATH, "temp_script", "- Wiiflow - (FORWARDER)"), false);
                    IniFile forwarderFile = new IniFile(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "- Wiiflow - (FORWARDER)"));
                    forwarderFile.IniWriteValue("info", "copyTo", CombinePath("WAD", "WAD for Modding"));
                    lines = lines + "temp, temp_script, - Wiiflow - (FORWARDER);";
                }

            }
            if (checkBoxDownloadOfficialChannel.Checked == true)
            {
                FileDelete(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Latest official channel"));                
                
                switch (region)
                {
                    
                    case "E":
                        FileMove(CombinePath(STANDARD_SCRIPT_PATH, "WAD - Official Channel", "Latest official channel [EUR]"), CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Latest official channel"), false);                     
                        break;
                    case "U":
                        FileMove(CombinePath(STANDARD_SCRIPT_PATH, "WAD - Official Channel", "Latest official channel [USA]"), CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Latest official channel"), false);                       
                        break;
                    case "K":
                        FileMove(CombinePath(STANDARD_SCRIPT_PATH, "WAD - Official Channel", "Latest official channel [KOR]"), CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Latest official channel"), false);                        
                        break;
                    case "J":
                        FileMove(CombinePath(STANDARD_SCRIPT_PATH, "WAD - Official Channel", "Latest official channel [JAP]"), CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Latest official channel"), false);                        
                        break;
                }
                IniFile scriptFile = new IniFile(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "Latest official channel"));
                scriptFile.IniWriteValue("info", "copyWadTo", CombinePath("WAD", "WAD for Modding"));
                lines = lines + "temp, temp_script, Latest official channel;";

            }

            if ((checkBoxDownload41.Checked == true) || (checkBoxDownload42.Checked == true) || (checkBoxDownload43.Checked == true))
            {
                firmware_script_lines = "[info]" + "\r\n" +
                               "name=" + exploitFW + "\r\n" +
                               "source=Download from NUS" + "\r\n" +
                               "copyWadTo=" + CombinePath("WAD", "WAD for Modding") + "\r\n" +                               
                               "nus_list=";
                string new_firmware;

                if (checkBoxDownload41.Checked == true)
                    new_firmware = "4.1";
                else if (checkBoxDownload42.Checked == true)
                    new_firmware = "4.2";
                else
                    new_firmware = "4.3";

                new_firmware = new_firmware + region;

                switch (new_firmware)
                {
                    case "4.1E":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v450[E]					,RVL-WiiSystemmenu-v450.wad,0000000100000002,450, , , ,System Menuv450;";
                        break;
                    case "4.2E":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v482[E]					,RVL-WiiSystemmenu-v482.wad,0000000100000002,482, , , ,System Menuv482;";
                        break;
                    case "4.3E":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v514[E]					,RVL-WiiSystemmenu-v514.wad,0000000100000002,514, , , ,System Menuv514;";
                        break;

                    case "4.1J":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v448[J]					,RVL-WiiSystemmenu-v448.wad,0000000100000002,448, , , ,System Menuv448;";
                        break;
                    case "4.2J":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v480[J]					,RVL-WiiSystemmenu-v480.wad,0000000100000002,480, , , ,System Menuv480;";
                        break;
                    case "4.3J":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v512[J]					,RVL-WiiSystemmenu-v512.wad,0000000100000002,512, , , ,System Menuv512;";
                        break;

                    case "4.1U":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v449[U]					,RVL-WiiSystemmenu-v449.wad,0000000100000002,449, , , ,System Menuv449;";
                        break;
                    case "4.2U":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v481[U]					,RVL-WiiSystemmenu-v481.wad,0000000100000002,481, , , ,System Menuv481;";
                        break;
                    case "4.3U":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v513[U]					,RVL-WiiSystemmenu-v513.wad,0000000100000002,513, , , ,System Menuv513;";
                        break;

                    case "4.1K":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v454[K]					,RVL-WiiSystemmenu-v454.wad,0000000100000002,454, , , ,System Menuv454;";
                        break;
                    case "4.2K":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v486[K]					,RVL-WiiSystemmenu-v486.wad,0000000100000002,486, , , ,System Menuv486;";
                        break;
                    case "4.3K":
                        firmware_script_lines = firmware_script_lines + "RVL-WiiSystemmenu-v518[K]					,RVL-WiiSystemmenu-v518.wad,0000000100000002,518, , , ,System Menuv518;";
                        break;

                }

                lines = lines + "temp, temp_script, firmware_script;";

                if (!Directory.Exists(TEMP_SCRIPT_PATH))
                    Directory.CreateDirectory(TEMP_SCRIPT_PATH);                

                Directory.CreateDirectory(CombinePath(TEMP_SCRIPT_PATH, "temp_script"));

                FileDelete(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "firmware_script"));

                System.IO.StreamWriter firmware_script_file = new System.IO.StreamWriter(CombinePath(TEMP_SCRIPT_PATH, "temp_script", "firmware_script"), true, Encoding.Unicode);
                firmware_script_file.WriteLine(firmware_script_lines);
                firmware_script_file.Close();                
                
            }

            if (lines == "")
            {
                if (Directory.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Hacking")))                
                    DeleteFolder(CombinePath(STANDARD_SCRIPT_PATH, "Hacking"), true );                    
                
                return false;
            }
            else
            {
                hacking_script = hacking_script + lines;

                if(!Directory.Exists(CombinePath(STANDARD_SCRIPT_PATH, "Hacking")))
                    Directory.CreateDirectory(CombinePath(STANDARD_SCRIPT_PATH, "Hacking"));

                FileDelete(CombinePath(STANDARD_SCRIPT_PATH, "Hacking", "Hack My Wii"));

                System.IO.StreamWriter file = new System.IO.StreamWriter(CombinePath(STANDARD_SCRIPT_PATH, "Hacking", "Hack My Wii"), true, Encoding.Unicode);
                
                file.WriteLine(hacking_script);
                file.Close();
                return true;
            }
        }

        private void buttonSaveWiiInfo_Click(object sender, EventArgs e)
        {  
            if (WiiInfoSave())
            {
                _tabControl.TabPages.Remove(_tabPage2);
                _tabControl.TabPages.Add(_tabPage1); 
                
                string message = "";
                      
                if (checkBoxCreateScript.Checked == true)
                {
                    if (CreateScriptForHack())
                    {
                        message = Dictionary.scriptCreated;

                        if (radioButtonStandard.Checked == true)
                        {
                            Global.group = "Standard";
                            comboBoxGroupUpdate();
                        }
                        else
                            radioButtonStandard.Checked = true;
                    }
                }
                else
                    message = Dictionary.wiiInfoSaves;                 
                
                MenuItemSetEnabledValue(true);                        
               
                string HTMLtutorialCreated = CombinePath(STARTUP_PATH, "tutorial.html");
                FileDelete(HTMLtutorialCreated);

                if(message != "")
                    MessageBox.Show(message,
                              this.Text + " - Info",
                             MessageBoxButtons.OK, MessageBoxIcon.Information);  
            }
        }

        private bool check_mac_for_wilbrand()
        {
            if ((macAddress1.Text.Trim() == "") &&
                 (macAddress2.Text.Trim() == "") &&
                 (macAddress3.Text.Trim() == "") &&
                 (macAddress4.Text.Trim() == "") &&
                 (macAddress5.Text.Trim() == "") &&
                 (macAddress6.Text.Trim() == ""))
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        private void macAddress1_TextChanged(object sender, EventArgs e)
        {
            if (macAddress1.TextLength == 2)
                macAddress2.Focus();
            
            reloadItemsForWilbrand();
        }

        private void macAddress2_TextChanged(object sender, EventArgs e)
        {
            if (macAddress2.TextLength == 2)
                macAddress3.Focus();
                              
            reloadItemsForWilbrand();
        }

        private void macAddress3_TextChanged(object sender, EventArgs e)
        {
            if (macAddress3.TextLength == 2)
                macAddress4.Focus();            
                
            reloadItemsForWilbrand();
        }

        private void macAddress4_TextChanged(object sender, EventArgs e)
        {
            if (macAddress4.TextLength == 2)
                macAddress5.Focus();           
                
            reloadItemsForWilbrand();
        }

        private void macAddress5_TextChanged(object sender, EventArgs e)
        {
            if (macAddress5.TextLength == 2)
                macAddress6.Focus();            
                
            reloadItemsForWilbrand();
        }

        private void macAddress6_TextChanged(object sender, EventArgs e)
        {
            if (macAddress6.Text.Contains(" "))
                macAddress6.Text = "";
            reloadItemsForWilbrand();
        }   

        private void checkBoxCreateScript_CheckedChanged(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            Global.LanguageChoice = ini.IniReadValue("Language", "LanguageChoice");

            IniFile dictionary = new IniFile(CombinePath(LANGUAGES_PATH, Global.LanguageChoice + ".ini"));
            
            if (checkBoxCreateScript.Checked == true)
            {
                checkBoxDownload41.Visible = true;
                checkBoxDownload42.Visible = true;
                checkBoxDownload43.Visible = true;
                checkBoxDownloadOfficialChannel.Visible = true;
                checkBoxDownloadPatchedSystemIOS.Visible = true;
                checkBoxDownloadActiveIOS.Visible = true;
                checkBoxDownloadCIOS.Visible = true;
                checkBoxDownloadIOS236.Visible = true;
           //     checkBoxDownloadMoreWADmanager.Visible = true;
                checkBoxDownloadPriiloader.Visible = true;
                checkBoxDownloadGX.Visible = true;
                checkBoxDownloadWiiFlow.Visible = true;
                checkBoxDownloadCFG.Visible = true;
                checkBoxDownloadHackmiiInstaller.Visible = true;
                checkBoxDownloadHackmiiInstallerWAD.Visible = true;
                comboBoxAppsForWilbrand.Visible = true;
                checkBoxUseWilbrand.Visible = true;                
                buttonHelp.Visible = true;
                linkLabelDefaultSettings.Visible = true;
                if(dictionary.IniReadValue("menu", "saveAndCreateScript") != "")
                    buttonSaveWiiInfo.Text = dictionary.IniReadValue("menu", "saveAndCreateScript");                
                else
                    buttonSaveWiiInfo.Text = "Save and create script";

            }
            else
            {
                checkBoxDownload41.Visible = false;
                checkBoxDownload42.Visible = false;
                checkBoxDownload43.Visible = false;
                checkBoxDownloadOfficialChannel.Visible = false;
                checkBoxDownloadPatchedSystemIOS.Visible = false;
                checkBoxDownloadActiveIOS.Visible = false;
                checkBoxDownloadCIOS.Visible = false;
                checkBoxDownloadIOS236.Visible = false;
                checkBoxDownloadMoreWADmanager.Visible = false;
                checkBoxDownloadPriiloader.Visible = false;
                checkBoxDownloadGX.Visible = false;
                checkBoxDownloadWiiFlow.Visible = false;
                checkBoxDownloadCFG.Visible = false;
                checkBoxDownloadHackmiiInstaller.Visible = false;
                checkBoxDownloadHackmiiInstallerWAD.Visible = false;
                comboBoxAppsForWilbrand.Visible = false;
                checkBoxUseWilbrand.Visible = false;
                buttonHelp.Visible = false;
                linkLabelDefaultSettings.Visible = false;
                if (dictionary.IniReadValue("menu", "save") != "")
                    buttonSaveWiiInfo.Text = dictionary.IniReadValue("menu", "save");
                else
                    buttonSaveWiiInfo.Text = "Save";                
            }
                      
        }

        private void comboBoxFirmware_SelectedIndexChanged(object sender, EventArgs e)
        {
            string fw = comboBoxFirmware.Text.Trim();
            fw = fw.Substring(0,1) + fw.Substring(2,1);
            int firmware = Convert.ToInt32(fw);           

            if(firmware < 41)            
                checkBoxDownload41.Checked = true;              
    
            else
            {
                checkBoxDownload41.Checked = false;
                checkBoxDownload42.Checked = false;
                checkBoxDownload43.Checked = false;                
            }       
            
            //if(firmware == 43)
            //    checkBoxDownloadHackmiiInstallerWAD.Checked = false;

            if (comboBoxRegion.Text != "")
            {
                checkBoxCreateScript.Visible = true;
                if (checkBoxCreateScript.Checked)
                {
                    checkBoxCreateScript.Checked = false;
                    checkBoxCreateScript.Checked = true;
                }
            }
        }

        private void checkBoxDownload41_CheckedChanged(object sender, EventArgs e)
        {
            if (firmwareChaghed == false)
                return;

            firmwareChaghed = false;

            string fw = comboBoxFirmware.Text;            
            fw = fw.Trim();
            fw = fw.Substring(0,1) + fw.Substring(2,1);
            int firmware = Convert.ToInt32(fw);

            if (checkBoxDownload41.Checked == true)
            {
                checkBoxDownload42.Checked = false;
                checkBoxDownload43.Checked = false;
            }
            else if (checkBoxDownload41.Checked == false && firmware < 41)            
                checkBoxDownload41.Checked = true;

            if (checkBoxDownload41.Checked || checkBoxDownload42.Checked || checkBoxDownload43.Checked)
            {
                checkBoxDownloadPriiloader.Checked = true;
                checkBoxDownloadMoreWADmanager.Checked = true;
                checkBoxDownloadIOS236.Checked = true;
                checkBoxDownloadPatchedSystemIOS.Checked = true;
            }

            WAM_check();

            firmwareChaghed = true;                           
        }

        private void checkBoxDownload42_CheckedChanged(object sender, EventArgs e)
        {
            if (firmwareChaghed == false)
                return;

            firmwareChaghed = false;
            
            string fw = comboBoxFirmware.Text;
            fw = fw.Trim();
            fw = fw.Substring(0, 1) + fw.Substring(2, 1);
            int firmware = Convert.ToInt32(fw);

            if (checkBoxDownload42.Checked == true)
            {
                checkBoxDownload41.Checked = false;
                checkBoxDownload43.Checked = false;
            }
            else if (checkBoxDownload42.Checked == false && firmware < 41)
                checkBoxDownload42.Checked = true;

            if (checkBoxDownload41.Checked || checkBoxDownload42.Checked || checkBoxDownload43.Checked)
            {
                checkBoxDownloadPriiloader.Checked = true;
                checkBoxDownloadMoreWADmanager.Checked = true;
                checkBoxDownloadIOS236.Checked = true;
                checkBoxDownloadPatchedSystemIOS.Checked = true;
            }

            WAM_check();

            firmwareChaghed = true;
        }

        private void checkBoxDownload43_CheckedChanged(object sender, EventArgs e)
        {
            if (firmwareChaghed == false)
                return;

            firmwareChaghed = false;

            string fw = comboBoxFirmware.Text;
            fw = fw.Trim();
            fw = fw.Substring(0, 1) + fw.Substring(2, 1);
            int firmware = Convert.ToInt32(fw);

            if (checkBoxDownload43.Checked == true)
            {
                checkBoxDownload41.Checked = false;
                checkBoxDownload42.Checked = false;
            }
            else if (checkBoxDownload43.Checked == false && firmware < 41)
                checkBoxDownload43.Checked = true;

            if (checkBoxDownload41.Checked || checkBoxDownload42.Checked || checkBoxDownload43.Checked)
            {
                checkBoxDownloadPriiloader.Checked = true;                
                checkBoxDownloadMoreWADmanager.Checked = true;                
                checkBoxDownloadIOS236.Checked = true;                
                checkBoxDownloadPatchedSystemIOS.Checked = true;
            }

            WAM_check();

            firmwareChaghed = true;
        }

        private void reloadItemsForWilbrand()
        {
            comboBoxAppsForWilbrand.Items.Clear();

            if ( (checkBoxDownloadHackmiiInstallerWAD.Checked == true) && (checkBoxDownloadMoreWADmanager.Checked == true) )
                comboBoxAppsForWilbrand.Items.Add("WAM");              
            if (checkBoxDownloadHackmiiInstaller.Checked == true)
                comboBoxAppsForWilbrand.Items.Add("Hackmii Installer");
            if ((checkBoxDownloadHackmiiInstallerWAD.Checked != true) && (checkBoxDownloadMoreWADmanager.Checked == true))
                comboBoxAppsForWilbrand.Items.Add("WAM");

            if (checkBoxDownloadHackmiiInstaller.Checked)
            {
                checkBoxDownloadHackmiiInstallerWAD.Enabled = true;
            }
            else
            {
                checkBoxDownloadHackmiiInstallerWAD.Checked = false;
                checkBoxDownloadHackmiiInstallerWAD.Enabled = false;
            }

            if (comboBoxAppsForWilbrand.Items.Count == 0)
            {
                comboBoxAppsForWilbrand.Enabled = false;
                checkBoxUseWilbrand.Checked = false;
                checkBoxUseWilbrand.Enabled = false;
            }
            else
            {          
                comboBoxAppsForWilbrand.SelectedIndex = 0;
                checkBoxUseWilbrand.Enabled = check_mac_for_wilbrand();
                checkBoxUseWilbrand.Checked = check_mac_for_wilbrand();
                comboBoxAppsForWilbrand.Enabled = check_mac_for_wilbrand();
            }                           
        }
        

        private void checkBoxDownloadHackmiiInstaller_CheckedChanged(object sender, EventArgs e)
        {
            reloadItemsForWilbrand();            
        }

        private void checkBoxDownloadMoreWADmanager_CheckedChanged(object sender, EventArgs e)
        {
            reloadItemsForWilbrand();
        }

        private void checkBoxUseWilbrand_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxUseWilbrand.Checked)
                comboBoxAppsForWilbrand.Enabled = true;
            else
                comboBoxAppsForWilbrand.Enabled = false;
        }

        private void comboBoxAppsForWilbrand_SelectedIndexChanged(object sender, EventArgs e)
        {
         
            if ( comboBoxAppsForWilbrand.Text == "WAM") 
            {
                if (checkBoxDownloadHackmiiInstaller.Checked)                
                    checkBoxDownloadHackmiiInstallerWAD.Checked = true;
                else                
                    checkBoxDownloadHackmiiInstallerWAD.Checked = false;
            }           
            else            
                checkBoxDownloadHackmiiInstallerWAD.Checked = false;           
        }

        private void linkLabelFW_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.youtube.com/watch?v=s9WFpm--iTg&feature=player_embedded");            
        }

        private void linkLabelMAC_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.youtube.com/watch?v=6Pc8L-ARa1M&feature=player_embedded");         
        }  

        private void MenuItemSetEnabledValue(bool value)
        {
            menuStrip1.Enabled = value;
        }

        private void freeCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult myDialogResult = MessageBox.Show(Dictionary.DeleteCacheFiles, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (myDialogResult == DialogResult.No)
                return;           

            this.Enabled = false;
            string oldInfoTextBox = this.infoTextBox.Text;
            infoTextBox.Clear();
            AppendText(Dictionary.DeletingCacheFiles);
            deleteAllFile(CACHE_PATH, "", true);
            deleteAllFile(MOD_MII_OUTPUT_PATH, "", true);
            deleteAllFile(MOD_MII_TEMP_PATH, "", true);
            AppendText("..OK!\n");
            MySleep(2000);
            this.infoTextBox.Text = oldInfoTextBox;
            this.Enabled = true;

        }

        private void enableCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("cache", "enableCache", "True");
            readSettingsFile();
        }


        private void disableCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("cache", "enableCache", "False");
            readSettingsFile();
        }            

        private void checkForInonImages()
        {                   
            if (!File.Exists(CombinePath(TUTORIAL_IMAGES_PATH, "pad.png")))
                image_PAD_Path = "";
            else
                image_PAD_Path = "Database\\tutorial\\images\\" + "pad.png";

            if (!File.Exists(CombinePath(TUTORIAL_IMAGES_PATH, "piu.png")))
                image_PLUS_Path = "";
            else
                image_PLUS_Path = "Database\\tutorial\\images\\" + "piu.png";

            if (!File.Exists(CombinePath(TUTORIAL_IMAGES_PATH, "meno.png")))
                image_MINUS_Path = "";
            else
                image_MINUS_Path = "Database\\tutorial\\images\\" + "meno.png";

            if (!File.Exists(CombinePath(TUTORIAL_IMAGES_PATH, "1.png")))
                image_ONE_Path = "";
            else
                image_ONE_Path = "Database\\tutorial\\images\\" + "1.png";

            if (!File.Exists(CombinePath(TUTORIAL_IMAGES_PATH, "A.png")))
                image_A_Path = "";
            else
                image_A_Path = "Database\\tutorial\\images\\" + "A.png";

            if (!File.Exists(CombinePath(TUTORIAL_IMAGES_PATH, "B.png")))
                image_B_Path = "";
            else
                image_B_Path = "Database\\tutorial\\images\\" + "B.png";

            if (!File.Exists(CombinePath(TUTORIAL_IMAGES_PATH, "home.png")))
                image_HOME_Path = "";
            else
                image_HOME_Path = "Database\\tutorial\\images\\" + "home.png";


        }

        private string ReadTutorialFile(string tutorialFile, string section, bool writeGuide)
        {
            StreamReader fileToRead = null;
           
            string HTMLtutorialCreated = CombinePath(STARTUP_PATH, "tutorial.html");           
            StreamWriter HTMLfileToWrite = new System.IO.StreamWriter(HTMLtutorialCreated, true, Encoding.Unicode);
                       
            string line;
            string message="";
            bool sectionFound = false, fisrtLine = true, parag1 = false, parag2 = false;

            fileToRead = new StreamReader(tutorialFile);

            if (fileToRead == null)
                return "";

            if (writeGuide)
            {                
                if (section == "Close_2UL")                                                   
                      HTMLfileToWrite.WriteLine("</UL></UL>\n");                 
                else if (section == "Close_1UL")                                                     
                    HTMLfileToWrite.WriteLine("</UL>\n");
                else if (section != "G12" && section != "G13" && section != "G15" && section != "G16" && section != "G17" && section != "G18" && section != "G22" && section != "G23" && section != "G24" && section != "G25")
                    HTMLfileToWrite.WriteLine("<UL>\n");                
            }

            while ((line = fileToRead.ReadLine()) != null)
            {
                if ( (section == "Close_1UL") || (section == "Close_2UL") )
                    break;

                if (sectionFound == false)
                {
                    if (line.Contains("[#" + section + "]"))
                    {
                        sectionFound = true;                        
                        continue;
                    }
                }

                if (line.Contains("[/#" + section + "]"))
                    break;

                if(sectionFound==false)
                    continue;

                if (writeGuide)
                {                 
                    if (line.Contains("[#WAD_LIST#]"))
                    {
                        if (comboBoxWADList.Items.Count != 0)
                        {
                            //comboBoxWADList.Sorted = true;
                            foreach (var item in comboBoxWADList.Items)
                                HTMLfileToWrite.WriteLine(item + "<BR>\n");
                        }
                    }
                    else if (line.Contains("[#IMG="))
                    {
                        string imagePath="";
                        string image;

                        image = line.Trim();
                        image = image.Replace("[#IMG=", "");
                        image = image.Replace("#]", "");                       

                        if (image == "")
                            continue;

                        if (!File.Exists(CombinePath(TUTORIAL_IMAGES_PATH, image)))
                            continue;                       

                        imagePath = "Database\\tutorial\\images\\" + image;

                        HTMLfileToWrite.WriteLine("<img src=\""+ imagePath +"\"><BR>\n");                         
                    }
                    else
                    {
                        if (line.Trim() != "")
                        {
                            if (line.Contains("[PAD]") || line.Contains("[A]") || line.Contains("[B]") || line.Contains("[HOME]") || line.Contains("[+]") || line.Contains("[-]") || line.Contains("[1]"))
                            {
                                if (image_PAD_Path != "")
                                    line = line.Replace("[PAD]", "<img src=\"" + image_PAD_Path + "\">");
                                if (image_PLUS_Path != "")
                                    line = line.Replace("[+]", "<img src=\"" + image_PLUS_Path + "\">");
                                if (image_MINUS_Path != "")
                                    line = line.Replace("[-]", "<img src=\"" + image_MINUS_Path + "\">");
                                if (image_ONE_Path != "")
                                    line = line.Replace("[1]", "<img src=\"" + image_ONE_Path + "\">");
                                if (image_B_Path != "")
                                    line = line.Replace("[B]", "<img src=\"" + image_B_Path + "\">");
                                if (image_A_Path != "")
                                    line = line.Replace("[A]", "<img src=\"" + image_A_Path + "\">");
                                if (image_HOME_Path != "")
                                    line = line.Replace("[HOME]", "<img src=\"" + image_HOME_Path + "\">");
                            }

                            if (fisrtLine && !line.Contains("--") && !line.Contains("-__-"))
                                HTMLfileToWrite.WriteLine("<b>");

                            if (line.Contains("----") && !parag1)
                            {
                                HTMLfileToWrite.WriteLine("<UL>\n");
                                parag1 = true;
                            }

                            if (line.Contains("------") && !parag2)
                            {
                                HTMLfileToWrite.WriteLine("<UL>\n");
                                parag2 = true;
                            }

                            if (!line.Contains("----") && parag1)
                            {
                                HTMLfileToWrite.WriteLine("</UL>\n");
                                parag1 = false;
                            }

                            if (!line.Contains("------") && parag2)
                            {
                                HTMLfileToWrite.WriteLine("</UL>\n");
                                parag2 = false;
                            }

                            if (line.Contains("--") || line.Contains("-__-"))
                                HTMLfileToWrite.WriteLine("<LI>");

                            if (line.Contains("--"))
                                HTMLfileToWrite.WriteLine(line.Replace("--", ""));
                            else if (line.Contains("----"))
                                HTMLfileToWrite.WriteLine(line.Replace("----", ""));
                            else if (line.Contains("------"))
                                HTMLfileToWrite.WriteLine(line.Replace("------", ""));
                            else if (line.Contains("-__-"))
                                HTMLfileToWrite.WriteLine(line.Replace("-__-", ""));
                            else
                                HTMLfileToWrite.WriteLine(line);

                            if (line.Contains("--") || line.Contains("-__-"))
                                HTMLfileToWrite.WriteLine("</LI>");

                            if (fisrtLine && !line.Contains("--") && !line.Contains("-__-"))
                                HTMLfileToWrite.WriteLine("</b>");

                            if (!fisrtLine && !line.Contains("--") && !line.Contains("-__-"))
                                HTMLfileToWrite.WriteLine("<BR>\n");

                            if (fisrtLine && !line.Contains("--") && !line.Contains("-__-"))
                                HTMLfileToWrite.WriteLine("<BR><UL>\n");                          
                                
                            fisrtLine = false;
                        }
                    }
                }
                else
                    message = message + line + "\n";

            }

            // close section
            if (writeGuide)
            {
                if ((section != "Close_1UL") && (section != "Close_2UL"))
                {
                    if (section != "G1" && section != "G14" && section != "G15" && section != "G16" && section != "G17" && section != "G18" && section != "G21" && section != "G22" && section != "G23" && section != "G24" && section != "G25")                                            
                        HTMLfileToWrite.WriteLine("</UL></UL>\n");                    
                    if (section == "G14")                                          
                        HTMLfileToWrite.WriteLine("<UL>\n");                    
                    if (section == "G21")
                        HTMLfileToWrite.WriteLine("<UL>\n");                    
                }
            }

            if (fileToRead != null)
                fileToRead.Close();           

            if (HTMLfileToWrite != null)
                HTMLfileToWrite.Close();   

            return message;
        }

        private void newStepInGuide()
        {
            stepNumber++;
            
            string HTMLtutorialCreated = CombinePath(STARTUP_PATH, "tutorial.html");            
            StreamWriter HTMLfileToWrite = new System.IO.StreamWriter(HTMLtutorialCreated, true, Encoding.Unicode);
                        
            HTMLfileToWrite.WriteLine("<HR WIDTH=\"100%\">\n" +
                                      "<UL><FONT SIZE=+1><b>" + "Step n° " + stepNumber + "</b></FONT></UL>\n");            

            if (HTMLfileToWrite != null)
                HTMLfileToWrite.Close(); 
        }



        private void firstLineInGuide()
        {            
            string HTMLtutorialCreated = CombinePath(STARTUP_PATH, "tutorial.html");
            
            StreamWriter HTMLfileToWrite = new System.IO.StreamWriter(HTMLtutorialCreated, true, Encoding.Unicode);            

            HTMLfileToWrite.WriteLine ("<HTML>\n" + 
                                       "<HEAD>\n" +
                                       "<TITLE>WiiDownloader Tutorial</TITLE>\n" +
                                       "</HEAD>\n" +
                                       "<BODY>\n" +                                       
                                       "<FONT SIZE=+2>\n" +
                                       "<HR WIDTH=\"100%\">\n" +
                                       "<HR WIDTH=\"100%\">\n" +
                                       "<UL>\n" +
                                       "Guide created by WiiDownloader on " + DateTime.Now + "\n" +                                      
                                       "</UL>\r\n" +
                                       "<HR WIDTH=\"100%\">\n" +                                      
                                       "</FONT>\n"+
                                       "<tt>\n");

            if (HTMLfileToWrite != null)
                HTMLfileToWrite.Close(); 
        }

        private void CloseHTMLFileInGuide()
        {
            
            string HTMLtutorialCreated = CombinePath(STARTUP_PATH, "tutorial.html");
            
            StreamWriter HTMLfileToWrite = new System.IO.StreamWriter(HTMLtutorialCreated, true, Encoding.Unicode);



            HTMLfileToWrite.WriteLine("</tt>\r\n" +
                                      "</BODY>\r\n" +
                                      "</HTML>\r\n");           
           

            if (HTMLfileToWrite != null)
                HTMLfileToWrite.Close();
        }

        
        private void createGuide()        
        {            
            string HTMLtutorialCreated = CombinePath(STARTUP_PATH, "tutorial.html");            
            FileDelete(HTMLtutorialCreated);              

            string tutorialFile = CombinePath(TUTORIAL_PATH, Global.LanguageChoice + ".txt");

            stepNumber = 0;

            if (File.Exists(tutorialFile))
            {
                LoadWiiInfo(true);

                checkForInonImages();

                firstLineInGuide();

                if (!checkBoxCopyToSD.Checked == true)
                {
                    newStepInGuide();
                    ReadTutorialFile(tutorialFile, "G0", true);
                }

                if (checkBoxUseWilbrand.Checked)
                {
                    newStepInGuide();
                    ReadTutorialFile(tutorialFile, "G1", true);
                    if (comboBoxAppsForWilbrand.Text == "WAM")
                    {
                        ReadTutorialFile(tutorialFile, "G12", true);
                        if (checkBoxDownloadHackmiiInstallerWAD.Checked)
                        {
                            newStepInGuide();
                            ReadTutorialFile(tutorialFile, "G2", true);
                            newStepInGuide();
                            ReadTutorialFile(tutorialFile, "G3", true);
                            newStepInGuide();
                            ReadTutorialFile(tutorialFile, "G4", true);
                        }
                    }
                    else
                    {
                        ReadTutorialFile(tutorialFile, "G13", true);
                        newStepInGuide();
                        ReadTutorialFile(tutorialFile, "G4", true);
                    }
                }
                else
                {
                    if (checkBoxDownloadHackmiiInstallerWAD.Checked)
                    {
                        newStepInGuide();
                        ReadTutorialFile(tutorialFile, "G11", true);
                        newStepInGuide();
                        ReadTutorialFile(tutorialFile, "G2", true);
                        newStepInGuide();
                        ReadTutorialFile(tutorialFile, "G3", true);
                        newStepInGuide();
                        ReadTutorialFile(tutorialFile, "G4", true);

                    }
                    else if (checkBoxDownloadHackmiiInstaller.Checked)
                    {
                        newStepInGuide();
                        ReadTutorialFile(tutorialFile, "G10", true);
                        newStepInGuide();
                        ReadTutorialFile(tutorialFile, "G4", true);
                    }
                }

                if (checkBoxDownloadHackmiiInstaller.Checked)
                {
                    newStepInGuide();
                    ReadTutorialFile(tutorialFile, "G5", true);
                }

                if (checkBoxDownloadIOS236.Checked)
                {
                    newStepInGuide();
                    ReadTutorialFile(tutorialFile, "G6", true);
                }

                if (!checkBoxDownload41.Checked && !checkBoxDownload42.Checked && !checkBoxDownload43.Checked)
                {
                    if (checkBoxDownloadPriiloader.Checked)
                    {
                        newStepInGuide();
                        ReadTutorialFile(tutorialFile, "G8", true);
                    }
                }

                if (    (checkBoxDownload41.Checked) ||
                        (checkBoxDownload42.Checked) ||
                        (checkBoxDownload43.Checked) ||
                        (checkBoxDownloadPatchedSystemIOS.Checked) ||
                        (checkBoxDownloadActiveIOS.Checked) ||
                        (checkBoxDownloadCIOS.Checked) ||
                        (checkBoxDownloadOfficialChannel.Checked) ||
                        (checkBoxDownloadCFG.Checked) ||
                        (checkBoxDownloadGX.Checked) ||
                        (checkBoxDownloadWiiFlow.Checked))
                {
                    newStepInGuide();
                    ReadTutorialFile(tutorialFile, "G7", true);
                }


                if (checkBoxDownload41.Checked || checkBoxDownload42.Checked || checkBoxDownload43.Checked)
                {
                    if (checkBoxDownloadPriiloader.Checked)
                    {
                        newStepInGuide();
                        ReadTutorialFile(tutorialFile, "G8", true);
                    }
                }

                if (checkBoxDownloadHackmiiInstaller.Checked)
                {
                    newStepInGuide();
                    ReadTutorialFile(tutorialFile, "G9", true);
                }

                if ((checkBoxDownloadHackmiiInstaller.Checked) ||
                    (checkBoxDownload41.Checked) ||
                    (checkBoxDownload42.Checked) ||
                    (checkBoxDownload43.Checked) ||
                    (checkBoxDownloadPatchedSystemIOS.Checked) ||
                    (checkBoxDownloadIOS236.Checked) ||
                    (checkBoxDownloadActiveIOS.Checked) ||
                    (checkBoxDownloadCIOS.Checked) ||
                    (checkBoxDownloadHackmiiInstallerWAD.Checked) ||
                    (checkBoxDownloadOfficialChannel.Checked) ||
                    (checkBoxDownloadCFG.Checked) ||
                    (checkBoxDownloadGX.Checked) ||
                    (checkBoxDownloadWiiFlow.Checked))
                {
                    newStepInGuide();                    
                    ReadTutorialFile(tutorialFile, "G14", true);

                    if (checkBoxDownloadHackmiiInstaller.Checked)
                        ReadTutorialFile(tutorialFile, "G15", true);

                    if (checkBoxDownloadIOS236.Checked)
                        ReadTutorialFile(tutorialFile, "G16", true);

                    if ((checkBoxDownload41.Checked) ||
                        (checkBoxDownload42.Checked) ||
                        (checkBoxDownload43.Checked) ||
                        (checkBoxDownloadPatchedSystemIOS.Checked) ||
                        (checkBoxDownloadActiveIOS.Checked) ||
                        (checkBoxDownloadCIOS.Checked) ||
                        (checkBoxDownloadOfficialChannel.Checked) ||
                        (checkBoxDownloadCFG.Checked) ||
                        (checkBoxDownloadGX.Checked) ||
                        (checkBoxDownloadWiiFlow.Checked))
                            ReadTutorialFile(tutorialFile, "G17", true);

                    if (checkBoxDownloadHackmiiInstallerWAD.Checked)
                        ReadTutorialFile(tutorialFile, "G18", true);

                    ReadTutorialFile(tutorialFile, "Close_2UL", true);

                    if ( (checkBoxDownloadHackmiiInstaller.Checked) ||
                         (checkBoxUseWilbrand.Checked) ||
                         (checkBoxDownloadIOS236.Checked) )
                    {                        
                        ReadTutorialFile(tutorialFile, "G21", true);

                        if(checkBoxDownloadHackmiiInstaller.Checked)
                            ReadTutorialFile(tutorialFile, "G22", true);                       

                        if (checkBoxUseWilbrand.Checked)
                        {
                            if (comboBoxAppsForWilbrand.Text == "WAM")
                                ReadTutorialFile(tutorialFile, "G23", true);
                            else
                                ReadTutorialFile(tutorialFile, "G24", true);
                        }

                        if (checkBoxDownloadIOS236.Checked)
                            ReadTutorialFile(tutorialFile, "G25", true);

                        ReadTutorialFile(tutorialFile, "Close_2UL", true);
                    }

                    ReadTutorialFile(tutorialFile, "Close_1UL", true);                                        

                }

                if ((checkBoxDownloadCFG.Checked) ||
                    (checkBoxDownloadGX.Checked) ||
                    (checkBoxDownloadWiiFlow.Checked))
                {
                    newStepInGuide();
                    ReadTutorialFile(tutorialFile, "G19", true);
                }

                newStepInGuide();
                ReadTutorialFile(tutorialFile, "G20", true);               

                CloseHTMLFileInGuide();

             //   Process.Start(tutorialCreated);
                Process.Start(HTMLtutorialCreated);

            }
        }


        private void buttonHelp_Click(object sender, EventArgs e)
        {
            string tutorialFile = CombinePath(TUTORIAL_PATH, Global.LanguageChoice + ".txt");
            HelpPage = 1;

            if (!File.Exists(tutorialFile))
            {
                MessageBox.Show(Global.LanguageChoice + ".txt not found in 'tutorial' folder!", "WiiDownloader", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                labelHelpTitle.Text = ReadTutorialFile(tutorialFile, "H1", false);
                richTextBoxHelpText.Text = ReadTutorialFile(tutorialFile, "H2", false);

                _tabControl.TabPages.Remove(_tabPage2);
                _tabControl.TabPages.Add(_tabPage3);

                MenuItemSetEnabledValue(true);

               // panelHelp.Size = new Size(590, 590);
               // this.panelWiiInfo.Size = new Size(0, 0);
              //  infoTextBox.Visible = false;             
            }
        }

        

        private void modYourWiiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            _tabControl.TabPages.Remove(_tabPage1);
            _tabControl.TabPages.Add(_tabPage2);

            MenuItemSetEnabledValue(false);
            
            richTextBoxWiiInfo.Text = Dictionary.WilbrandDisclamer;
            richTextBoxWiiInfo.ForeColor = SystemColors.WindowText;
            
            reloadItemsForWilbrand();

            LoadWiiInfo(false);                                  
        }

        private void buttonNextHelp_Click(object sender, EventArgs e)
        {
            HelpPage++;

            if (HelpPage == 2)
            {
                string tutorialFile = CombinePath(TUTORIAL_PATH, Global.LanguageChoice + ".txt");

                if (!File.Exists(tutorialFile))
                {
                    MessageBox.Show(Global.LanguageChoice + ".txt not found !", "WiiDownloader", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    labelHelpTitle.Text = ReadTutorialFile(tutorialFile, "H3", false);
                    richTextBoxHelpText.Text = ReadTutorialFile(tutorialFile, "H4", false);
                }
            }
            else
            {
                MenuItemSetEnabledValue(false);
                _tabControl.TabPages.Remove(_tabPage3);
                _tabControl.TabPages.Add(_tabPage2); 
            }
        }

        private void WAM_check()
        {
            if( (checkBoxDownload41.Checked) ||
                (checkBoxDownload42.Checked) ||
                (checkBoxDownload43.Checked) ||
                (checkBoxDownloadPatchedSystemIOS.Checked) ||
                (checkBoxDownloadActiveIOS.Checked) ||
                (checkBoxDownloadCIOS.Checked) ||               
                (checkBoxDownloadCFG.Checked) ||
                (checkBoxDownloadGX.Checked) ||
                (checkBoxDownloadOfficialChannel.Checked) ||
                (checkBoxDownloadHackmiiInstallerWAD.Checked) ||
                (checkBoxDownloadWiiFlow.Checked))               

                checkBoxDownloadMoreWADmanager.Checked = true;
            else
                checkBoxDownloadMoreWADmanager.Checked = false;
        }

        private void checkBoxDownloadWiiFlow_CheckedChanged(object sender, EventArgs e)
        {
            WAM_check();
        }

        private void checkBoxDownloadGX_CheckedChanged(object sender, EventArgs e)
        {
            WAM_check();            
        }

        private void checkBoxDownloadCFG_CheckedChanged(object sender, EventArgs e)
        {
            WAM_check();            
        }

        private void checkBoxDownloadActiveIOS_CheckedChanged(object sender, EventArgs e)
        {
            WAM_check();           
        }

        private void checkBoxDownloadOfficialChannel_CheckedChanged(object sender, EventArgs e)
        {
            WAM_check();            
        }

        private void checkBoxDownloadPatchedSystemIOS_CheckedChanged(object sender, EventArgs e)
        {
            WAM_check();            
        }

        private void checkBoxDownloadCIOS_CheckedChanged(object sender, EventArgs e)
        {            
            WAM_check(); 
        }

        private void checkBoxDownloadPriiloader_CheckedChanged(object sender, EventArgs e)
        {
            WAM_check();

            if (checkBoxDownloadPriiloader.Checked)
                checkBoxDownloadIOS236.Checked = true;
        }

        private void checkBoxDownloadHackmiiInstallerWAD_CheckedChanged(object sender, EventArgs e)
        {
          //  if(checkBoxDownloadHackmiiInstallerWAD.Checked)

            WAM_check();
            reloadItemsForWilbrand();
//            WAM_check();

        }

        private void comboBoxRegion_SelectedIndexChanged(object sender, EventArgs e)
        {            
            checkBoxDownload41.Text = Dictionary.Download + " SM 4.1" + comboBoxRegion.Text.Substring(1, 1);
            checkBoxDownload42.Text = Dictionary.Download + " SM 4.2" + comboBoxRegion.Text.Substring(1, 1);
            checkBoxDownload43.Text = Dictionary.Download + " SM 4.3" + comboBoxRegion.Text.Substring(1, 1);

            if (comboBoxFirmware.Text != "")
            {
                checkBoxCreateScript.Visible = true;
                if (checkBoxCreateScript.Checked)
                {
                    checkBoxCreateScript.Checked = false;
                    checkBoxCreateScript.Checked = true;
                }                
            }
        }

        private void linkLabelDefaultSettings_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string oldSystemMenu = comboBoxFirmware.Text;
            comboBoxFirmware.Text = "  4.3";
            comboBoxFirmware.Text = "  4.2";
            comboBoxFirmware.Text = oldSystemMenu;
            checkBoxUseWilbrand.Checked = true;
            checkBoxDownloadHackmiiInstaller.Checked = true;        
            checkBoxDownloadHackmiiInstallerWAD.Checked = true;          
            checkBoxDownloadIOS236.Checked = true;
            checkBoxDownloadPriiloader.Checked = true;
            checkBoxDownloadActiveIOS.Checked = true;
            checkBoxDownloadPatchedSystemIOS.Checked = true;
            checkBoxDownloadCIOS.Checked = true;
            checkBoxDownloadOfficialChannel.Checked = true;
            checkBoxDownloadGX.Checked = false;
            checkBoxDownloadCFG.Checked = false;
            checkBoxDownloadWiiFlow.Checked = false;

            reloadItemsForWilbrand();

        }

        private void WiiDownloader_Form_SizeChanged(object sender, EventArgs e)
        {
            IntPtr hSystemMenu = GetSystemMenu(this.Handle, false);
            if (!DOWNLOAD_OR_PROGRAM_WORKING)
                EnableMenuItem(hSystemMenu, SC_CLOSE, MF_ENABLED);
            else
                EnableMenuItem(hSystemMenu, SC_CLOSE, MF_GRAYED);
        }

        private void linkLabelVersion_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(codeGooglePage);
        }

        private void showShellForExternProgramsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            hideShellToolStripMenuItem.Checked = false;
            showShellToolStripMenuItem.Checked = true;
            IniFile ini = new IniFile(SETTINGS_INI_FILE);            
            ini.IniWriteValue("options", "show_process", "True");
            CheckForStartupOptions();
        }

        private void hideShellToolStripMenuItem_Click(object sender, EventArgs e)
        {
            hideShellToolStripMenuItem.Checked = true;
            showShellToolStripMenuItem.Checked = false;
            IniFile ini = new IniFile(SETTINGS_INI_FILE);
            ini.IniWriteValue("options", "show_process", "False");
            CheckForStartupOptions();
        }        

        private void richTextBox2_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start("http://modmii.zzl.org/home.html");
        }

        private void richTextBox6_LinkClicked(object sender, LinkClickedEventArgs e)
        {            
             Process.Start("https://code.google.com/p/wii-downloader/w/list");  
        }

        private void richTextBox4_LinkClicked(object sender, LinkClickedEventArgs e)
        {                
             Process.Start("http://gbatemp.net/topic/331626-wiidownloader/");       
        }

        private void linkLabelWilbrand_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if(File.Exists(CombinePath(IMAGES_PATH, "Homebrew_channel_logo.png")))
                Prompt.ShowHBCDialog(Dictionary.WhyMACaddress, Dictionary.WhyWilbrand, Dictionary.HomebrewChannel, CombinePath(IMAGES_PATH, "Homebrew_channel_logo.png"));
            else
                Prompt.ShowHBCDialog(Dictionary.WhyMACaddress, Dictionary.WhyWilbrand, Dictionary.HomebrewChannel, "");
            /* string text = "[EN] You need to put MAC address for use Wilbrand exploit, only if you haven't Homebrew Channel already installed on your Wii.\n\n" +
                "[IT] E' necessario inserire il MAC address per utilizzare l'exploit Wilbrand, solo se non si dispone di Homebrew Channel già installato sul vostro Wii.\n\n" +
                "[FR] Vous devez mettre l'adresse MAC pour une utilisation Wilbrand exploit, seulement si vous n'avez pas déjà installé Homebrew Channel sur votre Wii.\n\n" +
                "[ES] Tienes que poner la dirección MAC para su uso exploit Wilbrand, sólo si no tienen ya instalado el Homebrew Channel en tu Wii.\n\n";
            Prompt.ShowHBCDialog(text);    */       
          
        }                      
    }
}