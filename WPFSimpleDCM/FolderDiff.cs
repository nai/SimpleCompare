//#define TRACE_DETAIL

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using NLog;


namespace WPFSimpleDCM
{
    internal enum EDiffResult
    {
        Same,
        Diff,
        SrcOnly,
        DestOnly
    } ;
    internal class FileDiffResult
    {
        public static string BaseSrc { get; set; }
        public static string BaseDest { get; set; }

        public EDiffResult Status { get; set; }
        public string FullName { get; set; }
        public bool IsFolder { get; set; }

        public string Name
        {
            get { return System.IO.Path.GetFileName(FullName);  }
        }

        public string FullPath
        {
            get
            {
                if (Status == EDiffResult.DestOnly)
                    return BaseDest + "\\" + FullName;

                return BaseSrc + "\\" + FullName;
            }
        }
        public string LocalPath
        {
            get
            {
                //string path = "";
                //if (_status == EDiffResult.DestOnly)
                //    path= FolderDiff.GetLocalPath(name, BaseDest);
                
                //path= FolderDiff.GetLocalPath(name, BaseSrc);
                return  System.IO.Path.GetDirectoryName(FullName);
            }
        }

        public override string ToString()
        {
            return string.Format("{0,-9}{1}", Status, FullName);
        }

        public FileDiffResult(string fullName, EDiffResult status)
        {
            this.FullName = fullName;
            Status = status;
            IsFolder = false;
        }
        public FileDiffResult(string fullName)
        {
            this.FullName = fullName;
            Status = EDiffResult.Same;
            IsFolder = false;
        }

        //public string Path
        //{
        //    get
        //    {
        //        string path = "";
        //        if (_status == EDiffResult.DestOnly)
        //            path = FolderDiff.GetLocalPath(name, BaseDest);

        //        path = FolderDiff.GetLocalPath(name, BaseSrc);
        //        return path;
        //    }
        //}       
    }
    class FolderDiff 
    {
        #region Declare
        private static Logger applog = LogManager.GetLogger("applogger");
        private static Logger userlog = LogManager.GetLogger("userlogger");


        public List<FileDiffResult> ItemsAll = new List<FileDiffResult>();
        

        private string foldersrc, folderdest;

        public delegate void DELGSendStatus(string status, int percent);

        private DELGSendStatus FuncOnStatus, FuncOnComplete;

        private Thread thDoWork;
        private volatile bool flagRunThread;
        #endregion

        public FolderDiff(string src, string dest)
        {
            foldersrc = src;
            folderdest = dest;
            FileDiffResult.BaseSrc = src;
            FileDiffResult.BaseDest = dest;
        }
        
        public void DoCompare(DELGSendStatus onStatus,DELGSendStatus onComplete)
        {
            FuncOnStatus = onStatus;
            FuncOnComplete = onComplete;
            flagRunThread = true;   
            thDoWork = new Thread(doWork);
            thDoWork.IsBackground = true;
            thDoWork.Start();
            
        }

        public void Stop()
        {
            flagRunThread = false;
            Thread.Sleep(100);
        }
        private int totalCnt ;
        private void doWork()
        {
            int fcount = 1;
            double ttk = 0;
            if (flagRunThread)
            {
                userlog.Info("-------------- Started Compare Job --------------");
                Stopwatch stall = Stopwatch.StartNew();
                Stopwatch st = Stopwatch.StartNew();
                
                totalCnt = GetFolderCount(foldersrc);
                //totalCnt = Math.Max(totalCnt, GetFolderCount(folderdest));
                st.Stop();
                applog.Trace("File Count time : {0} ms" , st.ElapsedMilliseconds );
                userlog.Info("Total File count excluding files under excluded folder : {0}", totalCnt);

                ProcessFolder(ref fcount, foldersrc, folderdest);
                stall.Stop();
                
                StringBuilder strb = new StringBuilder();
                strb.Append(Environment.NewLine);
                foreach (FileDiffResult fdr in ItemsAll)
                    if( fdr.Status != EDiffResult.Same) strb.Append(fdr.ToString()+Environment.NewLine);
                strb.Append(Environment.NewLine);
 
                userlog.Info(strb.ToString());

                ttk = stall.ElapsedMilliseconds/1000.0;
                userlog.Info("============== Compare Job Ended Total Time Taken {0} sec", ttk);
                
            }

            if (FuncOnComplete != null)
                if (flagRunThread)
                    FuncOnComplete("Completed " + ttk + " sec.", 100);
                else
                    FuncOnComplete("Aborted " + ttk + " sec.",  100 * fcount/ totalCnt);
        }

        internal int GetFolderCount(string folder)
        {
            //return Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories).Count();
            int count = 0;
            count += Directory.GetFiles(folder).Count();
            foreach (string fdir in Directory.GetDirectories(folder))
                if( !CanIgnore(Path.GetFileName( fdir), Properties.Settings.Default.IgnoreFolders))
                count += GetFolderCount(fdir);
            return count;
        }

        Stopwatch stw = new Stopwatch();
        private void ProcessFolder(ref int fcount,string srcpath, string srcdest )
        {
            string[] srcfiles = Directory.GetFiles(srcpath);
            string[] destfiles = Directory.GetFiles(srcdest);
            string[] srcdirs = Directory.GetDirectories(srcpath);
            string[] destdirs = Directory.GetDirectories(srcdest);
            foreach (string srcdir in srcdirs)
            {
                string src1 = GetLocalPath(foldersrc, srcdir);
                bool canignore = CanIgnore(Path.GetFileName(src1), Properties.Settings.Default.IgnoreFolders);
                if (!canignore)
                {
#if TRACE_DETAIL
                    applog.Trace("---Processing Folder : {0}", srcdir);
#endif
                    bool found = FindSrcInDest(srcdir, foldersrc, folderdest, destdirs);
                    if (found)
                        ProcessFolder(ref fcount, srcdir,
                                      folderdest + "\\" + src1);
                    else
                    {
                        //this folder is not in dest yet
                        FileDiffResult CurResult = new FileDiffResult(src1);
                        CurResult.Status = EDiffResult.SrcOnly;
                        CurResult.IsFolder = true;
                        ItemsAll.Add(CurResult);
                    }
                }
#if TRACE_DETAIL
                else applog.Trace("-Ignored Folder : {0}", srcdir);
#endif
                if(!flagRunThread) return;
            }

            //To search Dest Only Folders
            foreach (string destdir in destdirs)
            {

                string src1 = GetLocalPath(folderdest, destdir);
                bool canignore = CanIgnore(Path.GetFileName(src1), Properties.Settings.Default.IgnoreFolders);
                if (!canignore)
                {
                    bool found = FindSrcInDest(destdir, folderdest, foldersrc,  srcdirs);
                    if (!found)
                    {
                        //this folder is not in dest yet
                        FileDiffResult CurResult = new FileDiffResult(src1);
                        CurResult.Status = EDiffResult.DestOnly;
                        CurResult.IsFolder = true;
                        ItemsAll.Add(CurResult);
                    }
                }
#if TRACE_DETAIL
                else applog.Trace("-Ignored Dest Folder : {0}", destdir);
#endif
                if (!flagRunThread) return;
            }

            

            foreach (string srcfile in srcfiles)
            {
                    
                string curFile = GetLocalPath(foldersrc, srcfile);

                FileDiffResult CurResult = new FileDiffResult(curFile);

                bool canignore = CanIgnore(CurResult.Name, Properties.Settings.Default.IgnoreFiles);
                if (canignore)
                {
#if TRACE_DETAIL
                    applog.Trace("-Ignored File : {0}", curFile);
#endif
                    continue;
                }

                if (FuncOnStatus != null)
                {
                    fcount++;
                    FuncOnStatus("Now [" + fcount + "]" + srcfile, 100 *  fcount / totalCnt);
//                        Thread.Sleep(200);
                }

                if (FindSrcInDest(srcfile, foldersrc, folderdest, 
                                  destfiles))
                {
#if TRACE_DETAIL
                    stw.Reset(); stw.Start();
#endif
                    bool isdiff =  FileDiff.FileEquals( srcfile,
                                                    folderdest + "\\" + curFile);
#if TRACE_DETAIL
                    stw.Stop();
                    applog.Trace("Diff [{2}] {0}  Time : {1}", curFile, stw.ElapsedMilliseconds, isdiff?"#":"=");
#endif

                    if (!isdiff)
                        CurResult.Status= EDiffResult.Diff; 
                        
                }
                else
                    CurResult.Status=EDiffResult.SrcOnly; 

                ItemsAll.Add(CurResult);
                if(!flagRunThread) break;
            }

            foreach (string destfile in destfiles)
            {
                string curFile = GetLocalPath(folderdest, destfile);
                FileDiffResult CurResult = new FileDiffResult(curFile, EDiffResult.DestOnly);  
                bool canignore = CanIgnore(CurResult.Name, Properties.Settings.Default.IgnoreFiles);
                if( canignore) continue;
                if (!FindSrcInDest(destfile, folderdest, foldersrc, srcfiles))
                    ItemsAll.Add( CurResult); 
                if (!flagRunThread) break;
            }
        }

        private bool CanIgnore(string str, StringCollection patterns)
        {
            foreach (string pattern in patterns)
            {
                if( pattern.Contains("*"))
                {
                    string edges = pattern.Replace("*", "");
                    if( pattern.StartsWith("*"))
                    {
                        if( str.EndsWith(edges,true,CultureInfo.CurrentCulture) ) 
                        return true;
                    }
                    else if (pattern.EndsWith("*"))
                    {
                        if( str.StartsWith(edges,true,CultureInfo.CurrentCulture) ) 
                        return true;
                        
                    }
                    else
                    {
                        string[] strs = pattern.Split(new char[] {'*'});
                        if( strs.Length ==2 )
                        {
                            if( str.StartsWith(strs[0]) && 
                                str.EndsWith(strs[1]) &&
                                str.Length >= strs[0].Length + strs[1].Length)
                                return true;
                        }
                        else throw new Exception("* pattern Too complex to process");
                        
                    }
                }
                else
                {
                    if( str.Equals(pattern,StringComparison.CurrentCultureIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        internal static string GetLocalPath(string basestr, string path)
        {
            int pos = path.IndexOf(basestr) + basestr.Length;
            return path.Substring(pos).TrimStart(new char[] {'\\'});
        }

        private bool FindSrcInDest(string srcfile, string srcbase, string destbase, string[] destfiles)
        {
            string srcloc = GetLocalPath(srcbase, srcfile);
            foreach (string destfile in destfiles)
            {
                if( srcloc.Equals( GetLocalPath(destbase, destfile),
                    StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
            
        }

         
    }
}
