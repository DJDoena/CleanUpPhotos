using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using DoenaSoft.DVDProfiler.DVDProfilerXML.Version400;

namespace DoenaSoft.DVDProfiler.CleanUpPhotos
{
    public partial class CopyImageForm : Form
    {
        private readonly String SourceFile;

        private readonly String BaseDir;

        private Dictionary<String, String> Profiles;

        public CopyImageForm(String baseDir
            , String sourceFile
            , String source
            , Collection collection)
        {
            String title;

            BaseDir = baseDir;
            SourceFile = sourceFile;

            this.GetProfiles(collection);

            this.InitializeComponent();

            ImageSourceTextBox.Text = source;

            if (Profiles.TryGetValue(source, out title))
            {
                ImageSourceProfileTextBox.Text = title;
            }

            ImageFileTextBox.Text = (new FileInfo(SourceFile)).Name;

            ExistingProfilesComboBox.DataSource = new BindingSource(Profiles, null);
            ExistingProfilesComboBox.DisplayMember = "Value";
            ExistingProfilesComboBox.ValueMember = "Key";
            ExistingProfilesComboBox.SelectedIndex = -1;
            ExistingProfilesComboBox.SelectedIndexChanged += this.OnExistingProfilesComboBoxSelectedIndexChanged;
        }

        private void GetProfiles(Collection collection)
        {
            if (collection.DVDList != null)
            {
                List<DVD> list;

                list = new List<DVD>(collection.DVDList);
                list.Sort((left, right) =>
                        {
                            if ((left == null) || (left.SortTitle == null))
                            {
                                if ((right == null) || (right.SortTitle == null))
                                {
                                    return (0);
                                }
                                else
                                {
                                    return (-1);
                                }
                            }
                            else if ((right == null) || (right.SortTitle == null))
                            {
                                return (1);
                            }
                            else
                            {
                                Int32 compare;

                                compare = left.SortTitle.CompareTo(right.SortTitle);

                                if (compare == 0)
                                {
                                    compare = left.ID.CompareTo(right.ID);
                                }

                                return (compare);
                            }
                        }
                    );

                Profiles = new Dictionary<String, String>(list.Count);
                foreach (DVD dvd in list)
                {
                    Profiles.Add(dvd.ID, dvd.ToString());
                }
            }
            else
            {
                Profiles = new Dictionary<String, String>(0);
            }
        }

        private void OnExistingProfilesComboBoxSelectedIndexChanged(Object sender, EventArgs e)
        {
            ImageTargetTextBox.Text = ExistingProfilesComboBox.SelectedValue.ToString();
        }

        private void OnCopyImageButtonClick(Object sender, EventArgs e)
        {
            String targetProfile;

            targetProfile = ImageTargetTextBox.Text;

            if (String.IsNullOrEmpty(targetProfile))
            {
                MessageBox.Show("No Profile ID entered!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                FileInfo fi;
                String path;
                String targetFile;

                path = Path.Combine(BaseDir, targetProfile);
                if (Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                }
                fi = new FileInfo(SourceFile);
                targetFile = Path.Combine(path, fi.Name);
                if (File.Exists(targetFile))
                {
                    File.SetAttributes(targetFile, FileAttributes.Normal | FileAttributes.Archive);
                }
                File.Copy(SourceFile, targetFile, true);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}