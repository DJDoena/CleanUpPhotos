using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using DoenaSoft.DVDProfiler.DVDProfilerHelper;
using DoenaSoft.DVDProfiler.DVDProfilerXML;
using DoenaSoft.DVDProfiler.DVDProfilerXML.Version400;
using DoenaSoft.ToolBox.Generics;
using Invelos.DVDProfilerPlugin;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace DoenaSoft.DVDProfiler.CleanUpPhotos
{
    internal partial class MainForm : Form
    {
        #region Inner Classes
        private class ListBoxItem
        {
            internal String Name;

            internal ListBoxItem(String name)
            {
                Name = name;
            }

            public override String ToString()
            {
                return (Name);
            }
        }

        private class ListBoxItemWithDVD : ListBoxItem
        {
            internal DVD DVD;

            internal ListBoxItemWithDVD(String name, DVD dvd)
                : base(name)
            {
                DVD = dvd;
            }
        }

        private class ListBoxItemWithDVDList : ListBoxItem
        {
            internal List<DVD> DVDList;

            internal ListBoxItemWithDVDList(String name, List<DVD> dvdList)
                : base(name)
            {
                DVDList = dvdList;
            }
        }

        private class ListBoxItemWithNames : ListBoxItem
        {
            internal DVD DVD;

            internal List<String> ValidList;

            internal List<String> InvalidList;

            internal ListBoxItemWithNames(String name, DVD dvd, List<String> validList, List<String> invalidList)
                : base(name)
            {
                DVD = dvd;
                ValidList = validList;
                InvalidList = invalidList;
            }
        }
        #endregion

        #region Fields
        private readonly IDVDProfilerAPI Api;

        private ProgressWindow ProgressWindow;

        private Boolean CanClose;

        private Collection Collection;
        #endregion

        #region Delegates
        private delegate void ProgressBarDelegate();

        private delegate String GetProfileDataDelegate(Object id);

        private delegate void ThreadFinishedDelegate(Collection collection, String coverImagesPath);
        #endregion

        #region Constructor
        public MainForm(IDVDProfilerAPI api)
        {
            Api = api;
            CanClose = true;
            this.InitializeComponent();
        }
        #endregion

        #region Form Events
        private void OnCheckForUpdateToolStripMenuItemClick(Object sender, EventArgs e)
        {
            this.CheckForNewVersion();
        }

        private void OnAboutToolStripMenuItemClick(Object sender, EventArgs e)
        {
            using (AboutBox aboutBox = new AboutBox(this.GetType().Assembly))
            {
                aboutBox.ShowDialog();
            }
        }

        private void OnMainFormLoad(Object sender, EventArgs e)
        {
            this.LayoutForm();
            CleanCoverImagesCheckBox.Checked = Plugin.Settings.DefaultValues.CleanUpCoverImages;
            SelectDatabaseFolderButton.Enabled = Plugin.Settings.DefaultValues.CleanUpCoverImages;
            CoverImagesTabControl.Enabled = CleanCoverImagesCheckBox.Checked;
            //this.DatabaseFolderTextbox.Text = Environment.ExpandEnvironmentVariables(Plugin.Settings.DefaultValues.DatabaseFolder);
            DatabaseFolderTextbox.Text = this.GetDatabaseFolder();
            CreditPhotosFolderTextBox.Text = Plugin.Settings.DefaultValues.CreditPhotosFolder;
            ScenePhotosFolderTextbox.Text = Plugin.Settings.DefaultValues.ScenePhotosFolder;
            if (Plugin.Settings.DefaultValues.CurrentVersion != this.GetType().Assembly.GetName().Version.ToString())
            {
                this.OpenReadMe();
                Plugin.Settings.DefaultValues.CurrentVersion = this.GetType().Assembly.GetName().Version.ToString();
            }
        }

        private void OnProcessButtonClick(Object sender, EventArgs e)
        {
            String coverImagesPath;

            #region Init UI
            #region Credit Photos
            #region General
            ValidCreditPhotosFileGeneralListBox.ClearItems();
            ValidCreditPhotosProfileGeneralListBox.ClearItems();
            ResetPictureBox(ValidCreditPhotosGeneralPictureBox);
            InvalidCreditPhotosFileGeneralListBox.ClearItems();
            ResetPictureBox(InvalidCreditPhotosGeneralPictureBox);
            RemoveInvalidCreditPhotosGeneralButton.Enabled = false;
            RemoveAllInvalidCreditPhotosGeneralButton.Enabled = false;
            #endregion
            #region Folder
            ValidCreditPhotosFolderFolderListBox.ClearItems();
            ValidCreditPhotosProfileFolderTextBox.Text = String.Empty;
            ValidCreditPhotosFileFolderListBox.ClearItems();
            ResetPictureBox(ValidCreditPhotosFolderPictureBox);
            InvalidCreditPhotosFolderFolderListBox.ClearItems();
            InvalidCreditPhotosFileFolderListBox.ClearItems();
            ResetPictureBox(InvalidCreditPhotosFolderPictureBox);
            RemoveInvalidCreditPhotosFolderButton.Enabled = false;
            RemoveAllInvalidCreditPhotosFolderButton.Enabled = false;
            #endregion
            #region Profile
            ValidCreditPhotosFolderProfileListBox.ClearItems();
            ValidCreditPhotosProfileProfileTextBox.Text = String.Empty;
            ValidCreditPhotosFileProfileListBox.ClearItems();
            ResetPictureBox(ValidCreditPhotosProfilePictureBox);
            InvalidCreditPhotosFolderProfileListBox.ClearItems();
            InvalidCreditPhotosInvalidFileProfileListBox.ClearItems();
            ResetPictureBox(InvalidCreditPhotosInvalidFileProfilePictureBox);
            InvalidCreditPhotosValidFileProfileListBox.ClearItems();
            ResetPictureBox(InvalidCreditPhotosValidFileProfilePictureBox);
            RemoveInvalidCreditPhotosProfileButton.Enabled = false;
            RemoveAllInvalidCreditPhotosProfileButton.Enabled = false;
            #endregion
            #endregion
            #region Scene Photos
            ValidScenePhotosFolderListBox.ClearItems();
            ValidScenePhotosProfileTextBox.Text = String.Empty;
            ValidScenePhotosFileListBox.ClearItems();
            ResetPictureBox(ValidScenePhotosPictureBox);
            InvalidScenePhotosFolderListBox.ClearItems();
            InvalidScenePhotosFileListBox.ClearItems();
            ResetPictureBox(InvalidScenePhotosPictureBox);
            RemoveInvalidScenePhotosButton.Enabled = false;
            RemoveAllInvalidScenePhotosButton.Enabled = false;
            #endregion
            #region Cover Images
            ValidCoverImagesFolderListBox.ClearItems();
            ValidCoverImagesProfileTextBox.Text = String.Empty;
            ValidCoverImagesFileListBox.ClearItems();
            ResetPictureBox(ValidCoverImagesPictureBox);
            InvalidCoverImagesFolderListBox.ClearItems();
            InvalidCoverImagesFileListBox.ClearItems();
            ResetPictureBox(InvalidCoverImagesPictureBox);
            RemoveInvalidCoverImagesButton.Enabled = false;
            RemoveAllInvalidCoverImagesButton.Enabled = false;
            #endregion
            #endregion
            if (Directory.Exists(CreditPhotosFolderTextBox.Text) == false)
            {
                MessageBox.Show("Credit Photos folder not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (Directory.Exists(ScenePhotosFolderTextbox.Text) == false)
            {
                MessageBox.Show("Scene Photos folder not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            coverImagesPath = String.Empty;
            if (CleanCoverImagesCheckBox.Checked)
            {
                coverImagesPath = Path.Combine(DatabaseFolderTextbox.Text, "Images");
                if (Directory.Exists(coverImagesPath) == false)
                {
                    MessageBox.Show("Cover Images folder file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            CanClose = false;
            this.UseWaitCursor = true;
            this.Cursor = Cursors.WaitCursor;
            if (Collection == null)
            {
                Thread thread;
                Object[] allIds;

                ProgressWindow = new ProgressWindow();
                ProgressWindow.ProgressBar.Minimum = 0;
                ProgressWindow.ProgressBar.Step = 1;
                ProgressWindow.CanClose = false;
                allIds = (Object[])(Api.GetAllProfileIDs());
                ProgressWindow.ProgressBar.Maximum = allIds.Length;
                ProgressWindow.Show();
                if (TaskbarManager.IsPlatformSupported)
                {
                    TaskbarManager.Instance.OwnerHandle = this.Handle;
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                    TaskbarManager.Instance.SetProgressValue(0, ProgressWindow.ProgressBar.Maximum);
                }
                thread = new Thread(new ParameterizedThreadStart(this.ThreadRun));
                thread.IsBackground = false;
                thread.Start(new Object[] { allIds, coverImagesPath });
            }
            else
            {
                this.ThreadFinished(Collection, coverImagesPath);
            }
        }

        private void ThreadFinished(Collection collection, String coverImagesPath)
        {
            Dictionary<String, DVD> validCreditPhotos;

            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
                TaskbarManager.Instance.OwnerHandle = IntPtr.Zero;
            }
            if (ProgressWindow != null)
            {
                ProgressWindow.CanClose = true;
                ProgressWindow.Close();
                ProgressWindow.Dispose();
                ProgressWindow = null;
            }
            Collection = collection;
            if (Collection != null)
            {
                this.ProcessCreditPhotosGeneral(collection.DVDList);
                validCreditPhotos = this.ProcessCreditPhotosFolder(collection.DVDList);
                this.ProcessCreditPhotosProfile(validCreditPhotos);
                this.ProcessScenePhotos(collection.DVDList);
                if (CleanCoverImagesCheckBox.Checked)
                {
                    this.ProcessCoverImages(coverImagesPath, collection.DVDList);
                }
                MessageBox.Show(this, MessageBoxTexts.Done, MessageBoxTexts.InformationHeader, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            this.Cursor = Cursors.Default;
            this.UseWaitCursor = false;
            CanClose = true;
        }

        private void OnReadMeToolStripMenuItemClick(Object sender, EventArgs e)
        {
            this.OpenReadMe();
        }

        private void OnMainFormFormClosing(Object sender, FormClosingEventArgs e)
        {
            if (CanClose == false)
            {
                e.Cancel = true;
                return;
            }
            Plugin.Settings.MainForm.Left = this.Left;
            Plugin.Settings.MainForm.Top = this.Top;
            Plugin.Settings.MainForm.Width = this.Width;
            Plugin.Settings.MainForm.Height = this.Height;
            Plugin.Settings.MainForm.WindowState = this.WindowState;
            Plugin.Settings.MainForm.RestoreBounds = this.RestoreBounds;
        }
        #endregion

        #region Process...
        private void ProcessCoverImages(String path, DVD[] dvdList)
        {
            List<String> coverImages;
            List<ListBoxItem> coverImagesListBoxItems;
            Dictionary<String, DVD> validCoverImages;
            Dictionary<String, Boolean> invalidCoverImages;
            Int32 index;
            Int32 step;

            coverImages = new List<String>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
            coverImages = coverImages.ConvertAll((input =>
                    {
                        FileInfo fi;
                        String file;

                        fi = new FileInfo(input);
                        file = fi.Name;
                        if (fi.Extension.Length > 0)
                        {
                            file = file.Substring(0, file.Length - fi.Extension.Length);
                        }
                        return (file.ToUpper());
                    }
                ));
            validCoverImages = new Dictionary<String, DVD>(coverImages.Count);

            step = this.StartProgress(dvdList.Length);
            index = 0;
            foreach (DVD dvd in dvdList)
            {
                Int32 indexOf1;
                Int32 indexOf2;
                String id;

                id = dvd.ID.ToUpper();
                do
                {
                    indexOf1 = coverImages.IndexOf(id + "F");
                    if (indexOf1 != -1)
                    {
                        if (validCoverImages.ContainsKey(id) == false)
                        {
                            validCoverImages.Add(id, dvd);
                        }
                        coverImages.RemoveAt(indexOf1);
                    }
                    indexOf2 = coverImages.IndexOf(id + "B");
                    if (indexOf2 != -1)
                    {
                        if (validCoverImages.ContainsKey(id) == false)
                        {
                            validCoverImages.Add(id, dvd);
                        }
                        coverImages.RemoveAt(indexOf2);
                    }
                } while ((indexOf1 != -1) || (indexOf2 != -1));

                this.UpdateProgressBar(index, step);
                index++;
            }

            this.EndProgress();

            coverImagesListBoxItems = new List<ListBoxItem>(validCoverImages.Count);
            foreach (KeyValuePair<String, DVD> kvp in validCoverImages)
            {
                coverImagesListBoxItems.Add(new ListBoxItemWithDVD(kvp.Key, kvp.Value));
            }
            coverImagesListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            ValidCoverImagesFolderListBox.Items.AddRange(coverImagesListBoxItems.ToArray());
            if (ValidCoverImagesFolderListBox.Items.Count > 0)
            {
                ValidCoverImagesFolderListBox.SelectedIndex = 0;
            }
            invalidCoverImages = new Dictionary<String, Boolean>(coverImages.Count);
            foreach (String fileName in coverImages)
            {
                String file;

                file = fileName;
                if ((file.EndsWith("F")) || (file.EndsWith("B")))
                {
                    file = file.Substring(0, file.Length - 1);
                }
                if (invalidCoverImages.ContainsKey(file) == false)
                {
                    invalidCoverImages.Add(file, true);
                }
            }
            coverImagesListBoxItems = new List<ListBoxItem>(invalidCoverImages.Count);
            foreach (KeyValuePair<String, Boolean> kvp in invalidCoverImages)
            {
                coverImagesListBoxItems.Add(new ListBoxItem(kvp.Key));
            }
            coverImagesListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            InvalidCoverImagesFolderListBox.Items.AddRange(coverImagesListBoxItems.ToArray());
            if (InvalidCoverImagesFolderListBox.Items.Count > 0)
            {
                InvalidCoverImagesFolderListBox.SelectedIndex = 0;
            }
        }

        private void ProcessScenePhotos(DVD[] dvdList)
        {
            List<String> scenePhotos;
            List<ListBoxItem> scenePhotosListBoxItems;
            Dictionary<String, DVD> validScenePhotos;
            Int32 index;
            Int32 step;

            scenePhotos = new List<String>(Directory.GetDirectories(ScenePhotosFolderTextbox.Text));
            scenePhotos = scenePhotos.ConvertAll<String>(new Converter<String, String>(ConvertAllDirectory));
            validScenePhotos = new Dictionary<String, DVD>(scenePhotos.Count);


            step = this.StartProgress(dvdList.Length);
            index = 0;
            foreach (DVD dvd in dvdList)
            {
                Int32 indexOf;

                indexOf = scenePhotos.IndexOf(dvd.ID.ToUpper());
                if (indexOf != -1)
                {
                    validScenePhotos.Add(scenePhotos[indexOf], dvd);
                    scenePhotos.RemoveAt(indexOf);
                }

                this.UpdateProgressBar(index, step);
                index++;
            }

            this.EndProgress();

            scenePhotosListBoxItems = new List<ListBoxItem>(validScenePhotos.Count);
            foreach (KeyValuePair<String, DVD> kvp in validScenePhotos)
            {
                scenePhotosListBoxItems.Add(new ListBoxItemWithDVD(kvp.Key, kvp.Value));
            }
            scenePhotosListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            ValidScenePhotosFolderListBox.Items.AddRange(scenePhotosListBoxItems.ToArray());
            if (ValidScenePhotosFolderListBox.Items.Count > 0)
            {
                ValidScenePhotosFolderListBox.SelectedIndex = 0;
            }
            scenePhotosListBoxItems = new List<ListBoxItem>(scenePhotos.Count);
            foreach (String folderName in scenePhotos)
            {
                scenePhotosListBoxItems.Add(new ListBoxItem(folderName));
            }
            scenePhotosListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            InvalidScenePhotosFolderListBox.Items.AddRange(scenePhotosListBoxItems.ToArray());
            if (InvalidScenePhotosFolderListBox.Items.Count > 0)
            {
                InvalidScenePhotosFolderListBox.SelectedIndex = 0;
            }
        }

        private Dictionary<String, DVD> ProcessCreditPhotosFolder(DVD[] dvdList)
        {
            List<String> creditPhotos;
            List<ListBoxItem> creditPhotosFolderListBoxItems;
            Dictionary<String, DVD> validCreditPhotos;
            Int32 index;
            Int32 step;

            creditPhotos = new List<String>(Directory.GetDirectories(CreditPhotosFolderTextBox.Text));
            creditPhotos = creditPhotos.ConvertAll<String>(new Converter<String, String>(ConvertAllDirectory));
            validCreditPhotos = new Dictionary<String, DVD>(creditPhotos.Count);

            step = this.StartProgress(dvdList.Length);
            index = 0;

            foreach (DVD dvd in dvdList)
            {
                Int32 indexOf;

                indexOf = creditPhotos.IndexOf(dvd.ID.ToUpper());
                if (indexOf != -1)
                {
                    validCreditPhotos.Add(creditPhotos[indexOf], dvd);
                    creditPhotos.RemoveAt(indexOf);
                }

                this.UpdateProgressBar(index, step);
                index++;
            }

            this.EndProgress();

            creditPhotosFolderListBoxItems = new List<ListBoxItem>(validCreditPhotos.Count);
            foreach (KeyValuePair<String, DVD> kvp in validCreditPhotos)
            {
                creditPhotosFolderListBoxItems.Add(new ListBoxItemWithDVD(kvp.Key, kvp.Value));
            }
            creditPhotosFolderListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            ValidCreditPhotosFolderFolderListBox.Items.AddRange(creditPhotosFolderListBoxItems.ToArray());
            if (ValidCreditPhotosFolderFolderListBox.Items.Count > 0)
            {
                ValidCreditPhotosFolderFolderListBox.SelectedIndex = 0;
            }
            creditPhotosFolderListBoxItems = new List<ListBoxItem>(creditPhotos.Count);
            foreach (String folderName in creditPhotos)
            {
                creditPhotosFolderListBoxItems.Add(new ListBoxItem(folderName));
            }
            creditPhotosFolderListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            InvalidCreditPhotosFolderFolderListBox.Items.AddRange(creditPhotosFolderListBoxItems.ToArray());
            if (InvalidCreditPhotosFolderFolderListBox.Items.Count > 0)
            {
                InvalidCreditPhotosFolderFolderListBox.SelectedIndex = 0;
            }
            return (validCreditPhotos);
        }

        private void ProcessCreditPhotosProfile(Dictionary<String, DVD> validCreditPhotosBase)
        {
            List<ListBoxItemWithNames> creditPhotosProfileListBoxItems;
            List<ListBoxItemWithNames> validCreditPhotosProfileListBoxItems;
            Int32 step;
            Int32 index;

            creditPhotosProfileListBoxItems = new List<ListBoxItemWithNames>(validCreditPhotosBase.Count);

            step = this.StartProgress(validCreditPhotosBase.Count);

            index = 0;
            foreach (KeyValuePair<String, DVD> kvp in validCreditPhotosBase)
            {
                String path;
                List<String> creditPhotos;
                Dictionary<String, Boolean> validCreditPhotos;

                path = Path.Combine(CreditPhotosFolderTextBox.Text, kvp.Key);
                creditPhotos = new List<String>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));

                creditPhotos = creditPhotos.ConvertAll(item => ConvertAllFile(item, path));
                validCreditPhotos = new Dictionary<String, Boolean>(creditPhotos.Count);
                if ((kvp.Value.CastList != null) && (kvp.Value.CastList.Length > 0))
                {
                    foreach (Object potentialCastMember in kvp.Value.CastList)
                    {
                        this.ProcessPerson(validCreditPhotos, creditPhotos, potentialCastMember);
                    }
                }
                if ((kvp.Value.CrewList != null) && (kvp.Value.CrewList.Length > 0))
                {
                    foreach (Object potentialCrewMember in kvp.Value.CrewList)
                    {
                        this.ProcessPerson(validCreditPhotos, creditPhotos, potentialCrewMember);
                    }
                }
                creditPhotosProfileListBoxItems.Add(new ListBoxItemWithNames(kvp.Key, kvp.Value, new List<String>(validCreditPhotos.Keys), creditPhotos));

                this.UpdateProgressBar(index, step);
                index++;
            }

            this.EndProgress();

            validCreditPhotosProfileListBoxItems = new List<ListBoxItemWithNames>(creditPhotosProfileListBoxItems.Count);
            for (Int32 i = creditPhotosProfileListBoxItems.Count - 1; i >= 0; i--)
            {
                creditPhotosProfileListBoxItems[i].InvalidList.Sort();
                creditPhotosProfileListBoxItems[i].ValidList.Sort();
                if (creditPhotosProfileListBoxItems[i].InvalidList.Count == 0)
                {
                    validCreditPhotosProfileListBoxItems.Add(creditPhotosProfileListBoxItems[i]);
                    creditPhotosProfileListBoxItems.RemoveAt(i);
                }
            }
            validCreditPhotosProfileListBoxItems.Sort(new Comparison<ListBoxItemWithNames>(CompareListBoxItems));
            ValidCreditPhotosFolderProfileListBox.Items.AddRange(validCreditPhotosProfileListBoxItems.ToArray());
            if (ValidCreditPhotosFolderProfileListBox.Items.Count > 0)
            {
                ValidCreditPhotosFolderProfileListBox.SelectedIndex = 0;
            }
            creditPhotosProfileListBoxItems.Sort(new Comparison<ListBoxItemWithNames>(CompareListBoxItems));
            InvalidCreditPhotosFolderProfileListBox.Items.AddRange(creditPhotosProfileListBoxItems.ToArray());
            if (InvalidCreditPhotosFolderProfileListBox.Items.Count > 0)
            {
                InvalidCreditPhotosFolderProfileListBox.SelectedIndex = 0;
            }
        }

        private void ProcessPerson(Dictionary<String, Boolean> validCreditPhotos, List<String> creditPhotos, Object potentialPerson)
        {
            IPerson person;

            person = potentialPerson as IPerson;
            if (person != null)
            {
                Boolean success;

                success = false;
                if (person.BirthYear != 0)
                {
                    String extendedfileName;

                    extendedfileName
                        = ProfilePhotoHelper.FileNameFromCreditName(person.FirstName, person.MiddleName, person.LastName, person.BirthYear);
                    success = this.ProcessEntry(validCreditPhotos, creditPhotos, extendedfileName);
                }
                if (success == false)
                {
                    String fileName;

                    fileName = ProfilePhotoHelper.FileNameFromCreditName(person.FirstName, person.MiddleName, person.LastName, 0);
                    this.ProcessEntry(validCreditPhotos, creditPhotos, fileName);
                }
            }
        }

        private Boolean ProcessEntry(Dictionary<String, Boolean> validCreditPhotos, List<String> creditPhotos, String fileName)
        {
            Int32 indexOf;

            fileName = fileName.ToUpper() + ".JPG";

            indexOf = creditPhotos.IndexOf(fileName);
            if (indexOf != -1)
            {
                validCreditPhotos.Add(fileName, true);
                creditPhotos.RemoveAt(indexOf);
                return (true);
            }
            else
            {
                Boolean temp;

                if (validCreditPhotos.TryGetValue(fileName, out temp))
                {
                    return (true);
                }
            }
            return (false);
        }

        private void ProcessCreditPhotosGeneral(DVD[] dvdList)
        {
            Dictionary<String, List<DVD>> validCreditPhotos;
            List<String> creditPhotos;
            List<ListBoxItem> creditPhotosListBoxItems;
            Int32 index;
            Int32 step;

            creditPhotos = new List<String>(Directory.GetFiles(CreditPhotosFolderTextBox.Text, "*.*"
                          , SearchOption.TopDirectoryOnly));
            creditPhotos = creditPhotos.ConvertAll((input) =>
                    {
                        FileInfo fi;

                        fi = new FileInfo(input);
                        return (fi.Name.ToUpper());
                    }
                );
            validCreditPhotos = new Dictionary<String, List<DVD>>(creditPhotos.Count);

            step = this.StartProgress(dvdList.Length);
            index = 0;
            foreach (DVD dvd in dvdList)
            {
                if ((dvd.CastList != null) && (dvd.CastList.Length > 0))
                {
                    foreach (Object potentialCastMember in dvd.CastList)
                    {
                        ProcessPerson(validCreditPhotos, creditPhotos, dvd, potentialCastMember);
                    }
                }
                if ((dvd.CrewList != null) && (dvd.CrewList.Length > 0))
                {
                    foreach (Object potentialCrewMember in dvd.CrewList)
                    {
                        ProcessPerson(validCreditPhotos, creditPhotos, dvd, potentialCrewMember);
                    }
                }

                this.UpdateProgressBar(index, step);
                index++;
            }

            this.EndProgress();

            creditPhotosListBoxItems = new List<ListBoxItem>(validCreditPhotos.Count);
            foreach (KeyValuePair<String, List<DVD>> kvp in validCreditPhotos)
            {
                kvp.Value.Sort(new Comparison<DVD>(delegate (DVD left, DVD right)
                        {
                            return (left.SortTitle.CompareTo(right.SortTitle));
                        }
                    ));
                creditPhotosListBoxItems.Add(new ListBoxItemWithDVDList(kvp.Key, kvp.Value));
            }
            creditPhotosListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            ValidCreditPhotosFileGeneralListBox.Items.AddRange(creditPhotosListBoxItems.ToArray());
            if (ValidCreditPhotosFileGeneralListBox.Items.Count > 0)
            {
                ValidCreditPhotosFileGeneralListBox.SelectedIndex = 0;
            }
            creditPhotosListBoxItems = new List<ListBoxItem>(creditPhotos.Count);
            foreach (String fileName in creditPhotos)
            {
                creditPhotosListBoxItems.Add(new ListBoxItem(fileName));
            }
            creditPhotosListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            InvalidCreditPhotosFileGeneralListBox.Items.AddRange(creditPhotosListBoxItems.ToArray());
            if (InvalidCreditPhotosFileGeneralListBox.Items.Count > 0)
            {
                InvalidCreditPhotosFileGeneralListBox.SelectedIndex = 0;
            }
        }

        private void UpdateProgressBar(Int32 index, Int32 step)
        {
            this.UpdateProgressBar();

            if ((index % step) == 0)
            {
                Application.DoEvents();
            }
        }

        private void EndProgress()
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
                TaskbarManager.Instance.OwnerHandle = IntPtr.Zero;
            }
            ProgressWindow.CanClose = true;
            ProgressWindow.Close();
            ProgressWindow.Dispose();
            ProgressWindow = null;
        }

        private Int32 StartProgress(Int32 count)
        {
            Int32 step;

            ProgressWindow = new ProgressWindow();
            ProgressWindow.ProgressBar.Minimum = 0;
            ProgressWindow.ProgressBar.Step = 1;
            ProgressWindow.CanClose = false;
            ProgressWindow.ProgressBar.Maximum = count;
            ProgressWindow.Show();
            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.OwnerHandle = this.Handle;
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                TaskbarManager.Instance.SetProgressValue(0, ProgressWindow.ProgressBar.Maximum);
            }

            step = GetStep(count);

            return (step);
        }

        private static Int32 GetStep(Int32 count)
        {
            Int32 step;

            step = count;

            if (count > 100)
            {
                step = count / 100;

                if ((count % 100) != 0)
                {
                    step++;
                }
            }

            return (step);
        }

        private static void ProcessPerson(Dictionary<String, List<DVD>> validCreditPhotos, List<String> creditPhotos, DVD dvd
          , Object potentialPerson)
        {
            IPerson person;

            person = potentialPerson as IPerson;
            if (person != null)
            {
                Boolean success;

                success = false;
                if (person.BirthYear != 0)
                {
                    String extendedfileName;

                    extendedfileName
                        = ProfilePhotoHelper.FileNameFromCreditName(person.FirstName, person.MiddleName, person.LastName, person.BirthYear);
                    success = ProcessEntry(validCreditPhotos, creditPhotos, dvd, extendedfileName);
                }
                if (success == false)
                {
                    String fileName;

                    fileName = ProfilePhotoHelper.FileNameFromCreditName(person.FirstName, person.MiddleName, person.LastName, 0);
                    ProcessEntry(validCreditPhotos, creditPhotos, dvd, fileName);
                }
            }
        }

        private static Boolean ProcessEntry(Dictionary<String, List<DVD>> validCreditPhotos, List<String> creditPhotos, DVD dvd
             , String fileName)
        {
            Int32 indexOf;
            List<DVD> dvdList;

            fileName = fileName.ToUpper() + ".JPG";
            indexOf = creditPhotos.IndexOf(fileName);
            if (indexOf != -1)
            {
                dvdList = new List<DVD>();
                dvdList.Add(dvd);
                validCreditPhotos.Add(fileName, dvdList);
                creditPhotos.RemoveAt(indexOf);
                return (true);
            }
            else
            {
                if (validCreditPhotos.TryGetValue(fileName, out dvdList))
                {
                    if (dvdList.Contains(dvd) == false)
                    {
                        dvdList.Add(dvd);
                    }
                    return (true);
                }
            }
            return (false);
        }
        #endregion

        #region Helper Functions
        private String GetDatabaseFolder()
        {
            IDVDInfo dvdInfo;
            String coverImage;
            FileInfo fi;

            dvdInfo = Api.GetDisplayedDVD();
            coverImage = dvdInfo.GetCoverImageFilename(true, false);
            if (String.IsNullOrEmpty(coverImage))
            {
                return (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"DVD Profiler\Databases\Default"));
            }
            else
            {
                fi = new FileInfo(coverImage);
                return (fi.Directory.Parent.FullName);
            }
        }

        private void ThreadRun(Object param)
        {
            Collection collection;
            String coverImagesPath;

            collection = null;
            coverImagesPath = String.Empty;
            try
            {
                Object[] allIds;
                List<DVD> dvdList;

                allIds = (Object[])(((Object[])param)[0]);
                coverImagesPath = (String)(((Object[])param)[1]);
                dvdList = new List<DVD>(allIds.Length);
                for (Int32 i = 0; i < allIds.Length; i++)
                {
                    DVD dvd;
                    String xml;

                    xml = (String)(this.Invoke(new GetProfileDataDelegate(this.GetProfileData), allIds[i]));
                    dvd = XmlSerializer<DVD>.FromString(xml, DVD.DefaultEncoding);
                    dvdList.Add(dvd);

                    this.Invoke(new ProgressBarDelegate(this.UpdateProgressBar));
                }
                collection = new Collection();
                collection.DVDList = dvdList.ToArray();
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => MessageBox.Show(ex.Message, MessageBoxTexts.ErrorHeader, MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
            finally
            {
                this.Invoke(new ThreadFinishedDelegate(this.ThreadFinished), collection, coverImagesPath);
            }
        }

        private void UpdateProgressBar()
        {
            ProgressWindow.ProgressBar.PerformStep();
            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressValue(ProgressWindow.ProgressBar.Value, ProgressWindow.ProgressBar.Maximum);
            }
        }

        private String GetProfileData(Object id)
        {
            IDVDInfo dvdInfo;
            String xml;

            Api.DVDByProfileID(out dvdInfo, (id).ToString(), -1, -1);
            xml = dvdInfo.GetXML(true);
            return (xml);
        }

        private void LayoutForm()
        {
            if (Plugin.Settings.MainForm.WindowState == FormWindowState.Normal)
            {
                this.Left = Plugin.Settings.MainForm.Left;
                this.Top = Plugin.Settings.MainForm.Top;
                if (Plugin.Settings.MainForm.Width > this.MinimumSize.Width)
                {
                    this.Width = Plugin.Settings.MainForm.Width;
                }
                else
                {
                    this.Width = this.MinimumSize.Width;
                }
                if (Plugin.Settings.MainForm.Height > this.MinimumSize.Height)
                {
                    this.Height = Plugin.Settings.MainForm.Height;
                }
                else
                {
                    this.Height = this.MinimumSize.Height;
                }
            }
            else
            {
                this.Left = Plugin.Settings.MainForm.RestoreBounds.X;
                this.Top = Plugin.Settings.MainForm.RestoreBounds.Y;
                if (Plugin.Settings.MainForm.RestoreBounds.Width > this.MinimumSize.Width)
                {
                    this.Width = Plugin.Settings.MainForm.RestoreBounds.Width;
                }
                else
                {
                    this.Width = this.MinimumSize.Width;
                }
                if (Plugin.Settings.MainForm.RestoreBounds.Height > this.MinimumSize.Height)
                {
                    this.Height = Plugin.Settings.MainForm.RestoreBounds.Height;
                }
                else
                {
                    this.Height = this.MinimumSize.Height;
                }
            }
            if (Plugin.Settings.MainForm.WindowState != FormWindowState.Minimized)
            {
                this.WindowState = Plugin.Settings.MainForm.WindowState;
            }
        }

        private void CheckForNewVersion()
        {
            OnlineAccess.Init("Doena Soft.", "Clean Up Photos");
            OnlineAccess.CheckForNewVersion("http://doena-soft.de/dvdprofiler/3.9.0/versions.xml", this, "CleanUpPhotos", this.GetType().Assembly);
        }

        private static Int32 CompareListBoxItems(ListBoxItem left, ListBoxItem right)
        {
            return (left.Name.CompareTo(right.Name));
        }

        private static Int32 CompareReverse(String left, String right)
        {
            return (right.CompareTo(left));
        }

        private void OpenReadMe()
        {
            String helpFile;

            helpFile = (new FileInfo(this.GetType().Assembly.Location)).DirectoryName + @"\Readme\readme.html";
            if (File.Exists(helpFile))
            {
                using (HelpForm helpForm = new HelpForm(helpFile))
                {
                    helpForm.Text = "Read Me";
                    helpForm.ShowDialog(this);
                }
            }
        }

        private static void ResetPictureBox(PictureBox box)
        {
            if (box.Image != null)
            {
                box.Image.Dispose();
                box.Image = null;
            }
        }

        private static Boolean HasPictureExtension(String file)
        {
            file = file.ToUpper();
            return ((file.EndsWith(".JPG")) || (file.EndsWith(".JPEG")) || (file.EndsWith(".GIF")) || (file.EndsWith(".PNG")) || (file.EndsWith(".BMP")));
        }

        private static String ConvertAllDirectory(String input)
        {
            DirectoryInfo di;

            di = new DirectoryInfo(input);
            return (di.Name.ToUpper());
        }

        private static String ConvertAllFile(String input
            , String path)
        {
            FileInfo fi;
            String file;

            fi = new FileInfo(input);
            file = fi.FullName.Replace(path + @"\", "");
            return (file.ToUpper());
        }
        #endregion

        #region Credit Photos
        #region General
        private void OnValidCreditPhotosFileGeneralListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = ValidCreditPhotosFileGeneralListBox.SelectedIndex;
            ValidCreditPhotosProfileGeneralListBox.ClearItems();
            ResetPictureBox(ValidCreditPhotosGeneralPictureBox);
            if (index != -1)
            {
                ListBoxItemWithDVDList item;

                item = (ListBoxItemWithDVDList)(ValidCreditPhotosFileGeneralListBox.SelectedItem);
                ValidCreditPhotosProfileGeneralListBox.Items.AddRange(item.DVDList.ToArray());
                try
                {
                    String file;

                    file = Path.Combine(CreditPhotosFolderTextBox.Text, item.Name);
                    if (HasPictureExtension(file))
                    {
                        ValidCreditPhotosGeneralPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    ValidCreditPhotosGeneralPictureBox.Image = (Image)(ValidCreditPhotosGeneralPictureBox.ErrorImage.Clone());
                }
                CopyToSpecificProfileValidGeneralButton.Enabled = true;
            }
            else
            {
                CopyToSpecificProfileValidGeneralButton.Enabled = false;
            }
        }

        private void OnInvalidCreditPhotosFileGeneralListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = InvalidCreditPhotosFileGeneralListBox.SelectedIndex;
            ResetPictureBox(InvalidCreditPhotosGeneralPictureBox);
            if (index != -1)
            {
                ListBoxItem item;

                item = (ListBoxItem)(InvalidCreditPhotosFileGeneralListBox.SelectedItem);
                try
                {
                    String file;

                    file = Path.Combine(CreditPhotosFolderTextBox.Text, item.Name);
                    if (HasPictureExtension(file))
                    {
                        InvalidCreditPhotosGeneralPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    InvalidCreditPhotosGeneralPictureBox.Image = (Image)(InvalidCreditPhotosGeneralPictureBox.ErrorImage.Clone());
                }
                RemoveInvalidCreditPhotosGeneralButton.Enabled = true;
            }
            else
            {
                RemoveInvalidCreditPhotosGeneralButton.Enabled = false;
            }
            if (InvalidCreditPhotosFileGeneralListBox.Items.Count > 0)
            {
                RemoveAllInvalidCreditPhotosGeneralButton.Enabled = true;
                CopyToSpecificProfileInvalidGeneralButton.Enabled = true;
            }
            else
            {
                RemoveAllInvalidCreditPhotosGeneralButton.Enabled = false;
                CopyToSpecificProfileInvalidGeneralButton.Enabled = false;
            }
        }

        private void OnRemoveInvalidCreditPhotosGeneralButtonClick(Object sender, EventArgs e)
        {
            if (InvalidCreditPhotosFileGeneralListBox.SelectedIndex != -1)
            {
                Int32 index;

                index = InvalidCreditPhotosFileGeneralListBox.SelectedIndex;
                if (MessageBox.Show(String.Format("Remove file?", InvalidCreditPhotosFileGeneralListBox.Items.Count), "Remove"
                    , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        String file;
                        ListBoxItem item;

                        item = (ListBoxItem)(InvalidCreditPhotosFileGeneralListBox.SelectedItem);
                        file = Path.Combine(CreditPhotosFolderTextBox.Text, item.Name);
                        ResetPictureBox(InvalidCreditPhotosGeneralPictureBox);
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    InvalidCreditPhotosFileGeneralListBox.Items.RemoveAt(index);
                    if (InvalidCreditPhotosFileGeneralListBox.Items.Count > 0)
                    {
                        if (index == 0)
                        {
                            InvalidCreditPhotosFileGeneralListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            InvalidCreditPhotosFileGeneralListBox.SelectedIndex = index - 1;
                        }
                    }
                    else
                    {
                        InvalidCreditPhotosFileGeneralListBox.SelectedIndex = -1;
                    }
                }
            }
            if (InvalidCreditPhotosFileGeneralListBox.SelectedIndex != -1)
            {
                RemoveInvalidCreditPhotosGeneralButton.Enabled = true;
            }
            else
            {
                RemoveInvalidCreditPhotosGeneralButton.Enabled = false;
            }
        }

        private void OnRemoveAllInvalidCreditPhotosGeneralButtonClick(Object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Remove {0} files?", InvalidCreditPhotosFileGeneralListBox.Items.Count), "Remove"
                , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                List<ListBoxItem> list;

                ResetPictureBox(InvalidCreditPhotosGeneralPictureBox);
                list = new List<ListBoxItem>(InvalidCreditPhotosFileGeneralListBox.Items.Count);
                foreach (ListBoxItem item in InvalidCreditPhotosFileGeneralListBox.Items)
                {
                    list.Add(item);
                }
                try
                {
                    for (Int32 i = list.Count - 1; i >= 0; i--)
                    {
                        String file;

                        file = Path.Combine(CreditPhotosFolderTextBox.Text, list[i].Name);
                        File.Delete(file);
                        list.RemoveAt(i);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    InvalidCreditPhotosFileGeneralListBox.ClearItems();
                    InvalidCreditPhotosFileGeneralListBox.Items.AddRange(list.ToArray());
                    if (InvalidCreditPhotosFileGeneralListBox.Items.Count > 0)
                    {
                        InvalidCreditPhotosFileGeneralListBox.SelectedIndex = 0;
                    }
                    else
                    {
                        InvalidCreditPhotosFileGeneralListBox.SelectedIndex = -1;
                    }
                    return;
                }
                InvalidCreditPhotosFileGeneralListBox.ClearItems();
                this.FireSelectionChanged();
            }
        }

        private void OnCopyToSpecificProfileInvalidGeneralButtonClick(Object sender, EventArgs e)
        {
            this.CopyImageGeneral(InvalidCreditPhotosFileGeneralListBox);
        }

        private void OnCopyToSpecificProfileValidGeneralButtonClick(Object sender, EventArgs e)
        {
            this.CopyImageGeneral(ValidCreditPhotosFileGeneralListBox);
        }
        #endregion

        #region Folder
        private void OnInvalidCreditPhotosFolderFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = InvalidCreditPhotosFolderFolderListBox.SelectedIndex;
            InvalidCreditPhotosFileFolderListBox.ClearItems();
            ResetPictureBox(InvalidCreditPhotosFolderPictureBox);
            if (index != -1)
            {
                try
                {
                    List<String> files;
                    String path;

                    path = Path.Combine(CreditPhotosFolderTextBox.Text
                        , InvalidCreditPhotosFolderFolderListBox.SelectedItem.ToString());
                    files = new List<String>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort();
                    InvalidCreditPhotosFileFolderListBox.Items.AddRange(files.ToArray());
                    if (InvalidCreditPhotosFileFolderListBox.Items.Count > 0)
                    {
                        InvalidCreditPhotosFileFolderListBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                RemoveInvalidCreditPhotosFolderButton.Enabled = true;
            }
            else
            {
                RemoveInvalidCreditPhotosFolderButton.Enabled = false;
            }
            if (InvalidCreditPhotosFolderFolderListBox.Items.Count > 0)
            {
                RemoveAllInvalidCreditPhotosFolderButton.Enabled = true;
            }
            else
            {
                RemoveAllInvalidCreditPhotosFolderButton.Enabled = false;
            }
        }

        private void OnInvalidCreditPhotosFileFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = InvalidCreditPhotosFileFolderListBox.SelectedIndex;
            ResetPictureBox(InvalidCreditPhotosFolderPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(InvalidCreditPhotosFileFolderListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(CreditPhotosFolderTextBox.Text
                        , InvalidCreditPhotosFolderFolderListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        InvalidCreditPhotosFolderPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    InvalidCreditPhotosFolderPictureBox.Image = (Image)(InvalidCreditPhotosFolderPictureBox.ErrorImage.Clone());
                }
                CopyToSpecificProfileInvalidFolderButton.Enabled = true;
            }
            else
            {
                CopyToSpecificProfileInvalidFolderButton.Enabled = false;
            }
        }

        private void OnValidCreditPhotosFolderFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = ValidCreditPhotosFolderFolderListBox.SelectedIndex;
            ValidCreditPhotosProfileFolderTextBox.Text = String.Empty;
            ValidCreditPhotosFileFolderListBox.ClearItems();
            ResetPictureBox(ValidCreditPhotosFolderPictureBox);
            if (index != -1)
            {
                try
                {
                    List<String> files;
                    ListBoxItemWithDVD listBoxItem;
                    String path;

                    listBoxItem = (ListBoxItemWithDVD)(ValidCreditPhotosFolderFolderListBox.SelectedItem);
                    ValidCreditPhotosProfileFolderTextBox.Text = listBoxItem.DVD.ToString();
                    path = Path.Combine(CreditPhotosFolderTextBox.Text, listBoxItem.Name);
                    files = new List<String>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort();
                    ValidCreditPhotosFileFolderListBox.Items.AddRange(files.ToArray());
                    if (ValidCreditPhotosFileFolderListBox.Items.Count > 0)
                    {
                        ValidCreditPhotosFileFolderListBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnValidCreditPhotosFileFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = ValidCreditPhotosFileFolderListBox.SelectedIndex;
            ResetPictureBox(ValidCreditPhotosFolderPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(ValidCreditPhotosFileFolderListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(CreditPhotosFolderTextBox.Text
                        , ValidCreditPhotosFolderFolderListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        ValidCreditPhotosFolderPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    ValidCreditPhotosFolderPictureBox.Image = (Image)(ValidCreditPhotosFolderPictureBox.ErrorImage.Clone());
                }
                CopyToSpecificProfileValidFolderButton.Enabled = true;
            }
            else
            {
                CopyToSpecificProfileValidFolderButton.Enabled = false;
            }
        }

        private void OnRemoveInvalidCreditPhotosFolderButtonClick(Object sender, EventArgs e)
        {
            if (InvalidCreditPhotosFolderFolderListBox.SelectedIndex != -1)
            {
                Int32 index;

                index = InvalidCreditPhotosFolderFolderListBox.SelectedIndex;
                if (MessageBox.Show(String.Format("Remove folder '{0}' and {1} files in it?", InvalidCreditPhotosFolderFolderListBox.SelectedItem
                    , InvalidCreditPhotosFileFolderListBox.Items.Count), "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        String path;
                        ListBoxItem item;
                        String[] files;

                        ResetPictureBox(InvalidCreditPhotosFolderPictureBox);
                        item = (ListBoxItem)(InvalidCreditPhotosFolderFolderListBox.SelectedItem);
                        path = Path.Combine(CreditPhotosFolderTextBox.Text, item.Name);
                        files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                        foreach (String file in files)
                        {
                            File.Delete(file);
                        }
                        files = Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories);
                        foreach (String file in files)
                        {
                            Directory.Delete(file);
                        }
                        Directory.Delete(path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.FireSelectionChanged();
                        return;
                    }
                    InvalidCreditPhotosFolderFolderListBox.Items.RemoveAt(index);
                    if (InvalidCreditPhotosFolderFolderListBox.Items.Count > 0)
                    {
                        if (index == 0)
                        {
                            InvalidCreditPhotosFolderFolderListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            InvalidCreditPhotosFolderFolderListBox.SelectedIndex = index - 1;
                        }
                    }
                    else
                    {
                        InvalidCreditPhotosFolderFolderListBox.SelectedIndex = -1;
                    }
                }
            }
            if (InvalidCreditPhotosFolderFolderListBox.SelectedIndex != -1)
            {
                RemoveInvalidCreditPhotosFolderButton.Enabled = true;
            }
            else
            {
                RemoveInvalidCreditPhotosFolderButton.Enabled = false;
            }
        }

        private void OnRemoveAllInvalidCreditPhotosFolderButtonClick(Object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Remove {0} folders and the files in them?", InvalidCreditPhotosFolderFolderListBox.Items.Count)
                , "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                List<ListBoxItem> list;

                ResetPictureBox(InvalidCreditPhotosFolderPictureBox);
                list = new List<ListBoxItem>(InvalidCreditPhotosFolderFolderListBox.Items.Count);
                foreach (ListBoxItem item in InvalidCreditPhotosFolderFolderListBox.Items)
                {
                    list.Add(item);
                }
                try
                {
                    for (Int32 i = list.Count - 1; i >= 0; i--)
                    {
                        String path;
                        String[] files;

                        path = Path.Combine(CreditPhotosFolderTextBox.Text, list[i].Name);
                        files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                        foreach (String file in files)
                        {
                            File.Delete(file);
                        }
                        Directory.Delete(path, true);
                        list.RemoveAt(i);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    InvalidCreditPhotosFolderFolderListBox.ClearItems();
                    InvalidCreditPhotosFolderFolderListBox.Items.AddRange(list.ToArray());
                    if (InvalidCreditPhotosFolderFolderListBox.Items.Count > 0)
                    {
                        InvalidCreditPhotosFolderFolderListBox.SelectedIndex = 0;
                    }
                    else
                    {
                        InvalidCreditPhotosFolderFolderListBox.SelectedIndex = -1;
                    }
                    return;
                }
                InvalidCreditPhotosFolderFolderListBox.ClearItems();
                this.FireSelectionChanged();
            }
        }

        private void OnCopyToSpecificProfileInvalidFolderButtonClick(Object sender, EventArgs e)
        {
            this.CopyImageProfile(InvalidCreditPhotosFileFolderListBox, InvalidCreditPhotosFolderFolderListBox);
        }

        private void OnCopyToSpecificProfileValidFolderButtonClick(Object sender, EventArgs e)
        {
            this.CopyImageProfile(ValidCreditPhotosFileFolderListBox, ValidCreditPhotosFolderFolderListBox);
        }
        #endregion

        #region Profile
        private void OnValidCreditPhotosFolderProfileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = ValidCreditPhotosFolderProfileListBox.SelectedIndex;
            ValidCreditPhotosProfileProfileTextBox.Text = String.Empty;
            ValidCreditPhotosFileProfileListBox.ClearItems();
            ResetPictureBox(ValidCreditPhotosProfilePictureBox);
            if (index != -1)
            {
                ListBoxItemWithNames listBoxItem;

                listBoxItem = (ListBoxItemWithNames)(ValidCreditPhotosFolderProfileListBox.SelectedItem);
                ValidCreditPhotosProfileProfileTextBox.Text = listBoxItem.DVD.ToString();
                ValidCreditPhotosFileProfileListBox.Items.AddRange(listBoxItem.ValidList.ToArray());
                if (ValidCreditPhotosFileProfileListBox.Items.Count > 0)
                {
                    ValidCreditPhotosFileProfileListBox.SelectedIndex = 0;
                }
            }
        }

        private void OnValidCreditPhotosFileProfileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = ValidCreditPhotosFileProfileListBox.SelectedIndex;
            ResetPictureBox(ValidCreditPhotosProfilePictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(ValidCreditPhotosFileProfileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(CreditPhotosFolderTextBox.Text
                        , ValidCreditPhotosFolderProfileListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        ValidCreditPhotosProfilePictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    ValidCreditPhotosProfilePictureBox.Image = (Image)(ValidCreditPhotosProfilePictureBox.ErrorImage.Clone());
                }
                CopyToSpecificProfileValidProfileButton.Enabled = true;
            }
            else
            {
                CopyToSpecificProfileValidProfileButton.Enabled = false;
            }
        }

        private void OnInvalidCreditPhotosFolderProfileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = InvalidCreditPhotosFolderProfileListBox.SelectedIndex;
            InvalidCreditPhotosInvalidFileProfileListBox.ClearItems();
            ResetPictureBox(InvalidCreditPhotosInvalidFileProfilePictureBox);
            InvalidCreditPhotosValidFileProfileListBox.ClearItems();
            ResetPictureBox(InvalidCreditPhotosValidFileProfilePictureBox);
            if (index != -1)
            {
                ListBoxItemWithNames listBoxItem;

                listBoxItem = (ListBoxItemWithNames)(InvalidCreditPhotosFolderProfileListBox.SelectedItem);
                InvalidCreditPhotosProfileProfileTextBox.Text = listBoxItem.DVD.ToString();
                InvalidCreditPhotosInvalidFileProfileListBox.Items.AddRange(listBoxItem.InvalidList.ToArray());
                if (InvalidCreditPhotosInvalidFileProfileListBox.Items.Count > 0)
                {
                    InvalidCreditPhotosInvalidFileProfileListBox.SelectedIndex = 0;
                }
                InvalidCreditPhotosValidFileProfileListBox.Items.AddRange(listBoxItem.ValidList.ToArray());
                if (InvalidCreditPhotosValidFileProfileListBox.Items.Count > 0)
                {
                    InvalidCreditPhotosValidFileProfileListBox.SelectedIndex = 0;
                }
                RemoveInvalidCreditPhotosProfileButton.Enabled = true;
            }
            else
            {
                InvalidCreditPhotosProfileProfileTextBox.Text = String.Empty;
                RemoveInvalidCreditPhotosProfileButton.Enabled = false;
            }
            if (InvalidCreditPhotosFolderProfileListBox.Items.Count > 0)
            {
                RemoveAllInvalidCreditPhotosProfileButton.Enabled = true;
            }
            else
            {
                RemoveAllInvalidCreditPhotosProfileButton.Enabled = false;
            }
        }

        private void OnInvalidCreditPhotosValidFileProfileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = InvalidCreditPhotosValidFileProfileListBox.SelectedIndex;
            ResetPictureBox(InvalidCreditPhotosValidFileProfilePictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(InvalidCreditPhotosValidFileProfileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(CreditPhotosFolderTextBox.Text
                        , InvalidCreditPhotosFolderProfileListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        InvalidCreditPhotosValidFileProfilePictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    InvalidCreditPhotosValidFileProfilePictureBox.Image
                        = (Image)(InvalidCreditPhotosValidFileProfilePictureBox.ErrorImage.Clone());
                }
                CopyToSpecificProfileInvalidProfileValidPhotoButton.Enabled = true;
            }
            else
            {
                CopyToSpecificProfileInvalidProfileValidPhotoButton.Enabled = false;
            }
        }

        private void OnInvalidCreditPhotosInvalidFileProfileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = InvalidCreditPhotosInvalidFileProfileListBox.SelectedIndex;
            ResetPictureBox(InvalidCreditPhotosInvalidFileProfilePictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(InvalidCreditPhotosInvalidFileProfileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(CreditPhotosFolderTextBox.Text
                        , InvalidCreditPhotosFolderProfileListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        InvalidCreditPhotosInvalidFileProfilePictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    InvalidCreditPhotosInvalidFileProfilePictureBox.Image
                        = (Image)(InvalidCreditPhotosInvalidFileProfilePictureBox.ErrorImage.Clone());
                }
                CopyToSpecificProfileInvalidProfileInvalidPhotoButton.Enabled = true;
            }
            else
            {
                CopyToSpecificProfileInvalidProfileInvalidPhotoButton.Enabled = false;
            }
        }

        private void OnRemoveInvalidCreditPhotosProfileButtonClick(Object sender, EventArgs e)
        {
            if (InvalidCreditPhotosFolderProfileListBox.SelectedIndex != -1)
            {
                Int32 index;

                index = InvalidCreditPhotosFolderProfileListBox.SelectedIndex;
                if (MessageBox.Show(String.Format("Remove {0} files?", InvalidCreditPhotosInvalidFileProfileListBox.Items.Count)
                    , "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    ListBoxItemWithNames item;
                    String path;

                    ResetPictureBox(InvalidCreditPhotosInvalidFileProfilePictureBox);
                    ResetPictureBox(ValidCreditPhotosFolderPictureBox);
                    item = (ListBoxItemWithNames)(InvalidCreditPhotosFolderProfileListBox.SelectedItem);
                    path = Path.Combine(CreditPhotosFolderTextBox.Text, item.Name);
                    if (this.RemoveInvalidCreditPhotosOnValidFolder(item, path, false) == false)
                    {
                        return;
                    }
                    InvalidCreditPhotosFolderProfileListBox.Items.RemoveAt(index);
                    if (InvalidCreditPhotosFolderProfileListBox.Items.Count > 0)
                    {
                        if (index == 0)
                        {
                            InvalidCreditPhotosFolderProfileListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            InvalidCreditPhotosFolderProfileListBox.SelectedIndex = index - 1;
                        }
                    }
                    else
                    {
                        InvalidCreditPhotosFolderProfileListBox.SelectedIndex = -1;
                    }
                }
            }
            if (InvalidCreditPhotosFolderProfileListBox.SelectedIndex != -1)
            {
                RemoveInvalidCreditPhotosProfileButton.Enabled = true;
            }
            else
            {
                RemoveInvalidCreditPhotosProfileButton.Enabled = false;
            }
        }

        private Boolean RemoveInvalidCreditPhotosOnValidFolder(ListBoxItemWithNames item, String path, Boolean batchMode)
        {
            List<String> invalidFiles;

            invalidFiles = new List<String>(item.InvalidList);
            try
            {
                for (Int32 i = invalidFiles.Count - 1; i >= 0; i--)
                {
                    String file;

                    file = Path.Combine(path, invalidFiles[i]);
                    File.Delete(file);
                    invalidFiles.RemoveAt(i);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                item.InvalidList = invalidFiles;
                if (batchMode == false)
                {
                    this.FireSelectionChanged();
                }
                return (false);
            }
            finally
            {
                if (batchMode == false)
                {
                    this.FireSelectionChanged();
                }
            }
            return (true);
        }

        private void OnRemoveAllInvalidCreditPhotosProfileButtonClick(Object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Remove files from {0} folders?", InvalidCreditPhotosFolderProfileListBox.Items.Count)
               , "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                List<ListBoxItemWithNames> list;

                ResetPictureBox(InvalidCreditPhotosInvalidFileProfilePictureBox);
                ResetPictureBox(ValidCreditPhotosFolderPictureBox);
                list = new List<ListBoxItemWithNames>(InvalidCreditPhotosFolderProfileListBox.Items.Count);
                foreach (ListBoxItemWithNames item in InvalidCreditPhotosFolderProfileListBox.Items)
                {
                    list.Add(item);
                }
                for (Int32 i = list.Count - 1; i >= 0; i--)
                {
                    String path;

                    path = Path.Combine(CreditPhotosFolderTextBox.Text, list[i].Name);
                    if (this.RemoveInvalidCreditPhotosOnValidFolder(list[i], path, true))
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        InvalidCreditPhotosFolderProfileListBox.ClearItems();
                        InvalidCreditPhotosFolderProfileListBox.Items.AddRange(list.ToArray());
                        if (InvalidCreditPhotosFolderProfileListBox.Items.Count > 0)
                        {
                            InvalidCreditPhotosFolderProfileListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            InvalidCreditPhotosFolderProfileListBox.SelectedIndex = -1;
                        }
                        this.FireSelectionChanged();
                        return;
                    }
                }
                InvalidCreditPhotosFolderProfileListBox.ClearItems();
                this.FireSelectionChanged();
            }
        }

        private void FireSelectionChanged()
        {
            Object sender;
            EventArgs e;

            sender = this;
            e = EventArgs.Empty;

            this.OnInvalidCoverImagesFileListBoxSelectedIndexChanged(sender, e);
            this.OnInvalidCoverImagesFolderListBoxSelectedIndexChanged(sender, e);
            this.OnInvalidCreditPhotosFileFolderListBoxSelectedIndexChanged(sender, e);
            this.OnInvalidCreditPhotosFileGeneralListBoxSelectedIndexChanged(sender, e);
            this.OnInvalidCreditPhotosFolderFolderListBoxSelectedIndexChanged(sender, e);
            this.OnInvalidCreditPhotosFolderProfileListBoxSelectedIndexChanged(sender, e);
            this.OnInvalidCreditPhotosInvalidFileProfileListBoxSelectedIndexChanged(sender, e);
            this.OnInvalidCreditPhotosValidFileProfileListBoxSelectedIndexChanged(sender, e);
            this.OnInvalidScenePhotosFileListBoxSelectedIndexChanged(sender, e);
            this.OnInvalidScenePhotosFolderListBoxSelectedIndexChanged(sender, e);

            this.OnValidCoverImagesFileListBoxSelectedIndexChanged(sender, e);
            this.OnValidCoverImagesFolderListBoxSelectedIndexChanged(sender, e);
            this.OnValidCreditPhotosFileFolderListBoxSelectedIndexChanged(sender, e);
            this.OnValidCreditPhotosFileGeneralListBoxSelectedIndexChanged(sender, e);
            this.OnValidCreditPhotosFileProfileListBoxSelectedIndexChanged(sender, e);
            this.OnValidCreditPhotosFolderFolderListBoxSelectedIndexChanged(sender, e);
            this.OnValidCreditPhotosFolderProfileListBoxSelectedIndexChanged(sender, e);
            this.OnValidScenePhotosFileListBoxSelectedIndexChanged(sender, e);
            this.OnValidScenePhotosFolderListBoxSelectedIndexChanged(sender, e);
        }

        private void OnCopyToSpecificProfileInvalidProfileInvalidPhotoButtonClick(Object sender, EventArgs e)
        {
            this.CopyImageProfile(InvalidCreditPhotosInvalidFileProfileListBox, InvalidCreditPhotosFolderProfileListBox);
        }

        private void OnCopyToSpecificProfileInvalidProfileValidPhotoButtonClick(Object sender, EventArgs e)
        {
            this.CopyImageProfile(InvalidCreditPhotosValidFileProfileListBox, InvalidCreditPhotosFolderProfileListBox);
        }

        private void OnCopyToSpecificProfileValidProfileButtonClick(Object sender, EventArgs e)
        {
            this.CopyImageProfile(ValidCreditPhotosFileProfileListBox, ValidCreditPhotosFolderProfileListBox);
        }
        #endregion
        #endregion

        #region Scene Photos
        private void OnInvalidScenePhotosFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = InvalidScenePhotosFolderListBox.SelectedIndex;
            InvalidScenePhotosFileListBox.ClearItems();
            ResetPictureBox(InvalidScenePhotosPictureBox);
            if (index != -1)
            {
                try
                {
                    List<String> files;
                    String path;

                    path = Path.Combine(ScenePhotosFolderTextbox.Text
                        , InvalidScenePhotosFolderListBox.SelectedItem.ToString());
                    files = new List<String>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));

                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort();
                    InvalidScenePhotosFileListBox.Items.AddRange(files.ToArray());
                    if (InvalidScenePhotosFileListBox.Items.Count > 0)
                    {
                        InvalidScenePhotosFileListBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                RemoveInvalidScenePhotosButton.Enabled = true;
            }
            else
            {
                RemoveInvalidScenePhotosButton.Enabled = false;
            }
            if (InvalidScenePhotosFolderListBox.Items.Count > 0)
            {
                RemoveAllInvalidScenePhotosButton.Enabled = true;
            }
            else
            {
                RemoveAllInvalidScenePhotosButton.Enabled = false;
            }
        }

        private void OnInvalidScenePhotosFileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = InvalidScenePhotosFileListBox.SelectedIndex;
            ResetPictureBox(InvalidScenePhotosPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(InvalidScenePhotosFileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(ScenePhotosFolderTextbox.Text
                        , InvalidScenePhotosFolderListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        InvalidScenePhotosPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    InvalidScenePhotosPictureBox.Image = (Image)(InvalidScenePhotosPictureBox.ErrorImage.Clone());
                }
            }
        }

        private void OnValidScenePhotosFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = ValidScenePhotosFolderListBox.SelectedIndex;
            ValidScenePhotosProfileTextBox.Text = String.Empty;
            ValidScenePhotosFileListBox.ClearItems();
            ResetPictureBox(ValidScenePhotosPictureBox);
            if (index != -1)
            {
                try
                {
                    List<String> files;
                    ListBoxItemWithDVD listBoxItem;
                    String path;

                    listBoxItem = (ListBoxItemWithDVD)(ValidScenePhotosFolderListBox.SelectedItem);
                    ValidScenePhotosProfileTextBox.Text = listBoxItem.DVD.ToString();
                    path = Path.Combine(ScenePhotosFolderTextbox.Text, listBoxItem.Name);
                    files = new List<String>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort();
                    ValidScenePhotosFileListBox.Items.AddRange(files.ToArray());
                    if (ValidScenePhotosFileListBox.Items.Count > 0)
                    {
                        ValidScenePhotosFileListBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnValidScenePhotosFileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = ValidScenePhotosFileListBox.SelectedIndex;
            ResetPictureBox(ValidScenePhotosPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(ValidScenePhotosFileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(ScenePhotosFolderTextbox.Text
                        , ValidScenePhotosFolderListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        ValidScenePhotosPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    ValidScenePhotosPictureBox.Image = (Image)(ValidScenePhotosPictureBox.ErrorImage.Clone());
                }
            }
        }

        private void OnRemoveInvalidScenePhotosButtonClick(Object sender, EventArgs e)
        {
            if (InvalidScenePhotosFolderListBox.SelectedIndex != -1)
            {
                Int32 index;

                index = InvalidScenePhotosFolderListBox.SelectedIndex;
                if (MessageBox.Show(String.Format("Remove folder '{0}' and {1} files in it?", InvalidScenePhotosFolderListBox.SelectedItem
                    , InvalidScenePhotosFileListBox.Items.Count), "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        String path;
                        ListBoxItem item;
                        String[] files;

                        ResetPictureBox(InvalidScenePhotosPictureBox);
                        item = (ListBoxItem)(InvalidScenePhotosFolderListBox.SelectedItem);
                        path = Path.Combine(ScenePhotosFolderTextbox.Text, item.Name);
                        files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                        foreach (String file in files)
                        {
                            File.Delete(file);
                        }
                        files = Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories);
                        foreach (String file in files)
                        {
                            Directory.Delete(file);
                        }
                        Directory.Delete(path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.FireSelectionChanged();
                        return;
                    }
                    InvalidScenePhotosFolderListBox.Items.RemoveAt(index);
                    if (InvalidScenePhotosFolderListBox.Items.Count > 0)
                    {
                        if (index == 0)
                        {
                            InvalidScenePhotosFolderListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            InvalidScenePhotosFolderListBox.SelectedIndex = index - 1;
                        }
                    }
                    else
                    {
                        InvalidScenePhotosFolderListBox.SelectedIndex = -1;
                    }
                }
            }
            if (InvalidScenePhotosFolderListBox.SelectedIndex != -1)
            {
                RemoveInvalidScenePhotosButton.Enabled = true;
            }
            else
            {
                RemoveInvalidScenePhotosButton.Enabled = false;
            }
        }

        private void OnRemoveAllInvalidScenePhotosButtonClick(Object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Remove {0} folders and the files in them?", InvalidScenePhotosFolderListBox.Items.Count), "Remove"
                    , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                List<ListBoxItem> list;

                ResetPictureBox(InvalidScenePhotosPictureBox);
                list = new List<ListBoxItem>(InvalidScenePhotosFolderListBox.Items.Count);
                foreach (ListBoxItem item in InvalidScenePhotosFolderListBox.Items)
                {
                    list.Add(item);
                }
                try
                {
                    for (Int32 i = list.Count - 1; i >= 0; i--)
                    {
                        String path;
                        String[] files;

                        path = Path.Combine(ScenePhotosFolderTextbox.Text, list[i].Name);
                        files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                        foreach (String file in files)
                        {
                            File.Delete(file);
                        }
                        files = Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories);
                        foreach (String file in files)
                        {
                            Directory.Delete(file);
                        }
                        Directory.Delete(path);
                        list.RemoveAt(i);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    InvalidScenePhotosFolderListBox.ClearItems();
                    InvalidScenePhotosFolderListBox.Items.AddRange(list.ToArray());
                    if (InvalidScenePhotosFolderListBox.Items.Count > 0)
                    {
                        InvalidScenePhotosFolderListBox.SelectedIndex = 0;
                    }
                    else
                    {
                        InvalidScenePhotosFolderListBox.SelectedIndex = -1;
                    }
                    return;
                }
                InvalidScenePhotosFolderListBox.ClearItems();
                this.FireSelectionChanged();
            }
        }
        #endregion

        #region Cover Images
        private void OnCleanCoverImagesCheckBoxCheckedChanged(Object sender, EventArgs e)
        {
            DatabaseFolderTextbox.Enabled = CleanCoverImagesCheckBox.Checked;
            SelectDatabaseFolderButton.Enabled = CleanCoverImagesCheckBox.Checked;
            CoverImagesTabControl.Enabled = CleanCoverImagesCheckBox.Checked;
            Plugin.Settings.DefaultValues.CleanUpCoverImages = CleanCoverImagesCheckBox.Checked;
        }

        private void OnSelectDatabaseFolderButtonClick(Object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                Boolean ok;

                fbd.Description = "Please select your Database folder";
                fbd.SelectedPath = DatabaseFolderTextbox.Text;
                fbd.ShowNewFolderButton = false;
                do
                {
                    ok = true;
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        if (Directory.GetFiles(fbd.SelectedPath, "collection.dat").Length == 0)
                        {
                            MessageBox.Show("Selected folder is not a database folder!", "Error", MessageBoxButtons.OK
                                , MessageBoxIcon.Warning);
                            ok = false;
                        }
                        else
                        {
                            DatabaseFolderTextbox.Text = fbd.SelectedPath;
                        }
                    }
                } while (ok == false);
            }
        }

        private void OnValidCoverImagesFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = ValidCoverImagesFolderListBox.SelectedIndex;
            ValidCoverImagesProfileTextBox.Text = String.Empty;
            ValidCoverImagesFileListBox.ClearItems();
            ResetPictureBox(ValidCoverImagesPictureBox);
            if (index != -1)
            {
                try
                {
                    String path;
                    List<String> files;
                    ListBoxItemWithDVD listBoxItem;
                    String filter;

                    listBoxItem = (ListBoxItemWithDVD)(ValidCoverImagesFolderListBox.SelectedItem);
                    ValidCoverImagesProfileTextBox.Text = listBoxItem.DVD.ToString();
                    path = Path.Combine(DatabaseFolderTextbox.Text, "Images");
                    if (Directory.Exists(path) == false)
                    {
                        MessageBox.Show("Cover Images folder file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    filter = ValidCoverImagesFolderListBox.SelectedItem.ToString();
                    files = new List<String>(Directory.GetFiles(path, filter + "*.*", SearchOption.AllDirectories));
                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort(CompareReverse);
                    ValidCoverImagesFileListBox.Items.AddRange(files.ToArray());
                    if (ValidCoverImagesFileListBox.Items.Count > 0)
                    {
                        ValidCoverImagesFileListBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnValidCoverImagesFileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = ValidCoverImagesFileListBox.SelectedIndex;
            ResetPictureBox(ValidCoverImagesPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(ValidCoverImagesFileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(DatabaseFolderTextbox.Text, "Images");
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        ValidCoverImagesPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    ValidCoverImagesPictureBox.Image = (Image)(ValidCoverImagesPictureBox.ErrorImage.Clone());
                }
            }
        }

        private void OnRemoveInvalidCoverImagesButtonClick(Object sender, EventArgs e)
        {
            if (InvalidCoverImagesFolderListBox.SelectedIndex != -1)
            {
                Int32 index;

                index = InvalidCoverImagesFolderListBox.SelectedIndex;
                if (MessageBox.Show(String.Format("Remove {0} files starting with '{1}'?"
                    , InvalidCoverImagesFileListBox.Items.Count, InvalidCoverImagesFolderListBox.SelectedItem)
                    , "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        String path;
                        ListBoxItem item;
                        List<String> files;

                        ResetPictureBox(InvalidCoverImagesPictureBox);
                        item = (ListBoxItem)(InvalidCoverImagesFolderListBox.SelectedItem);
                        path = Path.Combine(DatabaseFolderTextbox.Text, "Images");
                        files = new List<String>(Directory.GetFiles(path, item.Name + "f.jpg", SearchOption.AllDirectories));
                        files.AddRange(Directory.GetFiles(path, item.Name + "b.jpg", SearchOption.AllDirectories));
                        foreach (String file in files)
                        {
                            File.Delete(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.FireSelectionChanged();
                        return;
                    }
                    InvalidCoverImagesFolderListBox.Items.RemoveAt(index);
                    if (InvalidCoverImagesFolderListBox.Items.Count > 0)
                    {
                        if (index == 0)
                        {
                            InvalidCoverImagesFolderListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            InvalidCoverImagesFolderListBox.SelectedIndex = index - 1;
                        }
                    }
                    else
                    {
                        InvalidCoverImagesFolderListBox.SelectedIndex = -1;
                    }
                }
            }
            if (InvalidCoverImagesFolderListBox.SelectedIndex != -1)
            {
                RemoveInvalidCoverImagesButton.Enabled = true;
            }
            else
            {
                RemoveInvalidCoverImagesButton.Enabled = false;
            }
        }

        private void OnRemoveAllInvalidCoverImagesButtonClick(Object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Remove all files?", InvalidScenePhotosFolderListBox.Items.Count), "Remove"
                   , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                List<ListBoxItem> list;

                ResetPictureBox(InvalidCoverImagesPictureBox);
                list = new List<ListBoxItem>(InvalidCoverImagesFolderListBox.Items.Count);
                foreach (ListBoxItem item in InvalidCoverImagesFolderListBox.Items)
                {
                    list.Add(item);
                }
                try
                {
                    for (Int32 i = list.Count - 1; i >= 0; i--)
                    {
                        String path;
                        List<String> files;

                        path = Path.Combine(DatabaseFolderTextbox.Text, "Images");
                        files = new List<String>(Directory.GetFiles(path, list[i].Name + "f.jpg", SearchOption.AllDirectories));
                        files.AddRange(Directory.GetFiles(path, list[i].Name + "b.jpg", SearchOption.AllDirectories));
                        foreach (String file in files)
                        {
                            File.Delete(file);
                        }
                        list.RemoveAt(i);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    InvalidCoverImagesFolderListBox.ClearItems();
                    InvalidCoverImagesFolderListBox.Items.AddRange(list.ToArray());
                    if (InvalidCoverImagesFolderListBox.Items.Count > 0)
                    {
                        InvalidCoverImagesFolderListBox.SelectedIndex = 0;
                    }
                    else
                    {
                        InvalidCoverImagesFolderListBox.SelectedIndex = -1;
                    }
                    return;
                }
                InvalidCoverImagesFolderListBox.ClearItems();
                this.FireSelectionChanged();
            }
        }

        private void OnInvalidCoverImagesFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = InvalidCoverImagesFolderListBox.SelectedIndex;
            InvalidCoverImagesFileListBox.ClearItems();
            ResetPictureBox(InvalidCoverImagesPictureBox);
            if (index != -1)
            {
                try
                {
                    String path;
                    List<String> files;
                    String filter;

                    path = Path.Combine(DatabaseFolderTextbox.Text, "Images");
                    if (Directory.Exists(path) == false)
                    {
                        MessageBox.Show("Cover Images folder file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    filter = InvalidCoverImagesFolderListBox.SelectedItem.ToString();
                    files = new List<String>(Directory.GetFiles(path, filter + "b.jpg", SearchOption.AllDirectories));
                    files.AddRange(Directory.GetFiles(path, filter + "f.jpg", SearchOption.AllDirectories));
                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort(CompareReverse);
                    InvalidCoverImagesFileListBox.Items.AddRange(files.ToArray());
                    if (InvalidCoverImagesFileListBox.Items.Count > 0)
                    {
                        InvalidCoverImagesFileListBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                RemoveInvalidCoverImagesButton.Enabled = true;
            }
            else
            {
                RemoveInvalidCoverImagesButton.Enabled = false;
            }
            if (InvalidCoverImagesFolderListBox.Items.Count > 0)
            {
                RemoveAllInvalidCoverImagesButton.Enabled = true;
            }
            else
            {
                RemoveAllInvalidCoverImagesButton.Enabled = false;
            }
        }

        private void OnInvalidCoverImagesFileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = InvalidCoverImagesFileListBox.SelectedIndex;
            ResetPictureBox(InvalidCoverImagesPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(InvalidCoverImagesFileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(DatabaseFolderTextbox.Text, "Images");
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        InvalidCoverImagesPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    InvalidCoverImagesPictureBox.Image = (Image)(InvalidCoverImagesPictureBox.ErrorImage.Clone());
                }
            }
        }
        #endregion

        private void CopyImageProfile(ListBox profileListBox, ListBox imageListBox)
        {
            if (profileListBox.SelectedIndex != -1)
            {
                String file;
                String path;
                ListBoxItem profile;

                file = (String)(profileListBox.SelectedItem);
                profile = (ListBoxItem)(imageListBox.SelectedItem);
                path = Path.Combine(CreditPhotosFolderTextBox.Text, profile.Name);
                file = Path.Combine(path, file);
                using (CopyImageForm form = new CopyImageForm(CreditPhotosFolderTextBox.Text, file, profile.Name, Collection))
                {
                    form.ShowDialog();
                }
            }
        }

        private void CopyImageGeneral(ListBox listBox)
        {
            if (listBox.SelectedIndex != -1)
            {
                ListBoxItem item;
                String file;

                item = (ListBoxItem)(listBox.SelectedItem);
                file = Path.Combine(CreditPhotosFolderTextBox.Text, item.Name);
                using (CopyImageForm form = new CopyImageForm(CreditPhotosFolderTextBox.Text, file, "<General>", Collection))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}