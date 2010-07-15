using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFSimpleDCM
{
    /// <summary>
    /// Interaction logic for frmIgnores.xaml
    /// </summary>
    public partial class frmIgnores : Window
    {
        public frmIgnores()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string folder in Properties.Settings.Default.IgnoreFolders)
            {
                txtFolders.AppendText(folder + Environment.NewLine);
            }

            foreach (string file in Properties.Settings.Default.IgnoreFiles)
            {
                txtFiles.AppendText(file + Environment.NewLine);
            } 
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.IgnoreFolders = new StringCollection();
            for(int i = 0;i < txtFolders.LineCount ; i++)
            {
                string str = txtFolders.GetLineText(i).Replace("\r", "").Replace("\n", "");
                if( !string.IsNullOrEmpty(str))
                Properties.Settings.Default.IgnoreFolders.Add(str);
            }

            Properties.Settings.Default.IgnoreFiles = new StringCollection();
            for (int i = 0; i < txtFiles.LineCount; i++)
            {
                string str = txtFiles.GetLineText(i).Replace("\r", "").Replace("\n", "");
                if (!string.IsNullOrEmpty(str))
                Properties.Settings.Default.IgnoreFiles.Add(str);
            }
            Properties.Settings.Default.Save();

            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
