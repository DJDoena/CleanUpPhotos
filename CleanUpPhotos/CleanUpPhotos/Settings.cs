using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;
using DoenaSoft.DVDProfiler.CleanUpPhotos;
using System.Runtime.InteropServices;

namespace DoenaSoft.DVDProfiler.CleanUpPhotos
{
    [ComVisible(false)]
    [Serializable()]
    public class BaseForm
    {
        public Int32 Top = 50;

        public Int32 Left = 50;
    }

    [ComVisible(false)]
    [Serializable()]
    public class SizableForm : BaseForm
    {
        public Int32 Height = 500;

        public Int32 Width = 900;

        public FormWindowState WindowState = FormWindowState.Normal;

        public Rectangle RestoreBounds;
    }

    [ComVisible(false)]
    [Serializable()]
    public class Settings
    {
        public SizableForm MainForm;

        public DefaultValues DefaultValues;

        public String CurrentVersion;

        private static XmlSerializer s_XmlSerializer;

        [XmlIgnore()]
        public static XmlSerializer XmlSerializer
        {
            get
            {
                if(s_XmlSerializer == null)
                {
                    s_XmlSerializer = new XmlSerializer(typeof(Settings));
                }
                return (s_XmlSerializer);
            }
        }

        public static void Serialize(String fileName, Settings instance)
        {
            using(FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using(XmlTextWriter xtw = new XmlTextWriter(fs, Encoding.UTF8))
                {
                    xtw.Formatting = Formatting.Indented;
                    XmlSerializer.Serialize(xtw, instance);
                }
            }
        }

        public void Serialize(String fileName)
        {
            Serialize(fileName, this);
        }

        public static Settings Deserialize(String fileName)
        {
            using(FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using(XmlTextReader xtr = new XmlTextReader(fs))
                {
                    Settings instance;

                    instance = (Settings)(XmlSerializer.Deserialize(xtr));
                    return (instance);
                }
            }
        }
    }
}