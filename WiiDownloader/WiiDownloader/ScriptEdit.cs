using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Configuration;
using Ini;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Net.Mime;

namespace WiiDownloader
{

    public partial class ScriptEdit : Form
    {
        string STARTUP_PATH,
                WIIDOWNLOADER_TEMP_FOLDER,
                NUS_PATH,
                LANGUAGES_PATH,
                SCRIPT_PATH,
                CUSTOM_SCRIPT_PATH,
                IMAGES_PATH,
                TOOLS_PATH,
                DATABASE_PATH,
                patchForScript,
                versionForScript,
                slotForScript;

        bool SHOW_PROCESS;

        public ScriptEdit()
        {
            InitializeComponent();

            textBoxWhiteText.Enabled = false;

          //  STARTUP_PATH = System.IO.Path.GetFullPath(".\\");
            STARTUP_PATH = System.IO.Directory.GetCurrentDirectory();

            WIIDOWNLOADER_TEMP_FOLDER = CombinePath(System.IO.Directory.GetDirectoryRoot(STARTUP_PATH), "wiidownloader_temp_folder");
            DATABASE_PATH = CombinePath(STARTUP_PATH, @"Database");            
            NUS_PATH = CombinePath(DATABASE_PATH, @"nus");
            LANGUAGES_PATH = CombinePath(DATABASE_PATH, @"languages");
            SCRIPT_PATH = CombinePath(DATABASE_PATH, @"script");
            CUSTOM_SCRIPT_PATH = CombinePath(SCRIPT_PATH, @"Custom");
            IMAGES_PATH = CombinePath(DATABASE_PATH, @"images");
            DATABASE_PATH = CombinePath(STARTUP_PATH, @"Database");
            TOOLS_PATH = CombinePath(DATABASE_PATH, @"tools");

            Global.editorMode = WiiDownloader.WiiDownloader_Form.Global.editorMode;
            Global.name = WiiDownloader.WiiDownloader_Form.Global.name;
            Global.type = WiiDownloader.WiiDownloader_Form.Global.type;
            Global.group = WiiDownloader.WiiDownloader_Form.Global.group;
            Global.LanguageChoice = WiiDownloader.WiiDownloader_Form.Global.LanguageChoice;

            string SETTINGS_PATH = CombinePath(DATABASE_PATH, "settings");
            string SETTINGS_INI_FILE = CombinePath(SETTINGS_PATH, "settings.ini");
            IniFile settingsFile = new IniFile(SETTINGS_INI_FILE);

            if((settingsFile.IniReadValue("warning", "warning_1") == "OK") ||  (Global.editorMode == "View"))
                buttonWarning.Visible = false;

            if (settingsFile.IniReadValue("options", "show_process") == "True")
                SHOW_PROCESS = true;
            else
                SHOW_PROCESS = false;

            this.Text = "WiiDownloader Script Editor";
            this.Location = new Point(100, 30);

            createDictionary();

            textBoxRenameIn.Enabled = false;
            textBoxFolderForWAD.Enabled = false;
            comboBoxDownloadFrom.Text = "mediafire";


            if (Global.editorMode != "View")
            {
                enableButton(true, comboBoxTypeUpdate());
                enableScriptButton(true, comboBoxScriptTypeUpdate());
            }
            else
            {
                comboBoxTypeUpdate();
                enableButton(false, false);
                enableScriptButton(false, false);
            }

            if (Global.editorMode != "New")
                loadValue();
            else
                comboBoxDescription.Text = "english";
        }

        public string CombinePath(params string[] path)
        {
            string new_path = path[0];            

            for (int i = 1; i < path.Length; i++)
                new_path = System.IO.Path.Combine(new_path, path[i]);

            return new_path;
        }

        public static class Dictionary
        {
            public static string

                // message
                nameExist,
                nameEmpty,
                typeEmpty,
                notValidChar,
                sourceEmpty,
                scriptSaved,                  
                imageNotFound,
                NoLink,
                NoDownloadFrom,
                InvalidLink,
                InvalidVersion,
                FileToTheRootNotValid;
        }

        public static class Global
        {
            //public static string startupPath;
            public static string editorMode;
            public static string name;
            public static string type;
            public static string group;
            public static string LanguageChoice;
        }

        private bool comboBoxGroupUpdate()
        {
            DirectoryInfo dir;
            bool scriptFound = false;
            string group;

            if (radioButtonStandard.Checked == true)
                group = "Standard";
            else
                group = "Custom";

            dir = new DirectoryInfo(CombinePath(SCRIPT_PATH, group));
            comboBoxScriptType.DataSource = null;

            comboBoxScriptType.DataSource = dir.GetDirectories();

            if (comboBoxType.Items.Count > 0)
            {
                if (searchScriptName(group))
                    scriptFound = true;               
            }

            return scriptFound;
        }

        private bool not_a_multi_scricpt(string script_to_check)
        {
            IniFile script;
            script = new IniFile(script_to_check);
            string scriptString = script.IniReadValue("info", "source");

            if (scriptString != "Combine existing script")
                return true;
            else
                return false;
        }

        private bool searchScriptName(string group)
        {
            comboBoxScriptName.DataSource = null;

            DirectoryInfo dir;
            dir = new DirectoryInfo(CombinePath(SCRIPT_PATH, group, comboBoxScriptType.Text));

            string[] valid_Script = new string[0];
            string[] scriptype_dir = Directory.GetFiles(CombinePath(SCRIPT_PATH, group,comboBoxScriptType.Text), "*");
            foreach (string script_to_check in scriptype_dir)
            {
                if (not_a_multi_scricpt(script_to_check))
                {
                    Array.Resize(ref valid_Script, valid_Script.Length + 1);
                    valid_Script[valid_Script.Length - 1] = Path.GetFileName(script_to_check);
                }
            }

            if (valid_Script.Length > 0)
            {
                comboBoxScriptName.DataSource = valid_Script;
                return true;
            }
            else
                return false;
        }

        private bool comboBoxScriptTypeUpdate()
        {
            bool scriptFound = false;
            string group;

            if ((radioButtonStandard.Checked == false) && (radioButtonCustom.Checked == false))
                radioButtonStandard.Checked = true;

            if (radioButtonStandard.Checked == true)
                group = "Standard";
            else
                group = "Custom";

            //comboBoxScriptName.DataSource = dir.GetFiles(); prima era osì. ma prendeva anche i multi-script...

            if (searchScriptName(group))
                scriptFound = true;

            LoadScriptList();

            return scriptFound;
        }

        private bool comboBoxNusTypeUpdate()
        {
            // DirectoryInfo dir;
            bool scriptFound = false;
            string group;

            if ((radioButtonStandard.Checked == false) && (radioButtonCustom.Checked == false))
                return scriptFound;

            if (radioButtonStandard.Checked == true)
                group = "Standard";
            else
                group = "Custom";
                     
            if (searchScriptName(group))
                scriptFound = true;

            LoadScriptList();

            return scriptFound;
        }

        private void LoadScriptList()
        {
            buttonRemove.Enabled = false;

            if ((Global.type == null) || (Global.name == null))
                return;

            IniFile script = new IniFile(CombinePath(SCRIPT_PATH, Global.group, Global.type, Global.name));
            string downloadList = script.IniReadValue("info", "script_list");

            checkedListBoxScript.Items.Clear();

            char[] delimit = new char[] { ';' };

            buttonRemove.Enabled = false;

            foreach (string key in downloadList.Split(delimit))
            {
                if ((key != null) && (key.Trim() != ""))
                {
                    checkedListBoxScript.Items.Add(key);
                    if (Global.editorMode != "View")
                        buttonRemove.Enabled = true;
                }
            }
            // checkedListBoxScript.SelectedItem = (checkedListBoxScript.Items.Count - 1);

            if (radioButtonCustom.Checked == false && radioButtonStandard.Checked == false)
                radioButtonStandard.Checked = true;
        }

        private void LoadNusList()
        {
            buttonRemoveNUS.Enabled = false;

            if ((Global.type == null) || (Global.name == null))
                return;

            IniFile script = new IniFile(CombinePath(SCRIPT_PATH, Global.group, Global.type,Global.name));
            string downloadList = script.IniReadValue("info", "nus_list");

            checkedListBoxNus.Items.Clear();

            char[] delimit = new char[] { ';' };

            buttonRemove.Enabled = false;

            foreach (string key in downloadList.Split(delimit))
            {
                if ((key != null) && (key.Trim() != ""))
                {
                    checkedListBoxNus.Items.Add(key);
                    if (Global.editorMode != "View")
                        buttonRemoveNUS.Enabled = true;
                }
            }
            checkedListBoxNus.SelectedIndex = checkedListBoxNus.Items.Count - 1;
            checkedListBoxNus.Invalidate();
        }

        private void LoadCiosList()
        {
            buttonRemoveNUS.Enabled = false;

            if ((Global.type == null) || (Global.name == null))
                return;

            IniFile script = new IniFile(CombinePath(SCRIPT_PATH, Global.group, Global.type, Global.name));
            string downloadList = script.IniReadValue("info", "cios_list");

            checkedListBoxCIOS.Items.Clear();

            char[] delimit = new char[] { ';' };

            buttonRemoveCIOS.Enabled = false;

            foreach (string key in downloadList.Split(delimit))
            {
                if ((key != null) && (key.Trim() != ""))
                {
                    checkedListBoxCIOS.Items.Add(key);
                    if (Global.editorMode != "View")
                        buttonRemoveCIOS.Enabled = true;
                }
            }
            checkedListBoxCIOS.SelectedIndex = checkedListBoxCIOS.Items.Count - 1;
            checkedListBoxCIOS.Invalidate();
        }

        private void createDictionary()
        {

            IniFile dictionary = new IniFile(CombinePath(LANGUAGES_PATH, Global.LanguageChoice + ".ini"));

            // menu    
            if (dictionary.IniReadValue("menu", "description") != "")
                labelDescription.Text = dictionary.IniReadValue("menu", "description");
            else
                labelDescription.Text = "Description";

            labelName.Text = WiiDownloader.WiiDownloader_Form.Dictionary.name;            
            labelType.Text = WiiDownloader.WiiDownloader_Form.Dictionary.type;

            if (dictionary.IniReadValue("menu", "existingType") != "")
                existingType.Text = dictionary.IniReadValue("menu", "existingType");

            if (dictionary.IniReadValue("menu", "newType") != "")
                newType.Text = dictionary.IniReadValue("menu", "newType");

            if (dictionary.IniReadValue("menu", "save") != "")
                buttonSave.Text = dictionary.IniReadValue("menu", "save");

            if (dictionary.IniReadValue("menu", "cancel") != "")
                buttonCancel.Text = dictionary.IniReadValue("menu", "cancel");
            
            labelGroup.Text = WiiDownloader.WiiDownloader_Form.Dictionary.group;            
            labelScriptType.Text = WiiDownloader.WiiDownloader_Form.Dictionary.type;                       
            labelScriptName.Text = WiiDownloader.WiiDownloader_Form.Dictionary.name;

            if (dictionary.IniReadValue("menu", "buttonAdd") != "")
                buttonAdd.Text = dictionary.IniReadValue("menu", "buttonAdd");

            if (dictionary.IniReadValue("menu", "buttonAddNus") != "")
                buttonAddNUS.Text = dictionary.IniReadValue("menu", "buttonAddNus");

            if (dictionary.IniReadValue("menu", "buttonAddNus") != "")
                buttonAddCIOS.Text = dictionary.IniReadValue("menu", "buttonAddNus");

            if (dictionary.IniReadValue("menu", "buttonRemove") != "")
                buttonRemove.Text = dictionary.IniReadValue("menu", "buttonRemove");

            if (dictionary.IniReadValue("menu", "buttonRemove") != "")
                buttonRemoveNUS.Text = dictionary.IniReadValue("menu", "buttonRemove");

            if (dictionary.IniReadValue("menu", "buttonRemove") != "")
                buttonRemoveCIOS.Text = dictionary.IniReadValue("menu", "buttonRemove");

            if (dictionary.IniReadValue("menu", "checkBoxChangeFolder") != "")
                checkBoxChangeFolder.Text = dictionary.IniReadValue("menu", "checkBoxChangeFolder");

            if (dictionary.IniReadValue("menu", "downloadedFile") != "")
                labelDownloadedFile.Text = dictionary.IniReadValue("menu", "downloadedFile");

            if (dictionary.IniReadValue("menu", "downloadFrom") != "")
                labelDownloadFrom.Text = dictionary.IniReadValue("menu", "downloadFrom");

            if (dictionary.IniReadValue("menu", "actualLink") != "")
                labelActualLink.Text = dictionary.IniReadValue("menu", "actualLink");

            if (dictionary.IniReadValue("menu", "link") != "")
                labelLink.Text = dictionary.IniReadValue("menu", "link");

            if (dictionary.IniReadValue("menu", "officialSite") != "")
                labelOfficialSite.Text = dictionary.IniReadValue("menu", "officialSite");

            if (dictionary.IniReadValue("menu", "copyTo") != "")
                labelCopyTo.Text = dictionary.IniReadValue("menu", "copyTo");

            if (dictionary.IniReadValue("menu", "copyTo") != "")
                labelCopyWadTo.Text = dictionary.IniReadValue("menu", "copyTo");

            if (dictionary.IniReadValue("menu", "copyTo") != "")
                labelCopyModMiiWadTo.Text = dictionary.IniReadValue("menu", "copyTo");

            if (dictionary.IniReadValue("menu", "modMiiWadName") != "")
                labelModMiiWadName.Text = dictionary.IniReadValue("menu", "modMiiWadName");

            if (dictionary.IniReadValue("menu", "folderToTake") != "")
                labelFolderToTake.Text = dictionary.IniReadValue("menu", "folderToTake");

            if (dictionary.IniReadValue("menu", "downloadedFile") != "")
                labelDownloadedFile.Text = dictionary.IniReadValue("menu", "downloadedFile");
            
            labelVersionCIOS.Text = WiiDownloader.WiiDownloader_Form.Dictionary.type;

            if (dictionary.IniReadValue("menu", "title") != "")
                labelNameNUS.Text = dictionary.IniReadValue("menu", "title");

            if (dictionary.IniReadValue("menu", "region") != "")
                labelRegionNUS.Text = dictionary.IniReadValue("menu", "region");

            if (dictionary.IniReadValue("menu", "version") != "")
                labelVersionNUS.Text = dictionary.IniReadValue("menu", "version");

            if (dictionary.IniReadValue("menu", "imageFile") != "")
                labelImageFileLink.Text = dictionary.IniReadValue("menu", "imageFile");

            if (dictionary.IniReadValue("menu", "imageFile") != "")
                labelImageFileCombine.Text = dictionary.IniReadValue("menu", "imageFile");

            if (dictionary.IniReadValue("menu", "fileToRename") != "")
                labelFileToRename.Text = dictionary.IniReadValue("menu", "fileToRename");

            if (dictionary.IniReadValue("menu", "renameIn") != "")
                labelRanameIn.Text = dictionary.IniReadValue("menu", "renameIn");

            if (dictionary.IniReadValue("menu", "ChangeSlot") != "")
                labelChangeSlot.Text = dictionary.IniReadValue("menu", "ChangeSlot");

            if (dictionary.IniReadValue("menu", "ChangeVersion") != "")
                labelChangeVersion.Text = dictionary.IniReadValue("menu", "ChangeVersion");

            if (dictionary.IniReadValue("menu", "ChangeVersion") != "")
                labelVersionForCIOS.Text = dictionary.IniReadValue("menu", "ChangeVersion");

            if (dictionary.IniReadValue("menu", "SearchImage") != "")
                checkBoxSearchImage.Text = dictionary.IniReadValue("menu", "SearchImage");

            if (dictionary.IniReadValue("menu", "noUseOfLocalFiles") != "")
                checkBoxNotUseLocal.Text = dictionary.IniReadValue("menu", "noUseOfLocalFiles");

            if (dictionary.IniReadValue("menu", "useWilbrand") != "")
                radioButtonForWilbrand.Text = dictionary.IniReadValue("menu", "useWilbrand");

            if (dictionary.IniReadValue("menu", "moveFileToRoot") != "")
                radioButtonForMoveFile.Text = dictionary.IniReadValue("menu", "moveFileToRoot");

            if (dictionary.IniReadValue("menu", "fileToTake") != "")
                labelFileToTake.Text = dictionary.IniReadValue("menu", "fileToTake");

            // message
            if ((dictionary.IniReadValue("message", "nameExist1") != "") && (dictionary.IniReadValue("message", "nameExist2") != ""))
                Dictionary.nameExist = dictionary.IniReadValue("message", "nameExist1") + "\r\n" +
                                   dictionary.IniReadValue("message", "nameExist2");
            else
                Dictionary.nameExist = "It was already found another script with the same name." + "\r\n" +
                                        "Change it, or return to the main menu.";

            if (dictionary.IniReadValue("message", "nameEmpty") != "")
                Dictionary.nameEmpty = dictionary.IniReadValue("message", "nameEmpty");
            else
                Dictionary.nameEmpty = "Script name is empty.";

            if (dictionary.IniReadValue("message", "typeEmpty") != "")
                Dictionary.typeEmpty = dictionary.IniReadValue("message", "typeEmpty");
            else
                Dictionary.typeEmpty = "Script type is empty.";

            if (dictionary.IniReadValue("message", "sourceEmpty") != "")
                Dictionary.sourceEmpty = dictionary.IniReadValue("message", "sourceEmpty");
            else
                Dictionary.sourceEmpty = "Source of the script hasn't been selected.";

            if (dictionary.IniReadValue("message", "scriptSaved") != "")
                Dictionary.scriptSaved = dictionary.IniReadValue("message", "scriptSaved");
            else
                Dictionary.scriptSaved = "OK! Script saved.";  

            if (dictionary.IniReadValue("message", "notValidChar") != "")
                Dictionary.notValidChar = dictionary.IniReadValue("message", "notValidChar");
            else
                Dictionary.notValidChar = "Isn't possible to use character like ',' or ';'";

            if (dictionary.IniReadValue("message", "imageNotFound") != "")
                Dictionary.imageNotFound = dictionary.IniReadValue("message", "imageNotFound");
            else
                Dictionary.imageNotFound = "Image not found in folder \"database\\images\"..";

            if (dictionary.IniReadValue("message", "NoLink") != "")
                Dictionary.NoLink = dictionary.IniReadValue("message", "NoLink");
            else
                Dictionary.NoLink = "Hasn't been written the link.";

            if (dictionary.IniReadValue("message", "InvalidLink") != "")
                Dictionary.InvalidLink = dictionary.IniReadValue("message", "InvalidLink");
            else
                Dictionary.InvalidLink = "Invalid link: isn't associated to a file.";

            if (dictionary.IniReadValue("message", "InvalidVersion") != "")
                Dictionary.InvalidVersion = dictionary.IniReadValue("message", "InvalidVersion");
            else
                Dictionary.InvalidVersion = "Version number not valid (MIN:1 MAX:65535).";

            if (dictionary.IniReadValue("message", "NoDownloadFrom") != "")
                Dictionary.NoDownloadFrom = dictionary.IniReadValue("message", "NoDownloadFrom");
            else
                Dictionary.NoDownloadFrom = "For dynamic links, is required to say where they are hosted.";

            if (dictionary.IniReadValue("message", "FileToTheRootNotValid") != "")
                Dictionary.FileToTheRootNotValid = dictionary.IniReadValue("message", "FileToTheRootNotValid");
            else
                Dictionary.FileToTheRootNotValid = "The file to be moved is invalid: it must be 'boot.dol' or 'boot.elf'";            
            
        }

        private bool checkForNotValidChar(string textToCheck)
        {
            int index;

            index = textBoxName.Text.IndexOf(';');
            if (index > 0)
                return false;

            index = textBoxName.Text.IndexOf(',');
            if (index > 0)
                return false;

            return true;
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            (this.Owner as WiiDownloader_Form).Enabled = true;
            (this.Owner as WiiDownloader_Form).StartupCheck(true);

            this.Close();
        }


        private void enableButton(bool value, bool typeExists)
        {
            textBoxName.Enabled = value;
            comboBoxType.Enabled = typeExists;
            textBoxNewType.Enabled = value;
            //textBoxDescription.Enabled = value;
            buttonSave.Enabled = value;
            if (checkBoxSearchImage.Checked)
            {
                textBoxImageFileLink.Size = new System.Drawing.Size(0, 0);
                textBoxWhiteText.Size = new System.Drawing.Size(164, 25);
                textBoxWhiteText.Enabled = false;
            }
            else
            {
                textBoxImageFileLink.Size = new System.Drawing.Size(164, 25);
                textBoxWhiteText.Size = new System.Drawing.Size(0, 0);
                textBoxWhiteText.Enabled = true;
            }
        }

        private void enableScriptButton(bool value, bool scriptFound)
        {
            comboBoxScriptName.Enabled = scriptFound;
            if (Global.editorMode == "View")
                buttonAdd.Enabled = false;
            else
                buttonAdd.Enabled = scriptFound;
            //   checkedListBoxScript.Enabled = value;
            radioButtonIOS.Enabled = value;
            radioButtonSystem.Enabled = value;
            comboBoxScriptType.Enabled = value;
            comboBoxVersionCIOS.Enabled = value;
            radioButtonCustom.Enabled = value;
            radioButtonStandard.Enabled = value;
            checkBoxChangeFolder.Enabled = value;
            if (Global.group == "Standard")
                textBoxFolderForWAD.Enabled = value;
        }



        private void loadValue()
        {
            // value takes form the main Form            
            textBoxName.Text = Global.name;
            comboBoxType.Text = Global.type;

            if (Global.name == null)
                Global.name = "";
            if (Global.type == null)
                Global.type = "";

            IniFile script = new IniFile(CombinePath(SCRIPT_PATH, Global.group, Global.type, Global.name)); ;


            string source = script.IniReadValue("info", "source");
            switch (source)
            {
                case "Download from URL":
                    tabControl.SelectedIndex = 0;
                    break;
                case "Download from NUS":
                    tabControl.SelectedIndex = 1;
                    break;
                case "Download using ModMii":
                    tabControl.SelectedIndex = 2;
                    break;
                case "Other features":
                    tabControl.SelectedIndex = 3;
                    break;
                case "Combine existing script":
                    tabControl.SelectedIndex = 4;
                    break;
            }

            if (Global.editorMode == "View")
            {
                buttonAdd.Enabled = false;
                buttonAddNUS.Enabled = false;
                textBoxSlot.Enabled = false;
                textBoxNewVersion.Enabled = false;
                checkBoxNoPatch.Enabled = false;
                checkBoxFakeSigning.Enabled = false;
                checkBoxEsIdentify.Enabled = false;
                checkBoxNandPermission.Enabled = false;
                checkBoxVersion.Enabled = false;
                buttonRemove.Enabled = false;
                buttonRemoveNUS.Enabled = false;
                buttonSave.Enabled = false;
            }

            //menù
            switch (tabControl.SelectedTab.Text)
            {
                case "Download from URL":
                    comboBoxDownloadFrom.Text = script.IniReadValue("info", "downloadFrom");
                    if (script.IniReadValue("info", "urlType") == "dinamic")
                        radioButtonDinamicLink.Checked = true;
                    else
                        radioButtonStaticLink.Checked = true;
                    textBoxDownloadedFile.Text = script.IniReadValue("info", "downloadedFile");
                    textBoxLink.Text = script.IniReadValue("info", "link");
                    textBoxOfficialSite.Text = script.IniReadValue("info", "officialSite");
                    textBoxCopyTo.Text = script.IniReadValue("info", "copyTo");
                    textBoxFolderToTake.Text = script.IniReadValue("info", "folderToTake");
                    textBoxImageFileLink.Text = script.IniReadValue("info", "imageFileLink");
                    textBoxFileToRename.Text = script.IniReadValue("info", "fileToRename");
                    textBoxRenameIn.Text = script.IniReadValue("info", "renameIn");

                    if (script.IniReadValue("info", "searchImage") == "")
                        checkBoxSearchImage.Checked = false;
                    else
                        checkBoxSearchImage.Checked = Convert.ToBoolean(script.IniReadValue("info", "searchImage"));

                    if (script.IniReadValue("info", "noUseOfLocalFiles") == "")
                        checkBoxNotUseLocal.Checked = false;
                    else
                        checkBoxNotUseLocal.Checked = Convert.ToBoolean(script.IniReadValue("info", "noUseOfLocalFiles"));
                    

                    if (radioButtonDinamicLink.Checked == true)
                    {
                        // is not necessary for create link now..
                        textBoxActualLink.Text = ""; // WiiDownloader.WiiDownloader_Form.CreateActualLink(textBoxLink.Text, comboBoxDownloadFrom.Text);
                    }

                    break;
                case "Download from NUS":
                    LoadNusList();
                    textBoxCopyWadTo.Text = script.IniReadValue("info", "copyWadTo");
                    break;
                case "Download using ModMii":
                    LoadCiosList();
                    textBoxCopyCIOSTo.Text = script.IniReadValue("info", "copyCiosTo");
                    break;
                case "Other features":
                    LoadCiosList();
                    string featureType = script.IniReadValue("info", "OtherFeatures");
                    switch (featureType)
                    {
                        case "useWilbrand":
                            radioButtonForWilbrand.Checked = true;
                            break;
                        case "moveToRoot":
                            radioButtonForMoveFile.Checked = true;
                            textBoxFileToTheRoot.Text = script.IniReadValue("info", "fileToTheRoot");
                            break;                            
                    }
                    break;
                case "Combine existing script":
                    LoadScriptList();
                    if (script.IniReadValue("info", "forceToFolder") == "")
                    {
                        textBoxFolderForWAD.Enabled = false;
                        checkBoxChangeFolder.Checked = false;
                    }
                    else
                        checkBoxChangeFolder.Checked = Convert.ToBoolean(script.IniReadValue("info", "forceToFolder"));
                    textBoxFolderForWAD.Text = script.IniReadValue("info", "newFolder");
                    textBoxImageFileCombine.Text = script.IniReadValue("info", "imageFileCombine");
                    break;
                default:
                    break;
            }
            LoadDescription();

        }

        public void setNameForCIOS()
        {
            switch (comboBoxVersionCIOS.Text)
            {
                case "System menu patched IOS":
                    textBoxNameForCIOS.Text = "IOS" + comboBoxSlotForCIOS.Text + "v16174(IOS60v6174[FS-ES-NP-VP-DIP]).wad";
                    break;
                default:
                    textBoxNameForCIOS.Text = "cIOS" + comboBoxSlotForCIOS.Text + '[' + comboBoxBaseForCIOS.Text + "]-" + comboBoxVersionCIOS.Text + ".wad";
                    break;
            }            
            buttonAddCIOS.Enabled = true;
        }

        public void setBaseForCIOS()
        {
            comboBoxBaseForCIOS.Enabled = true;

            switch (comboBoxSlotForCIOS.Text)
            {
                case "202":
                    comboBoxBaseForCIOS.DataSource = (new string[] { "60" });
                    break;
                case "222":
                    comboBoxBaseForCIOS.DataSource = (new string[] { "38" });
                    break;
                case "223":
                    switch (comboBoxVersionCIOS.Text)
                    {
                        case "Hermes-v4":
                            comboBoxBaseForCIOS.DataSource = (new string[] { "37-38" });
                            break;
                        default:
                            comboBoxBaseForCIOS.DataSource = (new string[] { "37" });
                            break;
                    }
                    break;
                case "224":
                    comboBoxBaseForCIOS.DataSource = (new string[] { "57" });
                    break;
                default:
                    switch (comboBoxVersionCIOS.Text)
                    {
                        case "Waninkoko-v14":
                            comboBoxBaseForCIOS.DataSource = (new string[] { "38" });
                            break;
                        case "Waninkoko-v17b":
                            comboBoxBaseForCIOS.DataSource = (new string[] { "38" });
                            break;
                        case "Waninkoko-v19":
                            comboBoxBaseForCIOS.DataSource = (new string[] { "37", "38", "57" });
                            break;
                        case "Waninkoko-v20":
                            comboBoxBaseForCIOS.DataSource = (new string[] { "38", "56", "57" });
                            break;
                        case "Waninkoko-v21":
                            comboBoxBaseForCIOS.DataSource = (new string[] { "37", "38", "53", "55", "56", "57", "58" });
                            break;
                        case "d2x-v1-final":
                        case "d2x-v2-final":
                        case "d2x-v3-final":
                        case "d2x-v4-final":
                        case "d2x-v5-final":
                        case "d2x-v6-final":
                        case "d2x-v7-final":
                            comboBoxBaseForCIOS.DataSource = (new string[] { "37", "38", "53", "55", "56", "57", "58" });
                            break;
                        case "d2x-v8-final":
                        case "d2x-v9-beta(r47)":
                        case "d2x-v9-beta(r49)":
                        case "d2x-v10-beta52":
                        case "d2x-v10-beta53-alt":
                            comboBoxBaseForCIOS.DataSource = (new string[] { "37", "38", "53", "55", "56", "57", "58", "60", "70", "80" });
                            break;
                        case "System menu patched IOS":
                            comboBoxBaseForCIOS.Enabled = false;
                            comboBoxBaseForCIOS.DataSource = (new string[] { "60" });
                            break;
                        default:
                            break;
                    }

                    break;
            }
        }

        public void setSlotForCIOS()
        {
            comboBoxSlotForCIOS.Enabled = true;
            textBoxVersionForCIOS.Enabled = true;            
            string[] waninkoko_valid_slot = { "241", "242", "243", "244", "245", "246", "247", "248", "249", "250", "251", "252", "253" };
            string[] hermesv4_valid_slot = { "222", "223" };
            string[] hermesv5_valid_slot = { "222", "223", "224" };
            string[] hermesv51_valid_slot = { "202", "222", "223", "224" };
            string[] patched_system_IOS_slot = { "11", "20", "30", "40", "50", "52", "60", "70", "80" };

            switch (comboBoxVersionCIOS.Text)
            {
                case "Hermes-v4":
                    textBoxVersionForCIOS.Text = "65535";
                    comboBoxSlotForCIOS.DataSource = hermesv4_valid_slot;
                    comboBoxSlotForCIOS.Text = "222";
                    break;
                case "Hermes-v5":
                    textBoxVersionForCIOS.Text = "65535";
                    comboBoxSlotForCIOS.DataSource = hermesv5_valid_slot;
                    comboBoxSlotForCIOS.Text = "222";
                    break;
                case "HermesRodries-v5.1":
                    textBoxVersionForCIOS.Text = "65535";
                    comboBoxSlotForCIOS.DataSource = hermesv51_valid_slot;
                    comboBoxSlotForCIOS.Text = "202";
                    break;
                case "Waninkoko-v14":
                    textBoxVersionForCIOS.Text = "14";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "Waninkoko-v17b":
                    textBoxVersionForCIOS.Text = "17";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "Waninkoko-v19":
                    textBoxVersionForCIOS.Text = "19";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "Waninkoko-v20":
                    textBoxVersionForCIOS.Text = "20";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "Waninkoko-v21":
                    textBoxVersionForCIOS.Text = "21";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "d2x-v1-final":
                    textBoxVersionForCIOS.Text = "21001";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "d2x-v2-final":
                    textBoxVersionForCIOS.Text = "21002";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "d2x-v3-final":
                    textBoxVersionForCIOS.Text = "21003";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "d2x-v4-final":
                    textBoxVersionForCIOS.Text = "21004";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "d2x-v5-final":
                    textBoxVersionForCIOS.Text = "21005";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "d2x-v6-final":
                    textBoxVersionForCIOS.Text = "21006";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "d2x-v7-final":
                    textBoxVersionForCIOS.Text = "21007";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "d2x-v8-final":
                    textBoxVersionForCIOS.Text = "21008";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "d2x-v9-beta(r47)":
                    textBoxVersionForCIOS.Text = "21009";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "d2x-v9-beta(r49)":
                    textBoxVersionForCIOS.Text = "21009";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "d2x-v10-beta52":
                case "d2x-v10-beta53-alt":
                    textBoxVersionForCIOS.Text = "21010";
                    comboBoxSlotForCIOS.DataSource = waninkoko_valid_slot;
                    comboBoxSlotForCIOS.Text = "249";
                    break;
                case "System menu patched IOS":
                    textBoxVersionForCIOS.Enabled = false;
                    textBoxVersionForCIOS.Text = "16174";
                    comboBoxSlotForCIOS.DataSource = patched_system_IOS_slot;
                    comboBoxSlotForCIOS.Text = "80";
                    break;
                default:
                    break;

            }
        }
        
        public bool FileInUse(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
                {
                    return false;
                }

            }
            catch
            {
                return true;
            }
        }

        public void MySleep(int milliseconds)
        {
            bool timeElapsed = false;
            var TimerForSleep = new System.Timers.Timer(milliseconds);
            TimerForSleep.Elapsed += (s, e) => timeElapsed = true;
            TimerForSleep.Start();

            while (!timeElapsed)
                Application.DoEvents();
        }

        private bool executeCommandForEditor(string program, string arguments, string Text4LabelError)
        {
            try
            {
                ProcessStartInfo p_edit_command;
                Process EditCommandProcess;
                
                p_edit_command = new ProcessStartInfo("cmd.exe", "/c \"" + program + " " + arguments + "\"");                             
                                
                p_edit_command.UseShellExecute = true;
                p_edit_command.RedirectStandardOutput = false;
                p_edit_command.RedirectStandardInput = false;
                p_edit_command.RedirectStandardError = false;
                p_edit_command.CreateNoWindow = true;
                p_edit_command.WorkingDirectory = TOOLS_PATH;
                if (SHOW_PROCESS)
                    p_edit_command.WindowStyle = ProcessWindowStyle.Normal;
                else
                    p_edit_command.WindowStyle = ProcessWindowStyle.Hidden;
                p_edit_command.WindowStyle = ProcessWindowStyle.Hidden;
                EditCommandProcess = Process.Start(p_edit_command);
                EditCommandProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + Text4LabelError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private string getKeyLink(string stringToParse, string parsingKeyString, char finalChar)
        {
            int i;
            int index = stringToParse.LastIndexOf(parsingKeyString);
            string tempString = "";

            for (i = index + parsingKeyString.Length; i < stringToParse.Length; i++)
            {
                if (stringToParse[i] == ' ')
                    break;
                if (stringToParse[i] == finalChar)
                    break;
                tempString = tempString + stringToParse[i];
            }


            return tempString;
        }

        

        private string CreateActualLink(string ulrToParse, string downloadFrom)
        {
            string downloadUrl = "", parsingKeyString = "", /*batchToExecute = " ",*/ stringToAdd = "";
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
                    string hackmii_installer_version;
                    if (textBoxName.Text.Contains("1.0"))
                        hackmii_installer_version = "1.0";
                    else
                        hackmii_installer_version = "1.2";

                    parsingKeyString = "hackmii_installer_v" + hackmii_installer_version + ".zip&amp;key=";
                    finalChar = '"';
                    stringToAdd = "http://bootmii.org/get.php?file=hackmii_installer_v" + hackmii_installer_version + ".zip&key=";
                    break;
                default:
                    return "";
            }                     

            string program = "\"" + CombinePath(TOOLS_PATH, "wget.exe") + "\"";

            Directory.CreateDirectory(WIIDOWNLOADER_TEMP_FOLDER);

            if (!executeCommandForEditor(program,
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

            timer = 0;
            MySleep(100);
            while (FileInUse(CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "wiidownloaderTempLink.txt")))
            {
                timer++;
                if (timer > 20)
                    return "";

                MySleep(200);
                continue;
            }
            MySleep(100);

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
               


        private bool CheckValues()
        {
            if (textBoxName.Text.Trim() == "")
            {
                textBoxName.BackColor = Color.LightSalmon;
                MessageBox.Show(Dictionary.nameEmpty, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxName.BackColor = SystemColors.Window;
                return false;
            }

            if (!checkForNotValidChar(textBoxName.Text))
            {
                textBoxName.BackColor = Color.LightSalmon;
                MessageBox.Show(Dictionary.notValidChar, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxName.BackColor = SystemColors.Window;
                return false;
            }

            if (comboBoxType.Enabled == false && textBoxNewType.Text.Trim() == "")
            {
                textBoxNewType.BackColor = Color.LightSalmon;
                MessageBox.Show(Dictionary.typeEmpty, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxNewType.BackColor = SystemColors.Window;
                return false;
            }

            if (comboBoxType.Enabled == true && comboBoxType.Text.Trim() == "")
            {
                comboBoxType.BackColor = Color.LightSalmon;
                MessageBox.Show(Dictionary.typeEmpty, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBoxType.BackColor = SystemColors.Window;
                return false;
            }


            switch (tabControl.SelectedTab.Text)
            {
                case "Download from URL":
                    string linkToVerify;
                    if (textBoxLink.Text.Trim() == "")
                    {
                        textBoxLink.BackColor = Color.LightSalmon;
                        MessageBox.Show(Dictionary.NoLink, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        textBoxLink.BackColor = SystemColors.Window;
                        return false;
                    }

                    if (radioButtonDinamicLink.Checked == true && comboBoxDownloadFrom.Text.Trim() == "")
                    {
                        comboBoxDownloadFrom.BackColor = Color.LightSalmon;
                        MessageBox.Show(Dictionary.NoDownloadFrom, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        comboBoxDownloadFrom.BackColor = SystemColors.Window;
                        return false;
                    }

                    if (radioButtonDinamicLink.Checked == true)
                    {
                        this.Cursor = Cursors.WaitCursor;
                        this.Enabled = false;
                        SendKeys.Send("{TAB}");

                        linkToVerify = CreateActualLink(textBoxLink.Text, comboBoxDownloadFrom.Text);
                        
                        if (File.Exists(CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "wiidownloaderTempLink.txt")))
                            File.Delete(CombinePath(WIIDOWNLOADER_TEMP_FOLDER, "wiidownloaderTempLink.txt"));                        
                        if (Directory.Exists(WIIDOWNLOADER_TEMP_FOLDER))
                            Directory.Delete(WIIDOWNLOADER_TEMP_FOLDER, true);  

                        this.Cursor = Cursors.Default;
                        this.Enabled = true;

                        if (linkToVerify == "")
                        {
                            textBoxLink.BackColor = Color.LightSalmon;
                            MessageBox.Show("\n" + WiiDownloader.WiiDownloader_Form.Dictionary.error + ": " + WiiDownloader.WiiDownloader_Form.Dictionary.NoResponse + " " + textBoxLink.Text, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            textBoxLink.BackColor = SystemColors.Window;
                            return false;
                        }
                    }
                    else
                        linkToVerify = textBoxLink.Text;

                    string filename;

                    try
                    {
                        Uri uri = new Uri(linkToVerify);
                        {
                            try
                            {
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                                try
                                {
                                    HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                                    filename = webResponse.ResponseUri.AbsolutePath;
                                    webResponse.Close();
                                    request.Abort();
                                }
                                catch
                                {
                                    filename = "";
                                }
                            }
                            catch
                            {
                                filename = "";
                            }
                        }

                        if (radioButtonDinamicLink.Checked)
                            textBoxActualLink.Text = linkToVerify;

                        if (radioButtonDinamicLink.Checked && comboBoxDownloadFrom.Text == "bootmii.org")
                        {
                            if (textBoxName.Text.Contains("1.0"))
                                textBoxDownloadedFile.Text = "hackmii_installer_v1.0.zip";
                            else
                                textBoxDownloadedFile.Text = "hackmii_installer_v1.2.zip";
                        }
                        else
                        {
                            filename = Path.GetFileName(filename);
                            filename = filename.Replace("%20", " ");
                            if (radioButtonDinamicLink.Checked && comboBoxDownloadFrom.Text == "mediafire")
                                filename = filename.Replace("+", " ");

                            textBoxDownloadedFile.Text = filename;
                        }
                    }
                    catch (Exception ex)
                    {
                        textBoxLink.BackColor = Color.LightSalmon;
                        MessageBox.Show(WiiDownloader.WiiDownloader_Form.Dictionary.ErrorOccurred + "\n" + Dictionary.InvalidLink + " " + ex.Message);
                        textBoxLink.BackColor = SystemColors.Window;
                        return false;
                    }

                    if (radioButtonDinamicLink.Checked == false)
                    {
                        
                        try
                        {
                            HttpWebRequest httpWReq = (HttpWebRequest)HttpWebRequest.Create(linkToVerify);
                           // httpWReq.Method = "HEAD";
                            HttpWebResponse httpWRes = (HttpWebResponse)httpWReq.GetResponse();                            

                            if (httpWRes.StatusCode != HttpStatusCode.OK)
                            {
                                httpWReq.Abort();
                                httpWRes.Close();
                                textBoxLink.BackColor = Color.LightSalmon;
                                MessageBox.Show(WiiDownloader.WiiDownloader_Form.Dictionary.ErrorOccurred + "\n" + Dictionary.InvalidLink);
                                textBoxLink.BackColor = SystemColors.Window;                               

                                return false;
                            }
                            httpWReq.Abort();
                            httpWRes.Close();
                        }
                        catch (Exception ex)
                        {
                            textBoxLink.BackColor = Color.LightSalmon;
                            MessageBox.Show(WiiDownloader.WiiDownloader_Form.Dictionary.ErrorOccurred + "\n" + Dictionary.InvalidLink + "\n" + ex.Message);
                            textBoxLink.BackColor = SystemColors.Window;                           
                           
                            return false;
                        }        
                        


                    }

                    if (textBoxDownloadedFile.Text.Trim() == "")
                    {
                        textBoxDownloadedFile.BackColor = Color.LightSalmon;
                        textBoxLink.BackColor = Color.LightSalmon;
                        MessageBox.Show(Dictionary.InvalidLink, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        textBoxDownloadedFile.BackColor = SystemColors.Window;
                        textBoxLink.BackColor = SystemColors.Window;
                        return false;
                    }


                    break;
                case "Download from NUS":
                    break;
                case "Download using ModMii":
                    if (comboBoxVersionCIOS.Text == "")
                        break;
                    int Num;
                    bool isNum = int.TryParse(textBoxVersionForCIOS.Text, out Num);

                    if (!isNum || (Num < 1 || Num > 65535))
                    {
                        textBoxVersionForCIOS.BackColor = Color.LightSalmon;
                        MessageBox.Show(Dictionary.InvalidVersion, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        textBoxVersionForCIOS.BackColor = SystemColors.Window;
                        return false;
                    }
                    break;
                case "Other features":
                    if (radioButtonForWilbrand.Checked == true)
                        break;                        
                    else if (radioButtonForMoveFile.Checked == true)
                    {
                        string fileToTheRoot = textBoxFileToTheRoot.Text;
                        if(fileToTheRoot.Length < 8)
                        {
                            textBoxFileToTheRoot.BackColor = Color.LightSalmon;
                            MessageBox.Show(Dictionary.FileToTheRootNotValid, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            textBoxFileToTheRoot.BackColor = SystemColors.Window;
                            return false;
                        }
                        if( (fileToTheRoot.Substring(fileToTheRoot.Length - 8, 8) != "boot.dol") && (fileToTheRoot.Substring(fileToTheRoot.Length - 8, 8) != "boot.elf"))
                        {
                            textBoxFileToTheRoot.BackColor = Color.LightSalmon;
                            MessageBox.Show(Dictionary.FileToTheRootNotValid, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            textBoxFileToTheRoot.BackColor = SystemColors.Window;
                            return false;
                        }                        
                    }                     
                    break;
                case "Combine existing script":
                    if (textBoxImageFileCombine.Text.Trim() != "")
                    {
                        if (!File.Exists(CombinePath(IMAGES_PATH, textBoxImageFileCombine.Text)))
                        {
                            textBoxImageFileCombine.BackColor = Color.LightSalmon;
                            MessageBox.Show(Dictionary.imageNotFound, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            textBoxImageFileCombine.BackColor = SystemColors.Window;
                            return false;
                        }
                    }
                    break;
                default:
                    break;
            }


            bool somethingChanged = false;

            if ((Global.editorMode != "View") &&
                    (textBoxName.Text != Global.name))
                somethingChanged = true;

            if ((comboBoxType.Enabled == true) && (Global.editorMode != "View") &&
                     (comboBoxType.Text != Global.type))
                somethingChanged = true;

            if ((comboBoxType.Enabled == false) && (Global.editorMode != "View") &&
                     (textBoxNewType.Text != Global.type))
                somethingChanged = true;

            if (somethingChanged == true)
            {
                if (textBoxNewType.Text.Trim() != "")
                {
                    if (File.Exists(CombinePath(CUSTOM_SCRIPT_PATH, comboBoxType.Text, textBoxNewType.Text)))
                    {
                        MessageBox.Show(Dictionary.nameExist, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
                else if (File.Exists(CombinePath(CUSTOM_SCRIPT_PATH, comboBoxType.Text, textBoxName.Text)))
                {
                    MessageBox.Show(Dictionary.nameExist, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            if (Global.editorMode == "New")
                Directory.CreateDirectory(CombinePath(CUSTOM_SCRIPT_PATH, textBoxNewType.Text));
            else if (somethingChanged == true)
            {
                if (comboBoxType.Enabled == true) // use comboBoxType.Text
                {
                    if (File.Exists(CombinePath(CUSTOM_SCRIPT_PATH, Global.type, Global.name)))
                    {
                        Directory.CreateDirectory(CombinePath(CUSTOM_SCRIPT_PATH, comboBoxType.Text));
                        File.Move(CombinePath(CUSTOM_SCRIPT_PATH, Global.type, Global.name),
                                    CombinePath(CUSTOM_SCRIPT_PATH, comboBoxType.Text, textBoxName.Text));
                    }
                }
                else // use textBoxNewType.Text
                {
                    if (File.Exists(CombinePath(CUSTOM_SCRIPT_PATH, Global.type, Global.name)))
                    {
                        Directory.CreateDirectory(CombinePath(CUSTOM_SCRIPT_PATH, textBoxNewType.Text));
                        File.Move(CombinePath(CUSTOM_SCRIPT_PATH, Global.type, Global.name),
                                    CombinePath(CUSTOM_SCRIPT_PATH, textBoxNewType.Text, textBoxName.Text));
                    }
                }

                System.IO.DirectoryInfo editedScriptDir = new System.IO.DirectoryInfo(CombinePath(CUSTOM_SCRIPT_PATH, Global.type));
                if (editedScriptDir.GetFiles().Length == 0)
                    Directory.Delete(CombinePath(CUSTOM_SCRIPT_PATH, Global.type));
            }

            //   if (somethingChanged == true)
            Global.editorMode = WiiDownloader.WiiDownloader_Form.Global.editorMode = "Edit_with_changes";
            //  else
            //    Global.editorMode = WiiDownloader.WiiDownloader_Form.Global.editorMode = "Edit";

            return true;
        }

        private void LoadDescription()
        {
            IniFile script;

            if (Global.editorMode == "New")
            {
                comboBoxDescription.Text = "english";
                return;
            }

            script = new IniFile(CombinePath(SCRIPT_PATH, Global.group, Global.type, Global.name));

            textBoxDescription.Text = script.IniReadValue("info", "description." + Global.LanguageChoice);
            if (textBoxDescription.Text.Trim() != "")
                comboBoxDescription.Text = Global.LanguageChoice;
            else
            {
                textBoxDescription.Text = script.IniReadValue("info", "description.english");
                comboBoxDescription.Text = "english";
            }
        }

        private void SaveValues()
        {
            IniFile script;
            string typeUsed;

            if (comboBoxType.Enabled == false)
                typeUsed = textBoxNewType.Text;
            else
                typeUsed = comboBoxType.Text;

            if (!Directory.Exists(CombinePath(CUSTOM_SCRIPT_PATH, typeUsed)))
                Directory.CreateDirectory(CombinePath(CUSTOM_SCRIPT_PATH, typeUsed));

            // se il file è nuovo, lo creo in unicode
            if (!File.Exists(CombinePath(CUSTOM_SCRIPT_PATH, typeUsed, textBoxName.Text)))
            {
                System.IO.StreamWriter file = new System.IO.StreamWriter(CombinePath(CUSTOM_SCRIPT_PATH, typeUsed, textBoxName.Text), true, Encoding.Unicode);
                file.WriteLine("");
                file.Close();
            }

            script = new IniFile(CombinePath(CUSTOM_SCRIPT_PATH, typeUsed, textBoxName.Text));
            Global.type = typeUsed;


            Global.name = textBoxName.Text;
            script.IniWriteValue("info", "name", textBoxName.Text);
            script.IniWriteValue("info", "source", tabControl.SelectedTab.Text);

            script.IniWriteValue("info", "downloadedFile", "");
            script.IniWriteValue("info", "link", "");
            script.IniWriteValue("info", "officialSite", "");
            script.IniWriteValue("info", "copyTo", "");
            script.IniWriteValue("info", "copyWadTo", "");
            script.IniWriteValue("info", "folderToTake", "");
            script.IniWriteValue("info", "imageFileLink", "");
            script.IniWriteValue("info", "fileToRename", "");
            script.IniWriteValue("info", "renameIn", "");
            script.IniWriteValue("info", "imageFileCombine", "");
            script.IniWriteValue("info", "forceToFolder", "");
            script.IniWriteValue("info", "newFolder", "");
            script.IniWriteValue("info", "searchImage", "");
            script.IniWriteValue("info", "noUseOfLocalFiles", "");            
            script.IniWriteValue("info", "urlDownloadPage", "");
            script.IniWriteValue("info", "node", "");
            script.IniWriteValue("info", "urlType", "");
            script.IniWriteValue("info", "attribute", "");
            script.IniWriteValue("info", "oldString", "");
            script.IniWriteValue("info", "newString", "");
            script.IniWriteValue("info", "tagNumber", "");
            script.IniWriteValue("info", "copyCiosTo", "");
            script.IniWriteValue("info", "downloadFrom", "");
            script.IniWriteValue("info", "OtherFeatures", "");
            script.IniWriteValue("info", "fileToTheRoot", "");

            switch (tabControl.SelectedTab.Text)
            {
                case "Download from URL":
                    if (radioButtonStaticLink.Checked == true)
                        script.IniWriteValue("info", "urlType", "static");
                    else
                    {
                        script.IniWriteValue("info", "urlType", "dinamic");
                        script.IniWriteValue("info", "downloadFrom", comboBoxDownloadFrom.Text);
                    }

                    script.IniWriteValue("info", "downloadedFile", textBoxDownloadedFile.Text);
                    script.IniWriteValue("info", "link", textBoxLink.Text);
                    script.IniWriteValue("info", "officialSite", textBoxOfficialSite.Text);
                    script.IniWriteValue("info", "copyTo", textBoxCopyTo.Text);
                    script.IniWriteValue("info", "folderToTake", textBoxFolderToTake.Text);
                    script.IniWriteValue("info", "imageFileLink", textBoxImageFileLink.Text);
                    script.IniWriteValue("info", "fileToRename", textBoxFileToRename.Text);
                    script.IniWriteValue("info", "renameIn", textBoxRenameIn.Text);
                    script.IniWriteValue("info", "searchImage", Convert.ToString(checkBoxSearchImage.Checked));
                    script.IniWriteValue("info", "noUseOfLocalFiles", Convert.ToString(checkBoxNotUseLocal.Checked));
                    if (checkBoxNotUseLocal.Checked)
                        script.IniWriteValue("info", "MD5", "");
                    script.IniWriteValue("info", "script_list", "");
                    script.IniWriteValue("info", "nus_list", "");
                    script.IniWriteValue("info", "cios_list", "");

                    break;
                case "Download from NUS":
                    script.IniWriteValue("info", "copyWadTo", textBoxCopyWadTo.Text);
                    script.IniWriteValue("info", "script_list", "");
                    script.IniWriteValue("info", "cios_list", "");
                    break;
                case "Download using ModMii":
                    script.IniWriteValue("info", "copyCiosTo", textBoxCopyCIOSTo.Text);
                    script.IniWriteValue("info", "script_list", "");
                    script.IniWriteValue("info", "nus_list", "");
                    break;

                case "Combine existing script":
                    script.IniWriteValue("info", "nus_list", "");
                    script.IniWriteValue("info", "cios_list", "");
                    script.IniWriteValue("info", "imageFileCombine", textBoxImageFileCombine.Text);
                    script.IniWriteValue("info", "forceToFolder", Convert.ToString(checkBoxChangeFolder.Checked));
                    script.IniWriteValue("info", "newFolder", textBoxFolderForWAD.Text);
                    break;
                case "Other features":
                    if (radioButtonForWilbrand.Checked == true)
                        script.IniWriteValue("info", "OtherFeatures", "useWilbrand");
                    else if (radioButtonForMoveFile.Checked == true)
                    {
                        script.IniWriteValue("info", "OtherFeatures", "moveToRoot");
                        script.IniWriteValue("info", "fileToTheRoot", textBoxFileToTheRoot.Text);
                    }
                    break;
                default:
                    break;
            }
            if (textBoxDescription.Text.Trim() != "")
                script.IniWriteValue("info", "description." + comboBoxDescription.Text, textBoxDescription.Text);


        }


        private bool comboBoxTypeUpdate()
        {
            if (!Directory.Exists(CombinePath(SCRIPT_PATH, Global.group)))
                return false;

            DirectoryInfo dir;
            bool somethingFound = false;

            dir = new DirectoryInfo(CombinePath(SCRIPT_PATH, Global.group));
            comboBoxType.DataSource = null;

            comboBoxType.DataSource = dir.GetDirectories();

            if (comboBoxType.Items.Count > 0)
                somethingFound = true;

            return somethingFound;
        }

        private void textBoxNewType_TextChanged(object sender, EventArgs e)
        {
            if (textBoxNewType.Text != "")
                comboBoxType.Enabled = false;
            else
                comboBoxType.Enabled = true;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (!CheckValues())
                return;

           // string tab = tabControl.SelectedTab.Text;

            SaveValues();

            MessageBox.Show(Dictionary.scriptSaved,
                              this.Text + " - Info",
                             MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void comboBoxDescription_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Global.editorMode == "New")
                return;

            IniFile script;

            script = new IniFile(CombinePath(SCRIPT_PATH, Global.group, Global.type, Global.name));

            textBoxDescription.Text = script.IniReadValue("info", "description." + comboBoxDescription.Text);
        }

        private void radioButtonStandard_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxGroupUpdate();
        }

        private void radioButtonCustom_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxGroupUpdate();
        }

        private void comboBoxScriptType_SelectedIndexChanged(object sender, EventArgs e)
        {
            enableScriptButton(true, comboBoxScriptTypeUpdate());
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (!CheckValues())
                return;

            SaveValues();
            IniFile script = new IniFile(CombinePath(CUSTOM_SCRIPT_PATH, Global.type, Global.name));
            string downloadList = script.IniReadValue("info", "script_list");

            string group;
            if (radioButtonStandard.Checked == true)
                group = "Standard";
            else
                group = "Custom";

            script.IniWriteValue("info", "script_list", downloadList + group + ", " + comboBoxScriptType.Text + ", " + comboBoxScriptName.Text + ";");

            LoadScriptList();
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            string downloadList = "";

            foreach (Object items in checkedListBoxScript.Items)
            {
                bool toWrite = true;
                foreach (Object selectedItem in checkedListBoxScript.CheckedItems)
                {
                    if (selectedItem == items)
                        toWrite = false;
                }
                if (toWrite == true)
                    downloadList = downloadList + items + ";";
            }
            IniFile script = new IniFile(CombinePath(SCRIPT_PATH, Global.group, Global.type, Global.name));
            script.IniWriteValue("info", "script_list", downloadList);

            LoadScriptList();
        }

        private void checkBoxChangeFolder_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxChangeFolder.Checked == true)
                textBoxFolderForWAD.Enabled = true;
            else
                textBoxFolderForWAD.Enabled = false;
        }

        private void UpdateNUSList()
        {
            IniFile nus_inifile = new IniFile(CombinePath(NUS_PATH, "nusDatabase.ini"));
            char[] delimit = new char[] { ',' };
            //  bool regionFree;
            string titleList;

            comboBoxNameNUS.Items.Clear();
            textBoxWadName.Text = "";
            comboBoxNameNUS.Text = "";
            comboBoxNameNUS.Enabled = false;
            comboBoxVersionNUS.Enabled = false;
            comboBoxVersionNUS.Items.Clear();
            comboBoxVersionNUS.Text = "";
            buttonAddNUS.Enabled = false;
            textBoxSlot.Text = "";
            textBoxSlot.Enabled = false;
            textBoxNewVersion.Text = "";
            textBoxNewVersion.Enabled = false;
            checkBoxNoPatch.Enabled = false;
            checkBoxFakeSigning.Enabled = false;
            checkBoxEsIdentify.Enabled = false;
            checkBoxNandPermission.Enabled = false;
            checkBoxVersion.Enabled = false;

            if (radioButtonIOS.Checked == true)
            {
                titleList = nus_inifile.IniReadValue("IOS", "list");
                comboBoxRegionNUS.Enabled = false;
                comboBoxRegionNUS.Text = "";
            }
            else if (radioButtonSystem.Checked == true)
            {
                comboBoxRegionNUS.Enabled = true;
                if (comboBoxRegionNUS.Text.Trim() == "")
                    return;
                titleList = nus_inifile.IniReadValue("System", "list" + comboBoxRegionNUS.Text.Substring(1, 1));
            }
            else
                return;

            foreach (string title in titleList.Split(delimit))
            {
                if ((title != null) && (title.Trim() != ""))
                    comboBoxNameNUS.Items.Add(title);
            }
            if (comboBoxNameNUS.Items.Count > 0)
                comboBoxNameNUS.Enabled = true;
        }

        private void UpdateNUSVersion_List()
        {
            IniFile nus_inifile = new IniFile(CombinePath(NUS_PATH, "nusDatabase.ini"));
            char[] delimit = new char[] { ',' };

            string ID, region, versionList;

            comboBoxVersionNUS.Items.Clear();
            comboBoxVersionNUS.Text = "";

            if (radioButtonIOS.Checked == true)
                region = "";
            else if (nus_inifile.IniReadValue(comboBoxNameNUS.Text, "RegionFree") == "True")
                region = "";
            else
                region = comboBoxRegionNUS.Text.Substring(1, 1);

            ID = nus_inifile.IniReadValue(comboBoxNameNUS.Text, "ID" + region);
            versionList = nus_inifile.IniReadValue(comboBoxNameNUS.Text, "version" + region);

            foreach (string title in versionList.Split(delimit))
            {
                if ((title != null) && (title.Trim() != ""))
                    comboBoxVersionNUS.Items.Add(title);
            }
            if (comboBoxVersionNUS.Items.Count > 0)
            {
                comboBoxVersionNUS.Enabled = true;
                if (comboBoxVersionNUS.Text.Trim() != "")
                {
                    buttonAddNUS.Enabled = true;
                    if (radioButtonIOS.Checked == true)
                    {
                        textBoxSlot.Enabled = true;
                        textBoxNewVersion.Enabled = true;
                        checkBoxNoPatch.Enabled = true;
                        checkBoxFakeSigning.Enabled = true;
                        checkBoxEsIdentify.Enabled = true;
                        checkBoxNandPermission.Enabled = true;
                        checkBoxVersion.Enabled = true;
                    }
                    else
                    {
                        textBoxSlot.Enabled = false;
                        textBoxNewVersion.Enabled = false;
                        checkBoxNoPatch.Enabled = false;
                        checkBoxFakeSigning.Enabled = false;
                        checkBoxEsIdentify.Enabled = false;
                        checkBoxNandPermission.Enabled = false;
                        checkBoxVersion.Enabled = false;
                    }
                }
                else
                {
                    buttonAddNUS.Enabled = false;
                    textBoxSlot.Enabled = false;
                    textBoxNewVersion.Enabled = false;
                    checkBoxNoPatch.Enabled = false;
                    checkBoxFakeSigning.Enabled = false;
                    checkBoxEsIdentify.Enabled = false;
                    checkBoxNandPermission.Enabled = false;
                    checkBoxVersion.Enabled = false;
                }
            }

        }

        private string setBaseWad(string ciosVersion, string ciosSlot, string ciosBase)
        {
            switch (ciosVersion)
            {
                case "Hermes-v4":
                    switch (ciosSlot)
                    {
                        case "222":
                            return "cIOS222[38]-v4";
                        case "223":
                            return "cIOS223[37-38]-v4";
                    }
                    break;
                case "Hermes-v5":
                    switch (ciosSlot)
                    {
                        case "222":
                            return "cIOS222[38]-v5";
                        case "223":
                            return "cIOS223[37]-v5";
                        case "224":
                            return "cIOS224[57]-v5";
                    }
                    break;
                case "HermesRodries-v5.1":
                    switch (ciosSlot)
                    {
                        case "202":
                            return "cIOS202[60]-v5.1R";
                        case "222":
                            return "cIOS222[38]-v5.1R";
                        case "223":
                            return "cIOS223[37]-v5.1R";
                        case "224":
                            return "cIOS224[57]-v5.1R";
                    }
                    break;
                case "Waninkoko-v14":
                    return "cIOS249-v14";
                case "Waninkoko-v17b":
                    return "cIOS249-v17b";
                case "Waninkoko-v19":
                    switch (ciosBase)
                    {
                        case "37":
                            return "cIOS249[37]-v19";
                        case "38":
                            return "cIOS249[38]-v19";
                        case "57":
                            return "cIOS249[57]-v19";
                    }
                    break;
                case "Waninkoko-v20":
                    switch (ciosBase)
                    {
                        case "38":
                            return "cIOS249[38]-v20";
                        case "56":
                            return "cIOS249[56]-v20";
                        case "57":
                            return "cIOS249[57]-v20";
                    }
                    break;
                case "Waninkoko-v21":
                    switch (ciosBase)
                    {
                        case "37":
                            return "cIOS249[37]-v21";
                        case "38":
                            return "cIOS249[38]-v21";
                        case "53":
                            return "cIOS249[53]-v21";
                        case "55":
                            return "cIOS249[55]-v21";
                        case "56":
                            return "cIOS249[56]-v21";
                        case "57":
                            return "cIOS249[57]-v21";
                        case "58":
                            return "cIOS249[58]-v21";
                    }
                    break;
                case "d2x-v1-final":
                case "d2x-v2-final":
                case "d2x-v3-final":
                case "d2x-v4-final":
                case "d2x-v5-final":
                case "d2x-v6-final":
                case "d2x-v7-final":
                case "d2x-v8-final":
                case "d2x-v9-beta(r47)":
                case "d2x-v9-beta(r49)":
                case "d2x-v10-beta52":
                case "d2x-v10-beta53-alt":
                    switch (ciosBase)
                    {
                        case "37":
                            return "cIOS249[37]-d2x-v8-final";
                        case "38":
                            return "cIOS249[38]-d2x-v8-final";
                        case "53":
                            return "cIOS249[53]-d2x-v8-final";
                        case "55":
                            return "cIOS249[55]-d2x-v8-final";
                        case "56":
                            return "cIOS249[56]-d2x-v8-final";
                        case "57":
                            return "cIOS249[57]-d2x-v8-final";
                        case "58":
                            return "cIOS249[58]-d2x-v8-final";
                        case "60":
                            return "cIOS249[60]-d2x-v8-final";
                        case "70":
                            return "cIOS249[70]-d2x-v8-final";
                        case "80":
                            return "cIOS249[80]-d2x-v8-final";
                    }
                    break;
                case "System menu patched IOS":
                    switch (ciosSlot)
                    {                        
                        case "11":
                            return "IOS11P60";
                        case "20":
                            return "IOS20P60";
                        case "30":
                            return "IOS30P60";
                        case "40":
                            return "IOS40P60";
                        case "50":
                            return "IOS50P";
                        case "52":
                            return "IOS52P";
                        case "60":
                            return "IOS60P";                       
                        case "70":
                            return "IOS70K";
                        case "80":
                            return "IOS80K";
                    }
                    break;
            }
            return "";


        }

        private void AddCIOS()
        {
            if (!CheckValues())
                return;

            SaveValues();

            string base_wad = setBaseWad(comboBoxVersionCIOS.Text, comboBoxSlotForCIOS.Text, comboBoxBaseForCIOS.Text);
            IniFile script = new IniFile(CombinePath(CUSTOM_SCRIPT_PATH, Global.type, Global.name));
            string downloadList = script.IniReadValue("info", "cios_list");

            script.IniWriteValue("info", "cios_list", downloadList + textBoxNameForCIOS.Text + "\t\t\t\t, " + comboBoxVersionCIOS.Text + ", " + comboBoxSlotForCIOS.Text + ", " + comboBoxBaseForCIOS.Text + ", " + textBoxVersionForCIOS.Text + ", " + base_wad + ";");

            LoadCiosList();

        }

        private void AddNUStoScriptList()
        {
            if (!CheckValues())
                return;

            SaveValues();
            IniFile nus_inifile = new IniFile(CombinePath(NUS_PATH, "nusDatabase.ini"));
            IniFile script = new IniFile(CombinePath(CUSTOM_SCRIPT_PATH, Global.type, Global.name));
            string downloadList = script.IniReadValue("info", "nus_list");

            string ID, region, version, description;//, wadName;  

            description = "";

            if (radioButtonIOS.Checked == true)
                region = "";
            else if (nus_inifile.IniReadValue(comboBoxNameNUS.Text, "regionFree") == "True")
                region = "";
            else
            {
                region = comboBoxRegionNUS.Text.Substring(1, 1);
                description = description + '[' + region + ']';
            }

            ID = nus_inifile.IniReadValue(comboBoxNameNUS.Text, "ID" + region);
            version = "";
            for (int i = 0; i < comboBoxVersionNUS.Text.Length; i++)
            {
                if (comboBoxVersionNUS.Text[i] != ' ')
                    version = version + comboBoxVersionNUS.Text[i];
                else
                    break;
            }

            description = textBoxWadName.Text.Substring(0, textBoxWadName.Text.Length - 4) + description;

            script.IniWriteValue("info", "nus_list", downloadList + description + "\t\t\t\t\t" + "," + textBoxWadName.Text + "," + ID + "," + version + "," + patchForScript + " ," + versionForScript + " ," + slotForScript + " ," + comboBoxNameNUS.Text + "v" + version + ";");

            LoadNusList();

        }

        private void createWadNameForNus()
        {
            string version = "";
            IniFile nus_inifile = new IniFile(CombinePath(NUS_PATH, "nusDatabase.ini"));

            for (int i = 0; i < comboBoxVersionNUS.Text.Length; i++)
            {
                if (comboBoxVersionNUS.Text[i] != ' ')
                    version = version + comboBoxVersionNUS.Text[i];
                else
                    break;
            }

            if (radioButtonSystem.Checked == true)
            {
                textBoxWadName.Text = nus_inifile.IniReadValue(comboBoxNameNUS.Text, "name") + version + ".wad";
                textBoxSlot.Enabled = false;
                textBoxNewVersion.Enabled = false;
                checkBoxNoPatch.Enabled = false;
                checkBoxFakeSigning.Enabled = false;
                checkBoxEsIdentify.Enabled = false;
                checkBoxNandPermission.Enabled = false;
                checkBoxVersion.Enabled = false;
                return;
            }

            string newIos, newVersion, patch = "";
            bool iosChanged = false;
            bool versionChanged = false;
            bool isStub = false;
            bool isPatched = false;
            textBoxSlot.Enabled = true;
            textBoxNewVersion.Enabled = true;
            checkBoxNoPatch.Enabled = true;
            checkBoxFakeSigning.Enabled = true;
            checkBoxEsIdentify.Enabled = true;
            checkBoxNandPermission.Enabled = true;
            checkBoxVersion.Enabled = true;

            if (comboBoxVersionNUS.Text.Length > 6)
            {
                if (comboBoxVersionNUS.Text.Substring(comboBoxVersionNUS.Text.Length - 6, 6) == "[stub]")
                    isStub = true;
            }

            if (isStub)
            {
                textBoxSlot.Enabled = false;
                textBoxSlot.Text = "";
                textBoxNewVersion.Enabled = false;
                textBoxNewVersion.Text = "";
                checkBoxNoPatch.Enabled = true;
                checkBoxNoPatch.Checked = true;
                checkBoxFakeSigning.Enabled = false;
                checkBoxEsIdentify.Enabled = false;
                checkBoxNandPermission.Enabled = false;
                checkBoxVersion.Enabled = false;
            }


            patchForScript = "";

            if (checkBoxNoPatch.Checked)
                patch = "";
            else
            {
                if (checkBoxFakeSigning.Checked)
                {
                    patch = "[FS";
                    patchForScript = patchForScript + "FS";
                }
                if (checkBoxEsIdentify.Checked)
                {
                    if (patch != "")
                        patch = patch + "-ES";
                    else
                        patch = "[ES";
                    patchForScript = patchForScript + "ES";
                }

                if (checkBoxNandPermission.Checked)
                {
                    if (patch != "")
                        patch = patch + "-NP";
                    else
                        patch = "[NP";
                    patchForScript = patchForScript + "NP";
                }

                if (checkBoxVersion.Checked)
                {
                    if (patch != "")
                        patch = patch + "-VP";
                    else
                        patch = "[VP";
                    patchForScript = patchForScript + "VP";
                }

                if (patch != "")
                {
                    patch = patch + "]";
                    isPatched = true;
                }
            }

            slotForScript = "";
            // searching in witch IOS install
            if (textBoxSlot.Text.Trim() == "")
            {
                newIos = comboBoxNameNUS.Text;
                buttonAddNUS.Enabled = true;
            }
            else
            {
                int Num;
                bool isNum = int.TryParse(textBoxSlot.Text, out Num);

                if (isNum && Num < 255)
                {
                    if (textBoxSlot.Text.Length == 1 && Num < 3)
                        buttonAddNUS.Enabled = false;
                    else if (textBoxSlot.Text.Length > 1)
                        buttonAddNUS.Enabled = true;
                    else
                        buttonAddNUS.Enabled = true;

                    newIos = "IOS" + textBoxSlot.Text;
                    slotForScript = textBoxSlot.Text;
                    iosChanged = true;
                }
                else
                {
                    buttonAddNUS.Enabled = true;
                    textBoxSlot.Text = "";
                    newIos = comboBoxNameNUS.Text;
                }
            }

            // searching using version....
            versionForScript = "";
            if (textBoxNewVersion.Text.Trim() == "")
            {
                newVersion = "v" + version;
            }
            else
            {
                int Num = 0;
                bool isNum = int.TryParse(textBoxNewVersion.Text, out Num);

                if (isNum && Num < 65536)
                {
                    newVersion = "v" + textBoxNewVersion.Text;
                    versionForScript = textBoxNewVersion.Text;
                    versionChanged = true;
                }
                else
                {
                    textBoxNewVersion.Text = "";
                    newVersion = "v" + version;
                }
            }

            if (iosChanged || versionChanged)
                textBoxWadName.Text = newIos + newVersion + "(" + comboBoxNameNUS.Text + "-v" + version + patch + ").wad";
            else if (isPatched)
                textBoxWadName.Text = comboBoxNameNUS.Text + "-v" + version + patch + ".wad";
            else
                textBoxWadName.Text = comboBoxNameNUS.Text + "-64-v" + version + patch + ".wad";

        }

        private void radioButtonIOS_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxRegionNUS.Enabled = false;

            checkBoxNoPatch.Checked = true;
            textBoxNewVersion.Text = "";
            textBoxSlot.Text = "";

            comboBoxRegionNUS.Items.Clear();

            UpdateNUSList();
        }

        private void radioButtonSystem_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxRegionNUS.Enabled = true;

            checkBoxNoPatch.Checked = true;
            textBoxNewVersion.Text = "";
            textBoxSlot.Text = "";            
            patchForScript = "";
            versionForScript = "";
            slotForScript = "";

            comboBoxRegionNUS.Items.Clear();

            comboBoxRegionNUS.Items.Add("(E) - European");
            comboBoxRegionNUS.Items.Add("(U) - American");
            comboBoxRegionNUS.Items.Add("(J) - Japanese");
            comboBoxRegionNUS.Items.Add("(K) - Korean");

            UpdateNUSList();
        }

        private void comboBoxRegionNUS_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateNUSList();
        }

        private void comboBoxNameNUS_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkBoxNoPatch.Checked = true;
            textBoxNewVersion.Text = "";
            textBoxSlot.Text = "";
            UpdateNUSVersion_List();
        }

        private void buttonAddNUS_Click(object sender, EventArgs e)
        {
            AddNUStoScriptList();
        }

        private void comboBoxVersionNUS_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxVersionNUS.Text.Trim() != "")
            {
                buttonAddNUS.Enabled = true;
                checkBoxNoPatch.Checked = true;
                textBoxNewVersion.Text = "";
                textBoxSlot.Text = "";
                createWadNameForNus();
            }
            else
                buttonAddNUS.Enabled = false;
        }

        private void buttonRemoveNUS_Click(object sender, EventArgs e)
        {
            string downloadList = "";

            foreach (Object items in checkedListBoxNus.Items)
            {
                bool toWrite = true;
                foreach (Object selectedItem in checkedListBoxNus.CheckedItems)
                {
                    if (selectedItem == items)
                        toWrite = false;
                }
                if (toWrite == true)
                    downloadList = downloadList + items + ";";
            }
            IniFile script = new IniFile(CombinePath(SCRIPT_PATH, Global.group, Global.type, Global.name));
            script.IniWriteValue("info", "nus_list", downloadList);

            LoadNusList();
        }

        private void textBoxFileToRename_TextChanged(object sender, EventArgs e)
        {
            if (textBoxFileToRename.Text.Trim() == "")
                textBoxRenameIn.Enabled = false;
            else
                textBoxRenameIn.Enabled = true;
        }

        private void textBoxName_TextChanged(object sender, EventArgs e)
        {
            if (!checkForNotValidChar(textBoxName.Text))
                MessageBox.Show(Dictionary.notValidChar, this.Text + " - " + WiiDownloader.WiiDownloader_Form.Dictionary.error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }


        private void textBoxSlot_TextChanged(object sender, EventArgs e)
        {
            createWadNameForNus();
        }

        private void textBoxNewVersion_TextChanged(object sender, EventArgs e)
        {
            createWadNameForNus();
        }

        private void checkBoxNoPatch_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxNoPatch.Checked == true)
            {
                checkBoxFakeSigning.Checked = false;
                checkBoxEsIdentify.Checked = false;
                checkBoxNandPermission.Checked = false;
                checkBoxVersion.Checked = false;
            }
            if ((checkBoxNoPatch.Checked == false) &&
                (checkBoxFakeSigning.Checked == false) &&
                (checkBoxEsIdentify.Checked == false) &&
                (checkBoxNandPermission.Checked == false) &&
                (checkBoxVersion.Checked == false))
                checkBoxNoPatch.Checked = true;

            createWadNameForNus();
        }

        private void checkBoxFakeSigning_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxNoPatch.Checked = false;
            if ((checkBoxNoPatch.Checked == false) &&
                (checkBoxFakeSigning.Checked == false) &&
                (checkBoxEsIdentify.Checked == false) &&
                (checkBoxNandPermission.Checked == false) &&
                (checkBoxVersion.Checked == false))
                checkBoxNoPatch.Checked = true;

            createWadNameForNus();
        }

        private void checkBoxEsIdentify_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxNoPatch.Checked = false;
            if ((checkBoxNoPatch.Checked == false) &&
                (checkBoxFakeSigning.Checked == false) &&
                (checkBoxEsIdentify.Checked == false) &&
                (checkBoxNandPermission.Checked == false) &&
                (checkBoxVersion.Checked == false))
                checkBoxNoPatch.Checked = true;

            createWadNameForNus();
        }

        private void checkBoxNandPermission_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxNoPatch.Checked = false;
            if ((checkBoxNoPatch.Checked == false) &&
                (checkBoxFakeSigning.Checked == false) &&
                (checkBoxEsIdentify.Checked == false) &&
                (checkBoxNandPermission.Checked == false) &&
                (checkBoxVersion.Checked == false))
                checkBoxNoPatch.Checked = true;

            createWadNameForNus();
        }

        private void checkBoxVersion_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxNoPatch.Checked = false;
            if ((checkBoxNoPatch.Checked == false) &&
                (checkBoxFakeSigning.Checked == false) &&
                (checkBoxEsIdentify.Checked == false) &&
                (checkBoxNandPermission.Checked == false) &&
                (checkBoxVersion.Checked == false))
                checkBoxNoPatch.Checked = true;

            createWadNameForNus();
        }

        private void checkBoxSearchImage_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSearchImage.Checked)
            {
                textBoxImageFileLink.Size = new System.Drawing.Size(0, 0);
                textBoxWhiteText.Size = new System.Drawing.Size(164, 25);
                textBoxWhiteText.Enabled = false;
            }
            else
            {
                textBoxImageFileLink.Size = new System.Drawing.Size(164, 25);
                textBoxWhiteText.Size = new System.Drawing.Size(0, 0);
                textBoxWhiteText.Enabled = true;
            }

        }

        private void radioButtonStaticLink_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonStaticLink.Checked == true)
            {
                textBoxActualLink.Visible = false;
                comboBoxDownloadFrom.Visible = false;
                labelDownloadFrom.Visible = false;
                labelActualLink.Visible = false;
                textBoxLink.Enabled = true;
                textBoxLink.Text = "";
            }
        }

        private void radioButtonDinamicLink_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonDinamicLink.Checked == true)
            {
                textBoxActualLink.Visible = true;
                comboBoxDownloadFrom.Visible = true;
                labelDownloadFrom.Visible = true;
                labelActualLink.Visible = true;

                if (comboBoxDownloadFrom.Text == "bootmii.org")
                {
                    textBoxLink.Enabled = false;
                    textBoxLink.Text = "http://bootmii.org/download/";
                }
                else
                {
                    textBoxLink.Enabled = true;
                    textBoxLink.Text = "";
                }
            }
        }

        private void comboBoxDownloadFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxDownloadFrom.Text == "bootmii.org")
            {
                textBoxLink.Enabled = false;
                textBoxLink.Text = "http://bootmii.org/download/";
            }
            else
            {
                textBoxLink.Enabled = true;
                textBoxLink.Text = "";
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            setSlotForCIOS();
        }

        private void comboBoxSlotForCIOS_SelectedIndexChanged(object sender, EventArgs e)
        {
            setBaseForCIOS();
        }

        private void comboBoxBaseForCIOS_SelectedIndexChanged(object sender, EventArgs e)
        {
            setNameForCIOS();
        }

        private void buttonAddCIOS_Click(object sender, EventArgs e)
        {
            AddCIOS();
        }

        private void buttonRemoveCIOS_Click(object sender, EventArgs e)
        {
            string downloadList = "";

            foreach (Object items in checkedListBoxCIOS.Items)
            {
                bool toWrite = true;
                foreach (Object selectedItem in checkedListBoxCIOS.CheckedItems)
                {
                    if (selectedItem == items)
                        toWrite = false;
                }
                if (toWrite == true)
                    downloadList = downloadList + items + ";";
            }
            IniFile script = new IniFile(CombinePath(SCRIPT_PATH, Global.group, Global.type, Global.name));
            script.IniWriteValue("info", "cios_list", downloadList);

            LoadCiosList();
        }

        private void radioButtonForMoveFile_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonForMoveFile.Checked == true)
                textBoxFileToTheRoot.Enabled = true;
            else
                textBoxFileToTheRoot.Enabled = false;

        }

        private void buttonWarning_Click(object sender, EventArgs e)
        {            
            string SETTINGS_PATH = CombinePath(DATABASE_PATH, "settings");
            string SETTINGS_INI_FILE = CombinePath(SETTINGS_PATH, "settings.ini");
            
            IniFile settingsFile = new IniFile(SETTINGS_INI_FILE);

            settingsFile.IniWriteValue("warning", "warning_1", "OK");

            buttonWarning.Visible = false;

        }
        

    }
}

