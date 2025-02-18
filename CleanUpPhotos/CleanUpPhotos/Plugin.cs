using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DoenaSoft.DVDProfiler.DVDProfilerHelper;
using DoenaSoft.DVDProfiler.DVDProfilerXML;
using DoenaSoft.ToolBox.Generics;
using Invelos.DVDProfilerPlugin;

namespace DoenaSoft.DVDProfiler.CleanUpPhotos
{
    [ComVisible(true)]
    [Guid("447536AA-253C-4626-B5D0-C543492CC3B2")]
    public class Plugin : IDVDProfilerPlugin, IDVDProfilerPluginInfo
    {
        internal static Settings Settings;

        private readonly String SettingsFile;

        private readonly String ErrorFile;

        private readonly String ApplicationPath;

        private IDVDProfilerAPI Api;

        private const Int32 MenuId = 1;

        private String MenuTokenISCP = "";

        static Plugin()
        {
            DVDProfilerXMLAssemblyLoader.Load();
        }

        public Plugin()
        {
            ApplicationPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Doena Soft\Clean Up Photos\";
            SettingsFile = ApplicationPath + "CleanUpPhotosSettings.xml";
            ErrorFile = Environment.GetEnvironmentVariable("TEMP") + @"\CleanUpPhotosCrash.xml";
        }

        #region IDVDProfilerPlugin Members
        public void Load(IDVDProfilerAPI api)
        {
            Api = api;
            if (Directory.Exists(ApplicationPath) == false)
            {
                Directory.CreateDirectory(ApplicationPath);
            }
            if (File.Exists(SettingsFile))
            {
                try
                {
                    Settings = Settings.Deserialize(SettingsFile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format(MessageBoxTexts.FileCantBeRead, SettingsFile, ex.Message)
                        , MessageBoxTexts.ErrorHeader, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            CreateSettings();
            MenuTokenISCP = Api.RegisterMenuItem(PluginConstants.FORMID_Main, PluginConstants.MENUID_Form
                , "Tools", Texts.PluginName, MenuId);
        }

        public void Unload()
        {
            Api.UnregisterMenuItem(MenuTokenISCP);
            try
            {
                Settings.Serialize(SettingsFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format(MessageBoxTexts.FileCantBeWritten, SettingsFile, ex.Message)
                    , MessageBoxTexts.ErrorHeader, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Api = null;
        }

        public void HandleEvent(Int32 EventType, Object EventData)
        {
            if (EventType == PluginConstants.EVENTID_CustomMenuClick)
            {
                this.HandleMenuClick((Int32)EventData);
            }
        }
        #endregion

        #region IDVDProfilerPluginInfo Members
        public String GetName()
        {
            //if(Settings.DefaultValues.UiCulture!=0)
            //{
            //    return (Texts.ResourceManager.GetString("PluginName", CultureInfo.GetCultureInfo(Settings.DefaultValues.UiCulture)));
            //}
            return (Texts.PluginName);
        }

        public String GetDescription()
        {
            //if(Settings.DefaultValues.UiCulture != 0)
            //{
            //    return (Texts.ResourceManager.GetString("PluginDescription", CultureInfo.GetCultureInfo(Settings.DefaultValues.UiCulture)));
            //}
            return (Texts.PluginDescription);
        }

        public String GetAuthorName()
        {
            return ("Doena Soft.");
        }

        public String GetAuthorWebsite()
        {
            //if(Settings.DefaultValues.UiCulture != 0)
            //{
            //    return (Texts.ResourceManager.GetString("PluginUrl", CultureInfo.GetCultureInfo(Settings.DefaultValues.UiCulture)));
            //}
            return (Texts.PluginUrl);
        }

        public Int32 GetPluginAPIVersion()
        {
            return (PluginConstants.API_VERSION);
        }

        public Int32 GetVersionMajor()
        {
            Version version;

            version = System.Reflection.Assembly.GetAssembly(this.GetType()).GetName().Version;
            return (version.Major);
        }

        public Int32 GetVersionMinor()
        {
            Version version;

            version = System.Reflection.Assembly.GetAssembly(this.GetType()).GetName().Version;
            return (version.Minor * 100 + version.Build * 10 + version.Revision);
        }
        #endregion

        private void HandleMenuClick(Int32 MenuEventID)
        {
            if (MenuEventID == MenuId)
            {
                try
                {
                    using (MainForm mainForm = new MainForm(Api))
                    {
                        mainForm.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        ExceptionXml exceptionXml;

                        MessageBox.Show(String.Format(MessageBoxTexts.CriticalError, ex.Message, ErrorFile)
                            , MessageBoxTexts.CriticalErrorHeader, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        if (File.Exists(ErrorFile))
                        {
                            File.Delete(ErrorFile);
                        }
                        exceptionXml = new ExceptionXml(ex);
                        XmlSerializer<ExceptionXml>.Serialize(ErrorFile, exceptionXml);
                    }
                    catch (Exception inEx)
                    {
                        MessageBox.Show(String.Format(MessageBoxTexts.FileCantBeWritten, ErrorFile, inEx.Message), MessageBoxTexts.ErrorHeader
                            , MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private static void CreateSettings()
        {
            if (Settings == null)
            {
                Settings = new Settings();
            }
            if (Settings.MainForm == null)
            {
                Settings.MainForm = new SizableForm();
            }
            if (Settings.DefaultValues == null)
            {
                Settings.DefaultValues = new DefaultValues();
            }
        }

        #region Plugin Registering
        [DllImport("user32.dll")]
        public extern static int SetParent(int child, int parent);

        [ComImport(), Guid("0002E005-0000-0000-C000-000000000046")]
        internal class StdComponentCategoriesMgr { }

        [ComRegisterFunction()]
        public static void RegisterServer(Type t)
        {
            CategoryRegistrar.ICatRegister cr = (CategoryRegistrar.ICatRegister)new StdComponentCategoriesMgr();
            Guid clsidThis = new Guid("447536AA-253C-4626-B5D0-C543492CC3B2");
            Guid catid = new Guid("833F4274-5632-41DB-8FC5-BF3041CEA3F1");

            cr.RegisterClassImplCategories(ref clsidThis, 1,
                new Guid[] { catid });
        }

        [ComUnregisterFunction()]
        public static void UnregisterServer(Type t)
        {
            CategoryRegistrar.ICatRegister cr = (CategoryRegistrar.ICatRegister)new StdComponentCategoriesMgr();
            Guid clsidThis = new Guid("447536AA-253C-4626-B5D0-C543492CC3B2");
            Guid catid = new Guid("833F4274-5632-41DB-8FC5-BF3041CEA3F1");

            cr.UnRegisterClassImplCategories(ref clsidThis, 1,
                new Guid[] { catid });
        }
        #endregion
    }
}