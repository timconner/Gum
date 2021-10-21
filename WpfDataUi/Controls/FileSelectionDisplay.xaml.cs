﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// Interaction logic for FileSelectionDisplay.xaml
    /// </summary>
    public partial class FileSelectionDisplay : UserControl, IDataUi
    {
        #region Fields

        TextBoxDisplayLogic mTextBoxLogic;

        InstanceMember mInstanceMember;

        #endregion

        #region Properties

        public InstanceMember InstanceMember
        {
            get
            {
                return mInstanceMember;
            }
            set
            {
                mTextBoxLogic.InstanceMember = value;

                bool valueChanged = mInstanceMember != value;
                if (mInstanceMember != null && valueChanged)
                {
                    mInstanceMember.PropertyChanged -= HandlePropertyChange;
                }
                mInstanceMember = value;

                if (mInstanceMember != null && valueChanged)
                {
                    mInstanceMember.PropertyChanged += HandlePropertyChange;
                }


                //if (mInstanceMember != null)
                //{
                //    mInstanceMember.DebugInformation = "TextBoxDisplay " + mInstanceMember.Name;
                //}


                Refresh();
            }
        }

        public bool SuppressSettingProperty { get; set; }

        /// <summary>
        /// Sets the filter used by the OpenFileDialog. Example: "Bitmap Font Generator Font|*.fnt"
        /// </summary>
        public string Filter
        {
            get; set;
        }

        public static string FolderRelativeTo { get; set; }

        #endregion

        #region Methods

        public FileSelectionDisplay()
        {
            InitializeComponent();


            mTextBoxLogic = new TextBoxDisplayLogic(this, TextBox);

            this.RefreshContextMenu(TextBox.ContextMenu);
        }


        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {

            SuppressSettingProperty = true;

            mTextBoxLogic.RefreshDisplay();


            this.Label.Text = InstanceMember.DisplayName;
            this.RefreshContextMenu(TextBox.ContextMenu);

            SuppressSettingProperty = false;
        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            this.TextBox.Text = valueOnInstance?.ToString();
            return ApplyValueResult.Success;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            return mTextBoxLogic.TryGetValueOnUi(out value);
        }

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                this.Refresh();

            }
        }


        private void TextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {

            var result = mTextBoxLogic.TryApplyToInstance();

            if (result == ApplyValueResult.NotSupported)
            {
                this.IsEnabled = false;
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.Filter = Filter;

            var shouldOpen = fileDialog.ShowDialog();

            if (shouldOpen.HasValue && shouldOpen.Value)
            {
                string file = fileDialog.FileName;
                this.TextBox.Text = file;
                mTextBoxLogic.TryApplyToInstance();
            }
        }

        private void ViewInExplorerClicked(object sender, RoutedEventArgs e)
        {
            var fileToOpen = this.TextBox.Text;

            if(!string.IsNullOrEmpty(fileToOpen))
            {
                if(!string.IsNullOrEmpty(FolderRelativeTo ))
                {
                    fileToOpen = RemoveDotDotSlash(
                        FolderRelativeTo + fileToOpen)
                        .Replace("/", "\\");


                }

                if(System.IO.File.Exists(fileToOpen))
                {
                    Process.Start("explorer.exe", "/select," + fileToOpen);
                }

                //if (isFile)
                {
                }
                //else
                //{
                //    Process.Start("explorer.exe", "/root," + locationToShow);
                //}

            }
        }

        private string RemoveDotDotSlash(string fileNameToFix)
        {
            if (fileNameToFix.Contains(".."))
            {
                fileNameToFix = fileNameToFix.Replace("\\", "/");

                // First let's get rid of any ..'s that are in the middle
                // for example:
                //
                // "content/zones/area1/../../background/outdoorsanim/outdoorsanim.achx"
                //
                // would become
                // 
                // "content/background/outdoorsanim/outdoorsanim.achx"

                int indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");

                bool shouldLoop = indexOfNextDotDotSlash > 0;

                while (shouldLoop)
                {
                    int indexOfPreviousDirectory = fileNameToFix.LastIndexOf('/', indexOfNextDotDotSlash - 2, indexOfNextDotDotSlash - 2);

                    fileNameToFix = fileNameToFix.Remove(indexOfPreviousDirectory + 1, indexOfNextDotDotSlash - indexOfPreviousDirectory + 2);

                    indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");

                    shouldLoop = indexOfNextDotDotSlash > 0;
                }
            }

            return fileNameToFix.Replace("\\", "/");
        }

        #endregion

    }
}
