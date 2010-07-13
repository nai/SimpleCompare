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

namespace WPFSimpleDCM
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
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
            if (percent == 100)
            {
                //OK Well Done
                DisplayResult();
            }

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
            foreach (FileDiffResult result in fld.ItemsAll)
            {
                if (FlagShowSame == true && result.Status == EDiffResult.Same)
                {
                    listView1.Items.Add(result);
                    
                }
                if (FlagShowDiff == true && result.Status == EDiffResult.Diff)
                {
                    listView1.Items.Add(result);
                    
                }
                if (FlagShowSrcOnly == true && result.Status == EDiffResult.SrcOnly)
                {
                    listView1.Items.Add(result);

                    
                }
                if (FlagShowDestOnly == true && result.Status == EDiffResult.DestOnly)
                {
                    listView1.Items.Add(result );
                    
                }
            }
             
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

        private void btnCopyRight_Click(object sender, RoutedEventArgs e)
        {
            if( listView1.SelectedIndex != -1)
            {
                FileDiffResult res =(FileDiffResult) listView1.SelectedItem;
                File.Copy(FileDiffResult.BaseSrc + "\\" + res.FullName, FileDiffResult.BaseDest + "\\" + res.FullName);
            }
        }

        private static Logger applog = LogManager.GetLogger("applogger");
        private static Logger userlog = LogManager.GetLogger("userlogger");
        private void btnCopyLeft_Click(object sender, RoutedEventArgs e)
        {
            applog.Error("a hello");
            userlog.Info("u hello");
            
        }

       
       

         
    }
}
