namespace DoenaSoft.DVDProfiler.CleanUpPhotos
{
    partial class CopyImageForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CopyImageForm));
            this.ExistingProfilesComboBox = new System.Windows.Forms.ComboBox();
            this.ImageSourceTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.ImageTargetTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.CopyImageButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.ImageFileTextBox = new System.Windows.Forms.TextBox();
            this.ImageSourceProfileTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ExistingProfilesComboBox
            // 
            this.ExistingProfilesComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ExistingProfilesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ExistingProfilesComboBox.FormattingEnabled = true;
            this.ExistingProfilesComboBox.Location = new System.Drawing.Point(317, 64);
            this.ExistingProfilesComboBox.Name = "ExistingProfilesComboBox";
            this.ExistingProfilesComboBox.Size = new System.Drawing.Size(305, 21);
            this.ExistingProfilesComboBox.TabIndex = 2;
            // 
            // ImageSourceTextBox
            // 
            this.ImageSourceTextBox.Location = new System.Drawing.Point(148, 12);
            this.ImageSourceTextBox.Name = "ImageSourceTextBox";
            this.ImageSourceTextBox.ReadOnly = true;
            this.ImageSourceTextBox.Size = new System.Drawing.Size(163, 20);
            this.ImageSourceTextBox.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Image Source Profile:";
            // 
            // ImageTargetTextBox
            // 
            this.ImageTargetTextBox.Location = new System.Drawing.Point(148, 64);
            this.ImageTargetTextBox.Name = "ImageTargetTextBox";
            this.ImageTargetTextBox.Size = new System.Drawing.Size(163, 20);
            this.ImageTargetTextBox.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 67);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(105, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Image Target Profile:";
            // 
            // CopyImageButton
            // 
            this.CopyImageButton.Location = new System.Drawing.Point(148, 91);
            this.CopyImageButton.Name = "CopyImageButton";
            this.CopyImageButton.Size = new System.Drawing.Size(163, 23);
            this.CopyImageButton.TabIndex = 3;
            this.CopyImageButton.Text = "Copy Image";
            this.CopyImageButton.UseVisualStyleBackColor = true;
            this.CopyImageButton.Click += new System.EventHandler(this.OnCopyImageButtonClick);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 41);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Image File:";
            // 
            // ImageFileTextBox
            // 
            this.ImageFileTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ImageFileTextBox.Location = new System.Drawing.Point(148, 38);
            this.ImageFileTextBox.Name = "ImageFileTextBox";
            this.ImageFileTextBox.ReadOnly = true;
            this.ImageFileTextBox.Size = new System.Drawing.Size(474, 20);
            this.ImageFileTextBox.TabIndex = 7;
            // 
            // ImageSourceProfileTextBox
            // 
            this.ImageSourceProfileTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ImageSourceProfileTextBox.Location = new System.Drawing.Point(317, 12);
            this.ImageSourceProfileTextBox.Name = "ImageSourceProfileTextBox";
            this.ImageSourceProfileTextBox.ReadOnly = true;
            this.ImageSourceProfileTextBox.Size = new System.Drawing.Size(305, 20);
            this.ImageSourceProfileTextBox.TabIndex = 8;
            // 
            // CopyImageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(634, 126);
            this.Controls.Add(this.ImageSourceProfileTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.ImageFileTextBox);
            this.Controls.Add(this.CopyImageButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ImageTargetTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ImageSourceTextBox);
            this.Controls.Add(this.ExistingProfilesComboBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(1200, 165);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(650, 165);
            this.Name = "CopyImageForm";
            this.Text = "Copy Image";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox ExistingProfilesComboBox;
        private System.Windows.Forms.TextBox ImageSourceTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox ImageTargetTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button CopyImageButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox ImageFileTextBox;
        private System.Windows.Forms.TextBox ImageSourceProfileTextBox;
    }
}