using DoenaSoft.DVDProfiler.DVDProfilerHelper;
using DoenaSoft.DVDProfiler.DVDProfilerXML;
using DoenaSoft.DVDProfiler.DVDProfilerXML.Version390;
using Invelos.DVDProfilerPlugin;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

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
                this.Name = name;
            }

            public override String ToString()
            {
                return (this.Name);
            }
        }

        private class ListBoxItemWithDVD : ListBoxItem
        {
            internal DVD DVD;

            internal ListBoxItemWithDVD(String name, DVD dvd)
                : base(name)
            {
                this.DVD = dvd;
            }
        }

        private class ListBoxItemWithDVDList : ListBoxItem
        {
            internal List<DVD> DVDList;

            internal ListBoxItemWithDVDList(String name, List<DVD> dvdList)
                : base(name)
            {
                this.DVDList = dvdList;
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
                this.DVD = dvd;
                this.ValidList = validList;
                this.InvalidList = invalidList;
            }
        }
        #endregion

        #region Fields
        private IDVDProfilerAPI Api;

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
            this.Api = api;
            this.CanClose = true;
            InitializeComponent();
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
            this.CleanCoverImagesCheckBox.Checked = Plugin.Settings.DefaultValues.CleanUpCoverImages;
            this.SelectDatabaseFolderButton.Enabled = Plugin.Settings.DefaultValues.CleanUpCoverImages;
            this.CoverImagesTabControl.Enabled = this.CleanCoverImagesCheckBox.Checked;
            //this.DatabaseFolderTextbox.Text = Environment.ExpandEnvironmentVariables(Plugin.Settings.DefaultValues.DatabaseFolder);
            this.DatabaseFolderTextbox.Text = this.GetDatabaseFolder();
            this.CreditPhotosFolderTextBox.Text = Plugin.Settings.DefaultValues.CreditPhotosFolder;
            this.ScenePhotosFolderTextbox.Text = Plugin.Settings.DefaultValues.ScenePhotosFolder;
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
            this.ValidCreditPhotosFileGeneralListBox.ClearItems();
            this.ValidCreditPhotosProfileGeneralListBox.ClearItems();
            ResetPictureBox(this.ValidCreditPhotosGeneralPictureBox);
            this.InvalidCreditPhotosFileGeneralListBox.ClearItems();
            ResetPictureBox(this.InvalidCreditPhotosGeneralPictureBox);
            this.RemoveInvalidCreditPhotosGeneralButton.Enabled = false;
            this.RemoveAllInvalidCreditPhotosGeneralButton.Enabled = false;
            #endregion
            #region Folder
            this.ValidCreditPhotosFolderFolderListBox.ClearItems();
            this.ValidCreditPhotosProfileFolderTextBox.Text = String.Empty;
            this.ValidCreditPhotosFileFolderListBox.ClearItems();
            ResetPictureBox(this.ValidCreditPhotosFolderPictureBox);
            this.InvalidCreditPhotosFolderFolderListBox.ClearItems();
            this.InvalidCreditPhotosFileFolderListBox.ClearItems();
            ResetPictureBox(this.InvalidCreditPhotosFolderPictureBox);
            this.RemoveInvalidCreditPhotosFolderButton.Enabled = false;
            this.RemoveAllInvalidCreditPhotosFolderButton.Enabled = false;
            #endregion
            #region Profile
            this.ValidCreditPhotosFolderProfileListBox.ClearItems();
            this.ValidCreditPhotosProfileProfileTextBox.Text = String.Empty;
            this.ValidCreditPhotosFileProfileListBox.ClearItems();
            ResetPictureBox(this.ValidCreditPhotosProfilePictureBox);
            this.InvalidCreditPhotosFolderProfileListBox.ClearItems();
            this.InvalidCreditPhotosInvalidFileProfileListBox.ClearItems();
            ResetPictureBox(this.InvalidCreditPhotosInvalidFileProfilePictureBox);
            this.InvalidCreditPhotosValidFileProfileListBox.ClearItems();
            ResetPictureBox(this.InvalidCreditPhotosValidFileProfilePictureBox);
            this.RemoveInvalidCreditPhotosProfileButton.Enabled = false;
            this.RemoveAllInvalidCreditPhotosProfileButton.Enabled = false;
            #endregion
            #endregion
            #region Scene Photos
            this.ValidScenePhotosFolderListBox.ClearItems();
            this.ValidScenePhotosProfileTextBox.Text = String.Empty;
            this.ValidScenePhotosFileListBox.ClearItems();
            ResetPictureBox(this.ValidScenePhotosPictureBox);
            this.InvalidScenePhotosFolderListBox.ClearItems();
            this.InvalidScenePhotosFileListBox.ClearItems();
            ResetPictureBox(this.InvalidScenePhotosPictureBox);
            this.RemoveInvalidScenePhotosButton.Enabled = false;
            this.RemoveAllInvalidScenePhotosButton.Enabled = false;
            #endregion
            #region Cover Images
            this.ValidCoverImagesFolderListBox.ClearItems();
            this.ValidCoverImagesProfileTextBox.Text = String.Empty;
            this.ValidCoverImagesFileListBox.ClearItems();
            ResetPictureBox(this.ValidCoverImagesPictureBox);
            this.InvalidCoverImagesFolderListBox.ClearItems();
            this.InvalidCoverImagesFileListBox.ClearItems();
            ResetPictureBox(this.InvalidCoverImagesPictureBox);
            this.RemoveInvalidCoverImagesButton.Enabled = false;
            this.RemoveAllInvalidCoverImagesButton.Enabled = false;
            #endregion
            #endregion
            if (Directory.Exists(this.CreditPhotosFolderTextBox.Text) == false)
            {
                MessageBox.Show("Credit Photos folder not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (Directory.Exists(this.ScenePhotosFolderTextbox.Text) == false)
            {
                MessageBox.Show("Scene Photos folder not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            coverImagesPath = String.Empty;
            if (this.CleanCoverImagesCheckBox.Checked)
            {
                coverImagesPath = Path.Combine(this.DatabaseFolderTextbox.Text, "Images");
                if (Directory.Exists(coverImagesPath) == false)
                {
                    MessageBox.Show("Cover Images folder file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            this.CanClose = false;
            this.UseWaitCursor = true;
            this.Cursor = Cursors.WaitCursor;
            if (this.Collection == null)
            {
                Thread thread;
                Object[] allIds;

                this.ProgressWindow = new ProgressWindow();
                this.ProgressWindow.ProgressBar.Minimum = 0;
                this.ProgressWindow.ProgressBar.Step = 1;
                this.ProgressWindow.CanClose = false;
                allIds = (Object[])(this.Api.GetAllProfileIDs());
                this.ProgressWindow.ProgressBar.Maximum = allIds.Length;
                this.ProgressWindow.Show();
                if (TaskbarManager.IsPlatformSupported)
                {
                    TaskbarManager.Instance.OwnerHandle = this.Handle;
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                    TaskbarManager.Instance.SetProgressValue(0, this.ProgressWindow.ProgressBar.Maximum);
                }
                thread = new Thread(new ParameterizedThreadStart(this.ThreadRun));
                thread.IsBackground = false;
                thread.Start(new Object[] { allIds, coverImagesPath });
            }
            else
            {
                this.ThreadFinished(this.Collection, coverImagesPath);
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
            if (this.ProgressWindow != null)
            {
                this.ProgressWindow.CanClose = true;
                this.ProgressWindow.Close();
                this.ProgressWindow.Dispose();
                this.ProgressWindow = null;
            }
            this.Collection = collection;
            if (this.Collection != null)
            {
                this.ProcessCreditPhotosGeneral(collection.DVDList);
                validCreditPhotos = this.ProcessCreditPhotosFolder(collection.DVDList);
                this.ProcessCreditPhotosProfile(validCreditPhotos);
                this.ProcessScenePhotos(collection.DVDList);
                if (this.CleanCoverImagesCheckBox.Checked)
                {
                    this.ProcessCoverImages(coverImagesPath, collection.DVDList);
                }
                MessageBox.Show(this, MessageBoxTexts.Done, MessageBoxTexts.InformationHeader, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            this.Cursor = Cursors.Default;
            this.UseWaitCursor = false;
            this.CanClose = true;
        }

        private void OnReadMeToolStripMenuItemClick(Object sender, EventArgs e)
        {
            this.OpenReadMe();
        }

        private void OnMainFormFormClosing(Object sender, FormClosingEventArgs e)
        {
            if (this.CanClose == false)
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

            step = StartProgress(dvdList.Length);
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

                UpdateProgressBar(index, step);
                index++;
            }

            EndProgress();

            coverImagesListBoxItems = new List<ListBoxItem>(validCoverImages.Count);
            foreach (KeyValuePair<String, DVD> kvp in validCoverImages)
            {
                coverImagesListBoxItems.Add(new ListBoxItemWithDVD(kvp.Key, kvp.Value));
            }
            coverImagesListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            this.ValidCoverImagesFolderListBox.Items.AddRange(coverImagesListBoxItems.ToArray());
            if (this.ValidCoverImagesFolderListBox.Items.Count > 0)
            {
                this.ValidCoverImagesFolderListBox.SelectedIndex = 0;
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
            this.InvalidCoverImagesFolderListBox.Items.AddRange(coverImagesListBoxItems.ToArray());
            if (this.InvalidCoverImagesFolderListBox.Items.Count > 0)
            {
                this.InvalidCoverImagesFolderListBox.SelectedIndex = 0;
            }
        }

        private void ProcessScenePhotos(DVD[] dvdList)
        {
            List<String> scenePhotos;
            List<ListBoxItem> scenePhotosListBoxItems;
            Dictionary<String, DVD> validScenePhotos;
            Int32 index;
            Int32 step;

            scenePhotos = new List<String>(Directory.GetDirectories(this.ScenePhotosFolderTextbox.Text));
            scenePhotos = scenePhotos.ConvertAll<String>(new Converter<String, String>(ConvertAllDirectory));
            validScenePhotos = new Dictionary<String, DVD>(scenePhotos.Count);


            step = StartProgress(dvdList.Length);
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

                UpdateProgressBar(index, step);
                index++;
            }

            EndProgress();

            scenePhotosListBoxItems = new List<ListBoxItem>(validScenePhotos.Count);
            foreach (KeyValuePair<String, DVD> kvp in validScenePhotos)
            {
                scenePhotosListBoxItems.Add(new ListBoxItemWithDVD(kvp.Key, kvp.Value));
            }
            scenePhotosListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            this.ValidScenePhotosFolderListBox.Items.AddRange(scenePhotosListBoxItems.ToArray());
            if (this.ValidScenePhotosFolderListBox.Items.Count > 0)
            {
                this.ValidScenePhotosFolderListBox.SelectedIndex = 0;
            }
            scenePhotosListBoxItems = new List<ListBoxItem>(scenePhotos.Count);
            foreach (String folderName in scenePhotos)
            {
                scenePhotosListBoxItems.Add(new ListBoxItem(folderName));
            }
            scenePhotosListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            this.InvalidScenePhotosFolderListBox.Items.AddRange(scenePhotosListBoxItems.ToArray());
            if (this.InvalidScenePhotosFolderListBox.Items.Count > 0)
            {
                this.InvalidScenePhotosFolderListBox.SelectedIndex = 0;
            }
        }

        private Dictionary<String, DVD> ProcessCreditPhotosFolder(DVD[] dvdList)
        {
            List<String> creditPhotos;
            List<ListBoxItem> creditPhotosFolderListBoxItems;
            Dictionary<String, DVD> validCreditPhotos;
            Int32 index;
            Int32 step;

            creditPhotos = new List<String>(Directory.GetDirectories(this.CreditPhotosFolderTextBox.Text));
            creditPhotos = creditPhotos.ConvertAll<String>(new Converter<String, String>(ConvertAllDirectory));
            validCreditPhotos = new Dictionary<String, DVD>(creditPhotos.Count);

            step = StartProgress(dvdList.Length);
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

                UpdateProgressBar(index, step);
                index++;
            }

            EndProgress();

            creditPhotosFolderListBoxItems = new List<ListBoxItem>(validCreditPhotos.Count);
            foreach (KeyValuePair<String, DVD> kvp in validCreditPhotos)
            {
                creditPhotosFolderListBoxItems.Add(new ListBoxItemWithDVD(kvp.Key, kvp.Value));
            }
            creditPhotosFolderListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            this.ValidCreditPhotosFolderFolderListBox.Items.AddRange(creditPhotosFolderListBoxItems.ToArray());
            if (this.ValidCreditPhotosFolderFolderListBox.Items.Count > 0)
            {
                this.ValidCreditPhotosFolderFolderListBox.SelectedIndex = 0;
            }
            creditPhotosFolderListBoxItems = new List<ListBoxItem>(creditPhotos.Count);
            foreach (String folderName in creditPhotos)
            {
                creditPhotosFolderListBoxItems.Add(new ListBoxItem(folderName));
            }
            creditPhotosFolderListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            this.InvalidCreditPhotosFolderFolderListBox.Items.AddRange(creditPhotosFolderListBoxItems.ToArray());
            if (this.InvalidCreditPhotosFolderFolderListBox.Items.Count > 0)
            {
                this.InvalidCreditPhotosFolderFolderListBox.SelectedIndex = 0;
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

            step = StartProgress(validCreditPhotosBase.Count);

            index = 0;
            foreach (KeyValuePair<String, DVD> kvp in validCreditPhotosBase)
            {
                String path;
                List<String> creditPhotos;
                Dictionary<String, Boolean> validCreditPhotos;

                path = Path.Combine(this.CreditPhotosFolderTextBox.Text, kvp.Key);
                creditPhotos = new List<String>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));

                creditPhotos = creditPhotos.ConvertAll(item => ConvertAllFile(item, path));
                validCreditPhotos = new Dictionary<String, Boolean>(creditPhotos.Count);
                if ((kvp.Value.CastList != null) && (kvp.Value.CastList.Length > 0))
                {
                    foreach (Object potentialCastMember in kvp.Value.CastList)
                    {
                        ProcessPerson(validCreditPhotos, creditPhotos, potentialCastMember);
                    }
                }
                if ((kvp.Value.CrewList != null) && (kvp.Value.CrewList.Length > 0))
                {
                    foreach (Object potentialCrewMember in kvp.Value.CrewList)
                    {
                        ProcessPerson(validCreditPhotos, creditPhotos, potentialCrewMember);
                    }
                }
                creditPhotosProfileListBoxItems.Add(new ListBoxItemWithNames(kvp.Key, kvp.Value, new List<String>(validCreditPhotos.Keys), creditPhotos));

                UpdateProgressBar(index, step);
                index++;
            }

            EndProgress();

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
            this.ValidCreditPhotosFolderProfileListBox.Items.AddRange(validCreditPhotosProfileListBoxItems.ToArray());
            if (this.ValidCreditPhotosFolderProfileListBox.Items.Count > 0)
            {
                this.ValidCreditPhotosFolderProfileListBox.SelectedIndex = 0;
            }
            creditPhotosProfileListBoxItems.Sort(new Comparison<ListBoxItemWithNames>(CompareListBoxItems));
            this.InvalidCreditPhotosFolderProfileListBox.Items.AddRange(creditPhotosProfileListBoxItems.ToArray());
            if (this.InvalidCreditPhotosFolderProfileListBox.Items.Count > 0)
            {
                this.InvalidCreditPhotosFolderProfileListBox.SelectedIndex = 0;
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
                    success = ProcessEntry(validCreditPhotos, creditPhotos, extendedfileName);
                }
                if (success == false)
                {
                    String fileName;

                    fileName = ProfilePhotoHelper.FileNameFromCreditName(person.FirstName, person.MiddleName, person.LastName, 0);
                    ProcessEntry(validCreditPhotos, creditPhotos, fileName);
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

            creditPhotos = new List<String>(Directory.GetFiles(this.CreditPhotosFolderTextBox.Text, "*.*"
                          , SearchOption.TopDirectoryOnly));
            creditPhotos = creditPhotos.ConvertAll((input) =>
                    {
                        FileInfo fi;

                        fi = new FileInfo(input);
                        return (fi.Name.ToUpper());
                    }
                );
            validCreditPhotos = new Dictionary<String, List<DVD>>(creditPhotos.Count);

            step = StartProgress(dvdList.Length);
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

                UpdateProgressBar(index, step);
                index++;
            }

            EndProgress();

            creditPhotosListBoxItems = new List<ListBoxItem>(validCreditPhotos.Count);
            foreach (KeyValuePair<String, List<DVD>> kvp in validCreditPhotos)
            {
                kvp.Value.Sort(new Comparison<DVD>(delegate(DVD left, DVD right)
                        {
                            return (left.SortTitle.CompareTo(right.SortTitle));
                        }
                    ));
                creditPhotosListBoxItems.Add(new ListBoxItemWithDVDList(kvp.Key, kvp.Value));
            }
            creditPhotosListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            this.ValidCreditPhotosFileGeneralListBox.Items.AddRange(creditPhotosListBoxItems.ToArray());
            if (this.ValidCreditPhotosFileGeneralListBox.Items.Count > 0)
            {
                this.ValidCreditPhotosFileGeneralListBox.SelectedIndex = 0;
            }
            creditPhotosListBoxItems = new List<ListBoxItem>(creditPhotos.Count);
            foreach (String fileName in creditPhotos)
            {
                creditPhotosListBoxItems.Add(new ListBoxItem(fileName));
            }
            creditPhotosListBoxItems.Sort(new Comparison<ListBoxItem>(CompareListBoxItems));
            this.InvalidCreditPhotosFileGeneralListBox.Items.AddRange(creditPhotosListBoxItems.ToArray());
            if (this.InvalidCreditPhotosFileGeneralListBox.Items.Count > 0)
            {
                this.InvalidCreditPhotosFileGeneralListBox.SelectedIndex = 0;
            }
        }

        private void UpdateProgressBar(Int32 index, Int32 step)
        {
            UpdateProgressBar();

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
            this.ProgressWindow.CanClose = true;
            this.ProgressWindow.Close();
            this.ProgressWindow.Dispose();
            this.ProgressWindow = null;
        }

        private Int32 StartProgress(Int32 count)
        {
            Int32 step;

            this.ProgressWindow = new ProgressWindow();
            this.ProgressWindow.ProgressBar.Minimum = 0;
            this.ProgressWindow.ProgressBar.Step = 1;
            this.ProgressWindow.CanClose = false;
            this.ProgressWindow.ProgressBar.Maximum = count;
            this.ProgressWindow.Show();
            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.OwnerHandle = this.Handle;
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                TaskbarManager.Instance.SetProgressValue(0, this.ProgressWindow.ProgressBar.Maximum);
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

            dvdInfo = this.Api.GetDisplayedDVD();
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
                    dvd = Serializer<DVD>.FromString(xml, DVD.DefaultEncoding);
                    dvdList.Add(dvd);

                    this.Invoke(new ProgressBarDelegate(this.UpdateProgressBar));
                }
                collection = new Collection();
                collection.DVDList = dvdList.ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MessageBoxTexts.ErrorHeader, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Invoke(new ThreadFinishedDelegate(this.ThreadFinished), collection, coverImagesPath);
            }
        }

        private void UpdateProgressBar()
        {
            this.ProgressWindow.ProgressBar.PerformStep();
            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressValue(this.ProgressWindow.ProgressBar.Value, this.ProgressWindow.ProgressBar.Maximum);
            }
        }

        private String GetProfileData(Object id)
        {
            IDVDInfo dvdInfo;
            String xml;

            this.Api.DVDByProfileID(out dvdInfo, (id).ToString(), -1, -1);
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

            index = this.ValidCreditPhotosFileGeneralListBox.SelectedIndex;
            this.ValidCreditPhotosProfileGeneralListBox.ClearItems();
            ResetPictureBox(this.ValidCreditPhotosGeneralPictureBox);
            if (index != -1)
            {
                ListBoxItemWithDVDList item;

                item = (ListBoxItemWithDVDList)(this.ValidCreditPhotosFileGeneralListBox.SelectedItem);
                this.ValidCreditPhotosProfileGeneralListBox.Items.AddRange(item.DVDList.ToArray());
                try
                {
                    String file;

                    file = Path.Combine(this.CreditPhotosFolderTextBox.Text, item.Name);
                    if (HasPictureExtension(file))
                    {
                        this.ValidCreditPhotosGeneralPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    this.ValidCreditPhotosGeneralPictureBox.Image = (Image)(this.ValidCreditPhotosGeneralPictureBox.ErrorImage.Clone());
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

            index = this.InvalidCreditPhotosFileGeneralListBox.SelectedIndex;
            ResetPictureBox(this.InvalidCreditPhotosGeneralPictureBox);
            if (index != -1)
            {
                ListBoxItem item;

                item = (ListBoxItem)(this.InvalidCreditPhotosFileGeneralListBox.SelectedItem);
                try
                {
                    String file;

                    file = Path.Combine(this.CreditPhotosFolderTextBox.Text, item.Name);
                    if (HasPictureExtension(file))
                    {
                        this.InvalidCreditPhotosGeneralPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    this.InvalidCreditPhotosGeneralPictureBox.Image = (Image)(this.InvalidCreditPhotosGeneralPictureBox.ErrorImage.Clone());
                }
                this.RemoveInvalidCreditPhotosGeneralButton.Enabled = true;
            }
            else
            {
                this.RemoveInvalidCreditPhotosGeneralButton.Enabled = false;
            }
            if (this.InvalidCreditPhotosFileGeneralListBox.Items.Count > 0)
            {
                this.RemoveAllInvalidCreditPhotosGeneralButton.Enabled = true;
                this.CopyToSpecificProfileInvalidGeneralButton.Enabled = true;
            }
            else
            {
                this.RemoveAllInvalidCreditPhotosGeneralButton.Enabled = false;
                this.CopyToSpecificProfileInvalidGeneralButton.Enabled = false;
            }
        }

        private void OnRemoveInvalidCreditPhotosGeneralButtonClick(Object sender, EventArgs e)
        {
            if (this.InvalidCreditPhotosFileGeneralListBox.SelectedIndex != -1)
            {
                Int32 index;

                index = this.InvalidCreditPhotosFileGeneralListBox.SelectedIndex;
                if (MessageBox.Show(String.Format("Remove file?", this.InvalidCreditPhotosFileGeneralListBox.Items.Count), "Remove"
                    , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        String file;
                        ListBoxItem item;

                        item = (ListBoxItem)(this.InvalidCreditPhotosFileGeneralListBox.SelectedItem);
                        file = Path.Combine(this.CreditPhotosFolderTextBox.Text, item.Name);
                        ResetPictureBox(this.InvalidCreditPhotosGeneralPictureBox);
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    this.InvalidCreditPhotosFileGeneralListBox.Items.RemoveAt(index);
                    if (this.InvalidCreditPhotosFileGeneralListBox.Items.Count > 0)
                    {
                        if (index == 0)
                        {
                            this.InvalidCreditPhotosFileGeneralListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            this.InvalidCreditPhotosFileGeneralListBox.SelectedIndex = index - 1;
                        }
                    }
                    else
                    {
                        this.InvalidCreditPhotosFileGeneralListBox.SelectedIndex = -1;
                    }
                }
            }
            if (this.InvalidCreditPhotosFileGeneralListBox.SelectedIndex != -1)
            {
                this.RemoveInvalidCreditPhotosGeneralButton.Enabled = true;
            }
            else
            {
                this.RemoveInvalidCreditPhotosGeneralButton.Enabled = false;
            }
        }

        private void OnRemoveAllInvalidCreditPhotosGeneralButtonClick(Object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Remove {0} files?", this.InvalidCreditPhotosFileGeneralListBox.Items.Count), "Remove"
                , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                List<ListBoxItem> list;

                ResetPictureBox(this.InvalidCreditPhotosGeneralPictureBox);
                list = new List<ListBoxItem>(this.InvalidCreditPhotosFileGeneralListBox.Items.Count);
                foreach (ListBoxItem item in this.InvalidCreditPhotosFileGeneralListBox.Items)
                {
                    list.Add(item);
                }
                try
                {
                    for (Int32 i = list.Count - 1; i >= 0; i--)
                    {
                        String file;

                        file = Path.Combine(this.CreditPhotosFolderTextBox.Text, list[i].Name);
                        File.Delete(file);
                        list.RemoveAt(i);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.InvalidCreditPhotosFileGeneralListBox.ClearItems();
                    this.InvalidCreditPhotosFileGeneralListBox.Items.AddRange(list.ToArray());
                    if (this.InvalidCreditPhotosFileGeneralListBox.Items.Count > 0)
                    {
                        this.InvalidCreditPhotosFileGeneralListBox.SelectedIndex = 0;
                    }
                    else
                    {
                        this.InvalidCreditPhotosFileGeneralListBox.SelectedIndex = -1;
                    }
                    return;
                }
                this.InvalidCreditPhotosFileGeneralListBox.ClearItems();
                FireSelectionChanged();
            }
        }

        private void OnCopyToSpecificProfileInvalidGeneralButtonClick(Object sender, EventArgs e)
        {
            CopyImageGeneral(InvalidCreditPhotosFileGeneralListBox);
        }

        private void OnCopyToSpecificProfileValidGeneralButtonClick(Object sender, EventArgs e)
        {
            CopyImageGeneral(ValidCreditPhotosFileGeneralListBox);
        }
        #endregion

        #region Folder
        private void OnInvalidCreditPhotosFolderFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = this.InvalidCreditPhotosFolderFolderListBox.SelectedIndex;
            this.InvalidCreditPhotosFileFolderListBox.ClearItems();
            ResetPictureBox(this.InvalidCreditPhotosFolderPictureBox);
            if (index != -1)
            {
                try
                {
                    List<String> files;
                    String path;

                    path = Path.Combine(this.CreditPhotosFolderTextBox.Text
                        , this.InvalidCreditPhotosFolderFolderListBox.SelectedItem.ToString());
                    files = new List<String>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort();
                    this.InvalidCreditPhotosFileFolderListBox.Items.AddRange(files.ToArray());
                    if (this.InvalidCreditPhotosFileFolderListBox.Items.Count > 0)
                    {
                        this.InvalidCreditPhotosFileFolderListBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                this.RemoveInvalidCreditPhotosFolderButton.Enabled = true;
            }
            else
            {
                this.RemoveInvalidCreditPhotosFolderButton.Enabled = false;
            }
            if (this.InvalidCreditPhotosFolderFolderListBox.Items.Count > 0)
            {
                this.RemoveAllInvalidCreditPhotosFolderButton.Enabled = true;
            }
            else
            {
                this.RemoveAllInvalidCreditPhotosFolderButton.Enabled = false;
            }
        }

        private void OnInvalidCreditPhotosFileFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = this.InvalidCreditPhotosFileFolderListBox.SelectedIndex;
            ResetPictureBox(this.InvalidCreditPhotosFolderPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(this.InvalidCreditPhotosFileFolderListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(this.CreditPhotosFolderTextBox.Text
                        , this.InvalidCreditPhotosFolderFolderListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        this.InvalidCreditPhotosFolderPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    this.InvalidCreditPhotosFolderPictureBox.Image = (Image)(this.InvalidCreditPhotosFolderPictureBox.ErrorImage.Clone());
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

            index = this.ValidCreditPhotosFolderFolderListBox.SelectedIndex;
            this.ValidCreditPhotosProfileFolderTextBox.Text = String.Empty;
            this.ValidCreditPhotosFileFolderListBox.ClearItems();
            ResetPictureBox(this.ValidCreditPhotosFolderPictureBox);
            if (index != -1)
            {
                try
                {
                    List<String> files;
                    ListBoxItemWithDVD listBoxItem;
                    String path;

                    listBoxItem = (ListBoxItemWithDVD)(this.ValidCreditPhotosFolderFolderListBox.SelectedItem);
                    this.ValidCreditPhotosProfileFolderTextBox.Text = listBoxItem.DVD.ToString();
                    path = Path.Combine(this.CreditPhotosFolderTextBox.Text, listBoxItem.Name);
                    files = new List<String>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort();
                    this.ValidCreditPhotosFileFolderListBox.Items.AddRange(files.ToArray());
                    if (this.ValidCreditPhotosFileFolderListBox.Items.Count > 0)
                    {
                        this.ValidCreditPhotosFileFolderListBox.SelectedIndex = 0;
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

            index = this.ValidCreditPhotosFileFolderListBox.SelectedIndex;
            ResetPictureBox(this.ValidCreditPhotosFolderPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(this.ValidCreditPhotosFileFolderListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(this.CreditPhotosFolderTextBox.Text
                        , this.ValidCreditPhotosFolderFolderListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        this.ValidCreditPhotosFolderPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    this.ValidCreditPhotosFolderPictureBox.Image = (Image)(this.ValidCreditPhotosFolderPictureBox.ErrorImage.Clone());
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
            if (this.InvalidCreditPhotosFolderFolderListBox.SelectedIndex != -1)
            {
                Int32 index;

                index = this.InvalidCreditPhotosFolderFolderListBox.SelectedIndex;
                if (MessageBox.Show(String.Format("Remove folder '{0}' and {1} files in it?", this.InvalidCreditPhotosFolderFolderListBox.SelectedItem
                    , this.InvalidCreditPhotosFileFolderListBox.Items.Count), "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        String path;
                        ListBoxItem item;
                        String[] files;

                        ResetPictureBox(this.InvalidCreditPhotosFolderPictureBox);
                        item = (ListBoxItem)(this.InvalidCreditPhotosFolderFolderListBox.SelectedItem);
                        path = Path.Combine(this.CreditPhotosFolderTextBox.Text, item.Name);
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
                        FireSelectionChanged();
                        return;
                    }
                    this.InvalidCreditPhotosFolderFolderListBox.Items.RemoveAt(index);
                    if (this.InvalidCreditPhotosFolderFolderListBox.Items.Count > 0)
                    {
                        if (index == 0)
                        {
                            this.InvalidCreditPhotosFolderFolderListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            this.InvalidCreditPhotosFolderFolderListBox.SelectedIndex = index - 1;
                        }
                    }
                    else
                    {
                        this.InvalidCreditPhotosFolderFolderListBox.SelectedIndex = -1;
                    }
                }
            }
            if (this.InvalidCreditPhotosFolderFolderListBox.SelectedIndex != -1)
            {
                this.RemoveInvalidCreditPhotosFolderButton.Enabled = true;
            }
            else
            {
                this.RemoveInvalidCreditPhotosFolderButton.Enabled = false;
            }
        }

        private void OnRemoveAllInvalidCreditPhotosFolderButtonClick(Object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Remove {0} folders and the files in them?", this.InvalidCreditPhotosFolderFolderListBox.Items.Count)
                , "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                List<ListBoxItem> list;

                ResetPictureBox(this.InvalidCreditPhotosFolderPictureBox);
                list = new List<ListBoxItem>(this.InvalidCreditPhotosFolderFolderListBox.Items.Count);
                foreach (ListBoxItem item in this.InvalidCreditPhotosFolderFolderListBox.Items)
                {
                    list.Add(item);
                }
                try
                {
                    for (Int32 i = list.Count - 1; i >= 0; i--)
                    {
                        String path;
                        String[] files;

                        path = Path.Combine(this.CreditPhotosFolderTextBox.Text, list[i].Name);
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
                    this.InvalidCreditPhotosFolderFolderListBox.ClearItems();
                    this.InvalidCreditPhotosFolderFolderListBox.Items.AddRange(list.ToArray());
                    if (this.InvalidCreditPhotosFolderFolderListBox.Items.Count > 0)
                    {
                        this.InvalidCreditPhotosFolderFolderListBox.SelectedIndex = 0;
                    }
                    else
                    {
                        this.InvalidCreditPhotosFolderFolderListBox.SelectedIndex = -1;
                    }
                    return;
                }
                this.InvalidCreditPhotosFolderFolderListBox.ClearItems();
                FireSelectionChanged();
            }
        }

        private void OnCopyToSpecificProfileInvalidFolderButtonClick(Object sender, EventArgs e)
        {
            CopyImageProfile(InvalidCreditPhotosFileFolderListBox, InvalidCreditPhotosFolderFolderListBox);
        }

        private void OnCopyToSpecificProfileValidFolderButtonClick(Object sender, EventArgs e)
        {
            CopyImageProfile(ValidCreditPhotosFileFolderListBox, ValidCreditPhotosFolderFolderListBox);
        }
        #endregion

        #region Profile
        private void OnValidCreditPhotosFolderProfileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = this.ValidCreditPhotosFolderProfileListBox.SelectedIndex;
            this.ValidCreditPhotosProfileProfileTextBox.Text = String.Empty;
            this.ValidCreditPhotosFileProfileListBox.ClearItems();
            ResetPictureBox(this.ValidCreditPhotosProfilePictureBox);
            if (index != -1)
            {
                ListBoxItemWithNames listBoxItem;

                listBoxItem = (ListBoxItemWithNames)(this.ValidCreditPhotosFolderProfileListBox.SelectedItem);
                this.ValidCreditPhotosProfileProfileTextBox.Text = listBoxItem.DVD.ToString();
                this.ValidCreditPhotosFileProfileListBox.Items.AddRange(listBoxItem.ValidList.ToArray());
                if (this.ValidCreditPhotosFileProfileListBox.Items.Count > 0)
                {
                    this.ValidCreditPhotosFileProfileListBox.SelectedIndex = 0;
                }
            }
        }

        private void OnValidCreditPhotosFileProfileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = this.ValidCreditPhotosFileProfileListBox.SelectedIndex;
            ResetPictureBox(this.ValidCreditPhotosProfilePictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(this.ValidCreditPhotosFileProfileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(this.CreditPhotosFolderTextBox.Text
                        , this.ValidCreditPhotosFolderProfileListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        this.ValidCreditPhotosProfilePictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    this.ValidCreditPhotosProfilePictureBox.Image = (Image)(this.ValidCreditPhotosProfilePictureBox.ErrorImage.Clone());
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

            index = this.InvalidCreditPhotosFolderProfileListBox.SelectedIndex;
            this.InvalidCreditPhotosInvalidFileProfileListBox.ClearItems();
            ResetPictureBox(this.InvalidCreditPhotosInvalidFileProfilePictureBox);
            this.InvalidCreditPhotosValidFileProfileListBox.ClearItems();
            ResetPictureBox(this.InvalidCreditPhotosValidFileProfilePictureBox);
            if (index != -1)
            {
                ListBoxItemWithNames listBoxItem;

                listBoxItem = (ListBoxItemWithNames)(this.InvalidCreditPhotosFolderProfileListBox.SelectedItem);
                this.InvalidCreditPhotosProfileProfileTextBox.Text = listBoxItem.DVD.ToString();
                this.InvalidCreditPhotosInvalidFileProfileListBox.Items.AddRange(listBoxItem.InvalidList.ToArray());
                if (this.InvalidCreditPhotosInvalidFileProfileListBox.Items.Count > 0)
                {
                    this.InvalidCreditPhotosInvalidFileProfileListBox.SelectedIndex = 0;
                }
                this.InvalidCreditPhotosValidFileProfileListBox.Items.AddRange(listBoxItem.ValidList.ToArray());
                if (this.InvalidCreditPhotosValidFileProfileListBox.Items.Count > 0)
                {
                    this.InvalidCreditPhotosValidFileProfileListBox.SelectedIndex = 0;
                }
                this.RemoveInvalidCreditPhotosProfileButton.Enabled = true;
            }
            else
            {
                this.InvalidCreditPhotosProfileProfileTextBox.Text = String.Empty;
                this.RemoveInvalidCreditPhotosProfileButton.Enabled = false;
            }
            if (this.InvalidCreditPhotosFolderProfileListBox.Items.Count > 0)
            {
                this.RemoveAllInvalidCreditPhotosProfileButton.Enabled = true;
            }
            else
            {
                this.RemoveAllInvalidCreditPhotosProfileButton.Enabled = false;
            }
        }

        private void OnInvalidCreditPhotosValidFileProfileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = this.InvalidCreditPhotosValidFileProfileListBox.SelectedIndex;
            ResetPictureBox(this.InvalidCreditPhotosValidFileProfilePictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(this.InvalidCreditPhotosValidFileProfileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(this.CreditPhotosFolderTextBox.Text
                        , this.InvalidCreditPhotosFolderProfileListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        this.InvalidCreditPhotosValidFileProfilePictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    this.InvalidCreditPhotosValidFileProfilePictureBox.Image
                        = (Image)(this.InvalidCreditPhotosValidFileProfilePictureBox.ErrorImage.Clone());
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

            index = this.InvalidCreditPhotosInvalidFileProfileListBox.SelectedIndex;
            ResetPictureBox(this.InvalidCreditPhotosInvalidFileProfilePictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(this.InvalidCreditPhotosInvalidFileProfileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(this.CreditPhotosFolderTextBox.Text
                        , this.InvalidCreditPhotosFolderProfileListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        this.InvalidCreditPhotosInvalidFileProfilePictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    this.InvalidCreditPhotosInvalidFileProfilePictureBox.Image
                        = (Image)(this.InvalidCreditPhotosInvalidFileProfilePictureBox.ErrorImage.Clone());
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
            if (this.InvalidCreditPhotosFolderProfileListBox.SelectedIndex != -1)
            {
                Int32 index;

                index = this.InvalidCreditPhotosFolderProfileListBox.SelectedIndex;
                if (MessageBox.Show(String.Format("Remove {0} files?", this.InvalidCreditPhotosInvalidFileProfileListBox.Items.Count)
                    , "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    ListBoxItemWithNames item;
                    String path;

                    ResetPictureBox(this.InvalidCreditPhotosInvalidFileProfilePictureBox);
                    ResetPictureBox(this.ValidCreditPhotosFolderPictureBox);
                    item = (ListBoxItemWithNames)(this.InvalidCreditPhotosFolderProfileListBox.SelectedItem);
                    path = Path.Combine(this.CreditPhotosFolderTextBox.Text, item.Name);
                    if (this.RemoveInvalidCreditPhotosOnValidFolder(item, path, false) == false)
                    {
                        return;
                    }
                    this.InvalidCreditPhotosFolderProfileListBox.Items.RemoveAt(index);
                    if (this.InvalidCreditPhotosFolderProfileListBox.Items.Count > 0)
                    {
                        if (index == 0)
                        {
                            this.InvalidCreditPhotosFolderProfileListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            this.InvalidCreditPhotosFolderProfileListBox.SelectedIndex = index - 1;
                        }
                    }
                    else
                    {
                        this.InvalidCreditPhotosFolderProfileListBox.SelectedIndex = -1;
                    }
                }
            }
            if (this.InvalidCreditPhotosFolderProfileListBox.SelectedIndex != -1)
            {
                this.RemoveInvalidCreditPhotosProfileButton.Enabled = true;
            }
            else
            {
                this.RemoveInvalidCreditPhotosProfileButton.Enabled = false;
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
                    FireSelectionChanged();
                }
                return (false);
            }
            finally
            {
                if (batchMode == false)
                {
                    FireSelectionChanged();
                }
            }
            return (true);
        }

        private void OnRemoveAllInvalidCreditPhotosProfileButtonClick(Object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Remove files from {0} folders?", this.InvalidCreditPhotosFolderProfileListBox.Items.Count)
               , "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                List<ListBoxItemWithNames> list;

                ResetPictureBox(this.InvalidCreditPhotosInvalidFileProfilePictureBox);
                ResetPictureBox(this.ValidCreditPhotosFolderPictureBox);
                list = new List<ListBoxItemWithNames>(this.InvalidCreditPhotosFolderProfileListBox.Items.Count);
                foreach (ListBoxItemWithNames item in this.InvalidCreditPhotosFolderProfileListBox.Items)
                {
                    list.Add(item);
                }
                for (Int32 i = list.Count - 1; i >= 0; i--)
                {
                    String path;

                    path = Path.Combine(this.CreditPhotosFolderTextBox.Text, list[i].Name);
                    if (this.RemoveInvalidCreditPhotosOnValidFolder(list[i], path, true))
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        this.InvalidCreditPhotosFolderProfileListBox.ClearItems();
                        this.InvalidCreditPhotosFolderProfileListBox.Items.AddRange(list.ToArray());
                        if (this.InvalidCreditPhotosFolderProfileListBox.Items.Count > 0)
                        {
                            this.InvalidCreditPhotosFolderProfileListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            this.InvalidCreditPhotosFolderProfileListBox.SelectedIndex = -1;
                        }
                        FireSelectionChanged();
                        return;
                    }
                }
                this.InvalidCreditPhotosFolderProfileListBox.ClearItems();
                FireSelectionChanged();
            }
        }

        private void FireSelectionChanged()
        {
            Object sender;
            EventArgs e;

            sender = this;
            e = EventArgs.Empty;

            OnInvalidCoverImagesFileListBoxSelectedIndexChanged(sender, e);
            OnInvalidCoverImagesFolderListBoxSelectedIndexChanged(sender, e);
            OnInvalidCreditPhotosFileFolderListBoxSelectedIndexChanged(sender, e);
            OnInvalidCreditPhotosFileGeneralListBoxSelectedIndexChanged(sender, e);
            OnInvalidCreditPhotosFolderFolderListBoxSelectedIndexChanged(sender, e);
            OnInvalidCreditPhotosFolderProfileListBoxSelectedIndexChanged(sender, e);
            OnInvalidCreditPhotosInvalidFileProfileListBoxSelectedIndexChanged(sender, e);
            OnInvalidCreditPhotosValidFileProfileListBoxSelectedIndexChanged(sender, e);
            OnInvalidScenePhotosFileListBoxSelectedIndexChanged(sender, e);
            OnInvalidScenePhotosFolderListBoxSelectedIndexChanged(sender, e);

            OnValidCoverImagesFileListBoxSelectedIndexChanged(sender, e);
            OnValidCoverImagesFolderListBoxSelectedIndexChanged(sender, e);
            OnValidCreditPhotosFileFolderListBoxSelectedIndexChanged(sender, e);
            OnValidCreditPhotosFileGeneralListBoxSelectedIndexChanged(sender, e);
            OnValidCreditPhotosFileProfileListBoxSelectedIndexChanged(sender, e);
            OnValidCreditPhotosFolderFolderListBoxSelectedIndexChanged(sender, e);
            OnValidCreditPhotosFolderProfileListBoxSelectedIndexChanged(sender, e);
            OnValidScenePhotosFileListBoxSelectedIndexChanged(sender, e);
            OnValidScenePhotosFolderListBoxSelectedIndexChanged(sender, e);
        }

        private void OnCopyToSpecificProfileInvalidProfileInvalidPhotoButtonClick(Object sender, EventArgs e)
        {
            CopyImageProfile(InvalidCreditPhotosInvalidFileProfileListBox, InvalidCreditPhotosFolderProfileListBox);
        }

        private void OnCopyToSpecificProfileInvalidProfileValidPhotoButtonClick(Object sender, EventArgs e)
        {
            CopyImageProfile(InvalidCreditPhotosValidFileProfileListBox, InvalidCreditPhotosFolderProfileListBox);
        }

        private void OnCopyToSpecificProfileValidProfileButtonClick(Object sender, EventArgs e)
        {
            CopyImageProfile(ValidCreditPhotosFileProfileListBox, ValidCreditPhotosFolderProfileListBox);
        }
        #endregion
        #endregion

        #region Scene Photos
        private void OnInvalidScenePhotosFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = this.InvalidScenePhotosFolderListBox.SelectedIndex;
            this.InvalidScenePhotosFileListBox.ClearItems();
            ResetPictureBox(this.InvalidScenePhotosPictureBox);
            if (index != -1)
            {
                try
                {
                    List<String> files;
                    String path;

                    path = Path.Combine(this.ScenePhotosFolderTextbox.Text
                        , this.InvalidScenePhotosFolderListBox.SelectedItem.ToString());
                    files = new List<String>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));

                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort();
                    this.InvalidScenePhotosFileListBox.Items.AddRange(files.ToArray());
                    if (this.InvalidScenePhotosFileListBox.Items.Count > 0)
                    {
                        this.InvalidScenePhotosFileListBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                this.RemoveInvalidScenePhotosButton.Enabled = true;
            }
            else
            {
                this.RemoveInvalidScenePhotosButton.Enabled = false;
            }
            if (this.InvalidScenePhotosFolderListBox.Items.Count > 0)
            {
                this.RemoveAllInvalidScenePhotosButton.Enabled = true;
            }
            else
            {
                this.RemoveAllInvalidScenePhotosButton.Enabled = false;
            }
        }

        private void OnInvalidScenePhotosFileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = this.InvalidScenePhotosFileListBox.SelectedIndex;
            ResetPictureBox(this.InvalidScenePhotosPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(this.InvalidScenePhotosFileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(this.ScenePhotosFolderTextbox.Text
                        , this.InvalidScenePhotosFolderListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        this.InvalidScenePhotosPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    this.InvalidScenePhotosPictureBox.Image = (Image)(this.InvalidScenePhotosPictureBox.ErrorImage.Clone());
                }
            }
        }

        private void OnValidScenePhotosFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = this.ValidScenePhotosFolderListBox.SelectedIndex;
            this.ValidScenePhotosProfileTextBox.Text = String.Empty;
            this.ValidScenePhotosFileListBox.ClearItems();
            ResetPictureBox(this.ValidScenePhotosPictureBox);
            if (index != -1)
            {
                try
                {
                    List<String> files;
                    ListBoxItemWithDVD listBoxItem;
                    String path;

                    listBoxItem = (ListBoxItemWithDVD)(this.ValidScenePhotosFolderListBox.SelectedItem);
                    this.ValidScenePhotosProfileTextBox.Text = listBoxItem.DVD.ToString();
                    path = Path.Combine(this.ScenePhotosFolderTextbox.Text, listBoxItem.Name);
                    files = new List<String>(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort();
                    this.ValidScenePhotosFileListBox.Items.AddRange(files.ToArray());
                    if (this.ValidScenePhotosFileListBox.Items.Count > 0)
                    {
                        this.ValidScenePhotosFileListBox.SelectedIndex = 0;
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

            index = this.ValidScenePhotosFileListBox.SelectedIndex;
            ResetPictureBox(this.ValidScenePhotosPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(this.ValidScenePhotosFileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(this.ScenePhotosFolderTextbox.Text
                        , this.ValidScenePhotosFolderListBox.SelectedItem.ToString());
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        this.ValidScenePhotosPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    this.ValidScenePhotosPictureBox.Image = (Image)(this.ValidScenePhotosPictureBox.ErrorImage.Clone());
                }
            }
        }

        private void OnRemoveInvalidScenePhotosButtonClick(Object sender, EventArgs e)
        {
            if (this.InvalidScenePhotosFolderListBox.SelectedIndex != -1)
            {
                Int32 index;

                index = this.InvalidScenePhotosFolderListBox.SelectedIndex;
                if (MessageBox.Show(String.Format("Remove folder '{0}' and {1} files in it?", this.InvalidScenePhotosFolderListBox.SelectedItem
                    , this.InvalidScenePhotosFileListBox.Items.Count), "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        String path;
                        ListBoxItem item;
                        String[] files;

                        ResetPictureBox(this.InvalidScenePhotosPictureBox);
                        item = (ListBoxItem)(this.InvalidScenePhotosFolderListBox.SelectedItem);
                        path = Path.Combine(this.ScenePhotosFolderTextbox.Text, item.Name);
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
                        FireSelectionChanged();
                        return;
                    }
                    this.InvalidScenePhotosFolderListBox.Items.RemoveAt(index);
                    if (this.InvalidScenePhotosFolderListBox.Items.Count > 0)
                    {
                        if (index == 0)
                        {
                            this.InvalidScenePhotosFolderListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            this.InvalidScenePhotosFolderListBox.SelectedIndex = index - 1;
                        }
                    }
                    else
                    {
                        this.InvalidScenePhotosFolderListBox.SelectedIndex = -1;
                    }
                }
            }
            if (this.InvalidScenePhotosFolderListBox.SelectedIndex != -1)
            {
                this.RemoveInvalidScenePhotosButton.Enabled = true;
            }
            else
            {
                this.RemoveInvalidScenePhotosButton.Enabled = false;
            }
        }

        private void OnRemoveAllInvalidScenePhotosButtonClick(Object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Remove {0} folders and the files in them?", this.InvalidScenePhotosFolderListBox.Items.Count), "Remove"
                    , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                List<ListBoxItem> list;

                ResetPictureBox(this.InvalidScenePhotosPictureBox);
                list = new List<ListBoxItem>(this.InvalidScenePhotosFolderListBox.Items.Count);
                foreach (ListBoxItem item in this.InvalidScenePhotosFolderListBox.Items)
                {
                    list.Add(item);
                }
                try
                {
                    for (Int32 i = list.Count - 1; i >= 0; i--)
                    {
                        String path;
                        String[] files;

                        path = Path.Combine(this.ScenePhotosFolderTextbox.Text, list[i].Name);
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
                    this.InvalidScenePhotosFolderListBox.ClearItems();
                    this.InvalidScenePhotosFolderListBox.Items.AddRange(list.ToArray());
                    if (this.InvalidScenePhotosFolderListBox.Items.Count > 0)
                    {
                        this.InvalidScenePhotosFolderListBox.SelectedIndex = 0;
                    }
                    else
                    {
                        this.InvalidScenePhotosFolderListBox.SelectedIndex = -1;
                    }
                    return;
                }
                this.InvalidScenePhotosFolderListBox.ClearItems();
                FireSelectionChanged();
            }
        }
        #endregion

        #region Cover Images
        private void OnCleanCoverImagesCheckBoxCheckedChanged(Object sender, EventArgs e)
        {
            this.DatabaseFolderTextbox.Enabled = this.CleanCoverImagesCheckBox.Checked;
            this.SelectDatabaseFolderButton.Enabled = this.CleanCoverImagesCheckBox.Checked;
            this.CoverImagesTabControl.Enabled = this.CleanCoverImagesCheckBox.Checked;
            Plugin.Settings.DefaultValues.CleanUpCoverImages = this.CleanCoverImagesCheckBox.Checked;
        }

        private void OnSelectDatabaseFolderButtonClick(Object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                Boolean ok;

                fbd.Description = "Please select your Database folder";
                fbd.SelectedPath = this.DatabaseFolderTextbox.Text;
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
                            this.DatabaseFolderTextbox.Text = fbd.SelectedPath;
                        }
                    }
                } while (ok == false);
            }
        }

        private void OnValidCoverImagesFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = this.ValidCoverImagesFolderListBox.SelectedIndex;
            this.ValidCoverImagesProfileTextBox.Text = String.Empty;
            this.ValidCoverImagesFileListBox.ClearItems();
            ResetPictureBox(this.ValidCoverImagesPictureBox);
            if (index != -1)
            {
                try
                {
                    String path;
                    List<String> files;
                    ListBoxItemWithDVD listBoxItem;
                    String filter;

                    listBoxItem = (ListBoxItemWithDVD)(this.ValidCoverImagesFolderListBox.SelectedItem);
                    this.ValidCoverImagesProfileTextBox.Text = listBoxItem.DVD.ToString();
                    path = Path.Combine(this.DatabaseFolderTextbox.Text, "Images");
                    if (Directory.Exists(path) == false)
                    {
                        MessageBox.Show("Cover Images folder file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    filter = this.ValidCoverImagesFolderListBox.SelectedItem.ToString();
                    files = new List<String>(Directory.GetFiles(path, filter + "*.*", SearchOption.AllDirectories));
                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort(CompareReverse);
                    this.ValidCoverImagesFileListBox.Items.AddRange(files.ToArray());
                    if (this.ValidCoverImagesFileListBox.Items.Count > 0)
                    {
                        this.ValidCoverImagesFileListBox.SelectedIndex = 0;
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

            index = this.ValidCoverImagesFileListBox.SelectedIndex;
            ResetPictureBox(this.ValidCoverImagesPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(this.ValidCoverImagesFileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(this.DatabaseFolderTextbox.Text, "Images");
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        this.ValidCoverImagesPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    this.ValidCoverImagesPictureBox.Image = (Image)(this.ValidCoverImagesPictureBox.ErrorImage.Clone());
                }
            }
        }

        private void OnRemoveInvalidCoverImagesButtonClick(Object sender, EventArgs e)
        {
            if (this.InvalidCoverImagesFolderListBox.SelectedIndex != -1)
            {
                Int32 index;

                index = this.InvalidCoverImagesFolderListBox.SelectedIndex;
                if (MessageBox.Show(String.Format("Remove {0} files starting with '{1}'?"
                    , this.InvalidCoverImagesFileListBox.Items.Count, this.InvalidCoverImagesFolderListBox.SelectedItem)
                    , "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        String path;
                        ListBoxItem item;
                        List<String> files;

                        ResetPictureBox(this.InvalidCoverImagesPictureBox);
                        item = (ListBoxItem)(this.InvalidCoverImagesFolderListBox.SelectedItem);
                        path = Path.Combine(this.DatabaseFolderTextbox.Text, "Images");
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
                        FireSelectionChanged();
                        return;
                    }
                    this.InvalidCoverImagesFolderListBox.Items.RemoveAt(index);
                    if (this.InvalidCoverImagesFolderListBox.Items.Count > 0)
                    {
                        if (index == 0)
                        {
                            this.InvalidCoverImagesFolderListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            this.InvalidCoverImagesFolderListBox.SelectedIndex = index - 1;
                        }
                    }
                    else
                    {
                        this.InvalidCoverImagesFolderListBox.SelectedIndex = -1;
                    }
                }
            }
            if (this.InvalidCoverImagesFolderListBox.SelectedIndex != -1)
            {
                this.RemoveInvalidCoverImagesButton.Enabled = true;
            }
            else
            {
                this.RemoveInvalidCoverImagesButton.Enabled = false;
            }
        }

        private void OnRemoveAllInvalidCoverImagesButtonClick(Object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Remove all files?", this.InvalidScenePhotosFolderListBox.Items.Count), "Remove"
                   , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                List<ListBoxItem> list;

                ResetPictureBox(this.InvalidCoverImagesPictureBox);
                list = new List<ListBoxItem>(this.InvalidCoverImagesFolderListBox.Items.Count);
                foreach (ListBoxItem item in this.InvalidCoverImagesFolderListBox.Items)
                {
                    list.Add(item);
                }
                try
                {
                    for (Int32 i = list.Count - 1; i >= 0; i--)
                    {
                        String path;
                        List<String> files;

                        path = Path.Combine(this.DatabaseFolderTextbox.Text, "Images");
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
                    this.InvalidCoverImagesFolderListBox.ClearItems();
                    this.InvalidCoverImagesFolderListBox.Items.AddRange(list.ToArray());
                    if (this.InvalidCoverImagesFolderListBox.Items.Count > 0)
                    {
                        this.InvalidCoverImagesFolderListBox.SelectedIndex = 0;
                    }
                    else
                    {
                        this.InvalidCoverImagesFolderListBox.SelectedIndex = -1;
                    }
                    return;
                }
                this.InvalidCoverImagesFolderListBox.ClearItems();
                FireSelectionChanged();
            }
        }

        private void OnInvalidCoverImagesFolderListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = this.InvalidCoverImagesFolderListBox.SelectedIndex;
            this.InvalidCoverImagesFileListBox.ClearItems();
            ResetPictureBox(this.InvalidCoverImagesPictureBox);
            if (index != -1)
            {
                try
                {
                    String path;
                    List<String> files;
                    String filter;

                    path = Path.Combine(this.DatabaseFolderTextbox.Text, "Images");
                    if (Directory.Exists(path) == false)
                    {
                        MessageBox.Show("Cover Images folder file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    filter = this.InvalidCoverImagesFolderListBox.SelectedItem.ToString();
                    files = new List<String>(Directory.GetFiles(path, filter + "b.jpg", SearchOption.AllDirectories));
                    files.AddRange(Directory.GetFiles(path, filter + "f.jpg", SearchOption.AllDirectories));
                    files = files.ConvertAll(item => ConvertAllFile(item, path));
                    files.Sort(CompareReverse);
                    this.InvalidCoverImagesFileListBox.Items.AddRange(files.ToArray());
                    if (this.InvalidCoverImagesFileListBox.Items.Count > 0)
                    {
                        this.InvalidCoverImagesFileListBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                this.RemoveInvalidCoverImagesButton.Enabled = true;
            }
            else
            {
                this.RemoveInvalidCoverImagesButton.Enabled = false;
            }
            if (this.InvalidCoverImagesFolderListBox.Items.Count > 0)
            {
                this.RemoveAllInvalidCoverImagesButton.Enabled = true;
            }
            else
            {
                this.RemoveAllInvalidCoverImagesButton.Enabled = false;
            }
        }

        private void OnInvalidCoverImagesFileListBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 index;

            index = this.InvalidCoverImagesFileListBox.SelectedIndex;
            ResetPictureBox(this.InvalidCoverImagesPictureBox);
            if (index != -1)
            {
                String item;

                item = (String)(this.InvalidCoverImagesFileListBox.SelectedItem);
                try
                {
                    String file;
                    String path;

                    path = Path.Combine(this.DatabaseFolderTextbox.Text, "Images");
                    file = Path.Combine(path, item);
                    if (HasPictureExtension(file))
                    {
                        this.InvalidCoverImagesPictureBox.Image = Image.FromFile(file);
                    }
                }
                catch
                {
                    this.InvalidCoverImagesPictureBox.Image = (Image)(this.InvalidCoverImagesPictureBox.ErrorImage.Clone());
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