using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Windows.Threading;
using NLog;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace WPFSimpleDCM
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private static Logger applog = LogManager.GetLogger("applogger");
        private static Logger userlog = LogManager.GetLogger("userlogger");

        private FolderBrowserDialog folderdlg;
        private FolderDiff fld = null;
        private bool? FlagShowSame;
        private bool? FlagShowDiff;
        private bool? FlagShowSrcOnly;
        private bool? FlagShowDestOnly;

        public Window1()
        {
            InitializeComponent();
            folderdlg = new FolderBrowserDialog();
            folderdlg.ShowNewFolderButton = false;
            folderdlg.Description = Properties.Resources.Window1_Window1_Select_Folder_to_Compare;

            chkSrcOnly.IsChecked = true;
            chkDestOnly.IsChecked = true;
            chkDiff.IsChecked = true;
            chkSame.IsChecked = false;

            txtSrc.Text = Properties.Settings.Default.folderSrc;
            txtDest.Text = Properties.Settings.Default.folderDest;
            ValidateCmpbtn();
        }

        private void btnBrowseSrc_Click(object sender, RoutedEventArgs e)
        {
            folderdlg.SelectedPath = txtSrc.Text;

            if( folderdlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if( Directory.Exists( folderdlg.SelectedPath) )
                {
                    txtSrc.Text = folderdlg.SelectedPath;
                    if( Properties.Settings.Default.folderSrc != txtSrc.Text)
                    {
                        Properties.Settings.Default.folderSrc = txtSrc.Text;
                        Properties.Settings.Default.Save();
                    }
                }
            }
            ValidateCmpbtn();
        }

        private void btnBrowseDest_Click(object sender, RoutedEventArgs e)
        {
            folderdlg.SelectedPath = txtDest.Text;
            if (folderdlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(folderdlg.SelectedPath))
                {
                    txtDest.Text = folderdlg.SelectedPath;
                    if (Properties.Settings.Default.folderDest != txtDest.Text)
                    {
                        Properties.Settings.Default.folderDest = txtDest.Text;
                        Properties.Settings.Default.Save();
                    }
                }
            }
            ValidateCmpbtn();
        }

        void ValidateCmpbtn()
        {
            btnCompare.IsEnabled =  Directory.Exists(txtSrc.Text) &&
            Directory.Exists(txtDest.Text);
        }
        
        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            if (btnCompare.Content.ToString() == "Compare")
            {
                fld = new FolderDiff(txtSrc.Text, txtDest.Text);
                btnCompare.Content = "Cancel";
                listView1.Items.Clear();
                fld.DoCompare(OnStatusUpdate, OnComplete);                
            }
            else
            {
                if (fld != null)
                {
                    fld.Stop();
                    
                }
                btnCompare.Content = "Compare";
                
                
            }


        }
        void AddItem1( string str)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Send,
                                        (ThreadStart) delegate
                                                          {
                                                              ListViewItem newitem = new ListViewItem();
                                                              newitem.Content = str;

                                                              //listView1.Items.Add(newitem).subitem
                                                             // lstdiffview.Items.Add(newitem);
                                                          });
        }

        private void OnComplete(string status, int percent)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Send,
                                       (ThreadStart)delegate
                                       {
           
                //OK Well Done
                DisplayResult();
                OnStatusUpdate(status, percent);
             

             btnCompare.Content = "Compare";
                                       });
            
        }

        
        void ReadDisplayOpt()
        {
            this.Dispatcher.Invoke(
                DispatcherPriority.Send,
                (ThreadStart) delegate
                {
                    FlagShowSame = chkSame.IsChecked;
                    FlagShowDiff = chkDiff.IsChecked;
                    FlagShowSrcOnly = chkSrcOnly.IsChecked;
                    FlagShowDestOnly = chkDestOnly.IsChecked;
                });
        }

        private void DisplayResult()
        {
            ReadDisplayOpt();
            listView1.Items.Clear();
            bool[] btnStages = new bool[4]{false, false, false, false};
            
            foreach (FileDiffResult result in fld.ItemsAll)
            {
                if (FlagShowSame == true && result.Status == EDiffResult.Same)
                {
                    listView1.Items.Add(result);
                    btnStages[0] = true;
                }
                if (FlagShowDiff == true && result.Status == EDiffResult.Diff)
                {
                    listView1.Items.Add(result);
                    btnStages[1] = true;
                }
                if (FlagShowSrcOnly == true && result.Status == EDiffResult.SrcOnly)
                {
                    listView1.Items.Add(result);
                    btnStages[2] = true;
                    
                }
                if (FlagShowDestOnly == true && result.Status == EDiffResult.DestOnly)
                {
                    listView1.Items.Add(result );
                    btnStages[3] = true;
                }
            }

            ResizeGridViewColumn(((GridView) listView1.View).Columns[0]);
            ResizeGridViewColumn(((GridView)listView1.View).Columns[1]);

            ValidateButs(btnStages);
        }

        private void ResizeGridViewColumn(GridViewColumn column)
        {
            if (double.IsNaN(column.Width))
            {
                column.Width = column.ActualWidth;
            }

            column.Width = double.NaN;
        }
        
        private void ValidateButs(bool []btnstages)
        {
            btnCopyRightAll.IsEnabled = btnstages[2];
            btnCopyLeftAll.IsEnabled = btnstages[3];
            btnMergeAll.IsEnabled = btnstages[1];
        }

        private void OnStatusUpdate(string status, int percent)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Send,
                                        (ThreadStart) delegate
                                                          {
                                                              st2.Text = status;
                                                              st1.Text = percent.ToString() + " %";
                                                          });
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (fld != null)
            {
                fld.Stop();

            }
        }

       

        private void btnIgnore_Click(object sender, RoutedEventArgs e)
        {
            frmIgnores dlg = new frmIgnores();
            dlg.ShowDialog();
        }

        private void CheckBox_Clicked(object sender, RoutedEventArgs e)
        {
            if( this.IsLoaded && fld != null)
            DisplayResult();
        }
        
        #region File Copy merge Tasks

        private void taskCopyFF(string src, string dest, bool isFolder)
        {
            if (isFolder) //Directory.Exists(src)) //this is folder so copy folder
                taskCopyFolder(src, dest);
            else //this is file so copy file
                taskCopyFile(src, dest);
        }

        private void taskCopyFolder(string src, string dest)
        {
            try
            {
                string[] dirs = Directory.GetDirectories(src);
                string[] files = Directory.GetFiles(src);
                if (!Directory.Exists(dest))
                {
                    Directory.CreateDirectory(dest);
                    userlog.Info("***Folder Copy : {0} -> {1}", src, dest);    
                }
                
                foreach (string dir in dirs)
                {
                    string srcfile = Path.GetFileName(dir);
                    taskCopyFolder(dir , dest+"\\"+srcfile);
                }

                foreach (string file in files)
                {
                    string srcfile = Path.GetFileName(file);
                    File.Copy(file, dest+"\\"+srcfile);
                    userlog.Info("***File Copy : {0} -> {1}", file, dest + "\\" + srcfile);    
                }
                
            }
            catch (Exception ex)
            {
                applog.Error("Fail to copy Folder/Files : {0} -> {1} {2}", src, dest, ex.Message);
            }
        }

        private void taskCopyFile(string src, string dest)
        {
            try
            {
                File.Copy(src,dest);
                userlog.Info("***File Copy : {0} -> {1}", src, dest);
            }
            catch (Exception ex)
            {
                applog.Error("**Fail to copy File {0}->{1}: {2}",src,dest, ex.Message);
            }
        }

        private void taskExtMerge(string src, string dest)
        {
            try
            {
                Exec(@"C:\Program Files\Kdiff3\kdiff3.exe",
                     string.Format("{0} {1} -o {1}", src, dest));
                userlog.Info("***File Merge : {0} -> {1}", src, dest  );
            }
            catch(Exception ex)
            {
               applog.Error("**Fail to merge files {0} -> {1}", src, dest);
            }
        }

        private void Exec(string execname , string argc)
        {
            Process proc = new Process();

            //proc.StartInfo.WorkingDirectory = @"C:\Program Files\Kdiff3\";
            proc.StartInfo.FileName = execname;
            proc.StartInfo.Arguments = argc;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = false;
            proc.StartInfo.RedirectStandardError = false;
            proc.Start();
            proc.WaitForExit();
            proc.Close();
        }

        #endregion
        
        #region Actions

        private void btnCopyRight_Click(object sender, RoutedEventArgs e)
        {
            if (listView1.SelectedIndex != -1)
            {
                foreach (var item in listView1.SelectedItems)
                {
                    FileDiffResult res =(FileDiffResult) item;
                    if( res.Status == EDiffResult.SrcOnly)//copy right is for source only
                    taskCopyFF(FileDiffResult.BaseSrc + "\\" + res.FullName,
                                 FileDiffResult.BaseDest + "\\" + res.FullName, 
                                 res.IsFolder);
                   
                }
                
            }
        }
        
        private void btnCopyLeft_Click(object sender, RoutedEventArgs e)
        {
            if (listView1.SelectedIndex != -1)
            {
                foreach (var item in listView1.SelectedItems)
                {
                    FileDiffResult res = (FileDiffResult)item;
                    if (res.Status == EDiffResult.DestOnly)//copy left is for dest only
                        taskCopyFF(FileDiffResult.BaseDest + "\\" + res.FullName,
                                   FileDiffResult.BaseSrc + "\\" + res.FullName,
                                   res.IsFolder);

                }

            }
        }

        private void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            if (listView1.SelectedIndex != -1)
            {
                foreach (var item in listView1.SelectedItems)
                {
                    FileDiffResult res = (FileDiffResult)item;
                    if (res.Status == EDiffResult.Diff && !res.IsFolder)//copy left is for dest only
                    {
                        if(MessageBoxResult.OK == MessageBox.Show("Do you want to call external Merge to merge these files :"+
                            Environment.NewLine+"Src : " +FileDiffResult.BaseSrc + "\\" + res.FullName +
                            Environment.NewLine+"Dest : " +FileDiffResult.BaseDest + "\\" + res.FullName,
                            "Confirm Manual Merge",MessageBoxButton.OKCancel))
                            taskExtMerge(FileDiffResult.BaseSrc + "\\" + res.FullName,
                                   FileDiffResult.BaseDest + "\\" + res.FullName );
                    }
                }
            }
        }

        private void btnCopyRightAll_Click(object sender, RoutedEventArgs e)
        {
            //only do visible items (don't touch not listed items)
            foreach (var item in listView1.Items)
            {
                FileDiffResult res = (FileDiffResult)item;
                if (res.Status == EDiffResult.SrcOnly)//copy right is for source only
                    taskCopyFF(FileDiffResult.BaseSrc + "\\" + res.FullName,
                                 FileDiffResult.BaseDest + "\\" + res.FullName,
                                 res.IsFolder);

            }             
        }

        private void btnCopyLeftAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in listView1.Items)
            {
                FileDiffResult res = (FileDiffResult)item;
                if (res.Status == EDiffResult.DestOnly)//copy left is for dest only
                    taskCopyFF(FileDiffResult.BaseDest + "\\" + res.FullName,
                               FileDiffResult.BaseSrc + "\\" + res.FullName,
                               res.IsFolder);

            }
        }

        private void btnMergeAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in listView1.Items)
            {
                FileDiffResult res = (FileDiffResult)item;
                if (res.Status == EDiffResult.Diff && !res.IsFolder)//copy left is for dest only
                {
                    if (MessageBoxResult.OK == MessageBox.Show("Do you want to call external Merge to merge these files :" +
                        Environment.NewLine + "Src : " + FileDiffResult.BaseSrc + "\\" + res.FullName +
                        Environment.NewLine + "Dest : " + FileDiffResult.BaseDest + "\\" + res.FullName,
                        "Confirm Manual Merge", MessageBoxButton.OKCancel))
                        taskExtMerge(FileDiffResult.BaseSrc + "\\" + res.FullName,
                               FileDiffResult.BaseDest + "\\" + res.FullName);
                }

            }
        }

        #endregion

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if( listView1.SelectedItems.Count > 0)
            {
                foreach (FileDiffResult fileDiffResult in listView1.SelectedItems)
                {
                    if( fileDiffResult.Status!= ((FileDiffResult)listView1.SelectedItem).Status)
                    {
                        btnMerge.IsEnabled = false;
                        btnCopyRight.IsEnabled = false;
                        btnCopyLeft.IsEnabled = false;
                        return;
                    }
                }
            }

            btnMerge.IsEnabled = (listView1.SelectedIndex != -1 &&
                                 ((FileDiffResult) listView1.SelectedItem).Status == EDiffResult.Diff);
            btnCopyRight.IsEnabled = (listView1.SelectedIndex != -1 &&
                                 ((FileDiffResult)listView1.SelectedItem).Status == EDiffResult.SrcOnly);
            btnCopyLeft.IsEnabled = (listView1.SelectedIndex != -1 &&
                                 ((FileDiffResult)listView1.SelectedItem).Status == EDiffResult.DestOnly);                
        }

    }
}
