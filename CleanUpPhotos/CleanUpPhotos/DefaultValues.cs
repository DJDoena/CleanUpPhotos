using System;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace DoenaSoft.DVDProfiler.CleanUpPhotos
{
    [ComVisible(false)]
    [Serializable()]
    public class DefaultValues
    {
        public String CollectionFile = @"C:\collection.xml";

        public Boolean CleanUpCoverImages = false;

        public String CurrentVersion = String.Empty;

        [XmlIgnore()]
        internal String CreditPhotosFolder
        {
            get
            {
                RegistryKey regKey;
                String path;

                regKey = Registry.CurrentUser.OpenSubKey(@"Software\Invelos Software\DVD Profiler", false);
                path = String.Empty;
                if (regKey != null)
                {
                    path = (String)(regKey.GetValue("PathCreditPhotos", String.Empty));
                }
                return (path);
            }
        }

        [XmlIgnore()]
        internal String ScenePhotosFolder
        {
            get
            {
                RegistryKey regKey;
                String path;

                regKey = Registry.CurrentUser.OpenSubKey(@"Software\Invelos Software\DVD Profiler", false);
                path = String.Empty;
                if (regKey != null)
                {
                    path = (String)(regKey.GetValue("PathScenePhotos", String.Empty));
                }
                return (path);
            }
        }
    }
}