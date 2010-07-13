using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
    internal struct FileDiffResult
    {
        private string fullName;
        public string Name
        {
            get { return    System.IO.Path.GetFileName(fullName);  }
            //set { name = value; }
        }
        private static string _baseSrc;
        private static string _baseDest;
        public string LocalPath
        {
            get
            {
                //string path = "";
                //if (_status == EDiffResult.DestOnly)
                //    path= FolderDiff.GetLocalPath(name, BaseDest);
                
                //path= FolderDiff.GetLocalPath(name, BaseSrc);
                return  System.IO.Path.GetDirectoryName(fullName);
            }
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

        public static string BaseSrc
        {
            get { return _baseSrc; }
            set { _baseSrc = value; }
        }

        public static string BaseDest
        {
            get { return _baseDest; }
            set { _baseDest = value; }
        }

        public EDiffResult Status
        {
            get { return _status; }
            set { _status = value; }
        }

        private EDiffResult _status ;
        public FileDiffResult(string fullName, EDiffResult status)
        {
            this.fullName = fullName;
            _status = status;
        }
        public FileDiffResult(string fullName )
        {
            this.fullName = fullName;
            _status = EDiffResult.Same;
        }

        public string FullName
        {
            get { return fullName; }
            set{ fullName = value;}
        }
        
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
            if (flagRunThread)
            {
                Stopwatch st = new Stopwatch();
                st.Reset();
                totalCnt = GetFolderCount(foldersrc);
                totalCnt = Math.Max(totalCnt, GetFolderCount(folderdest));
                st.Stop();
                applog.Trace("File Count time : {0} ms" , st.ElapsedMilliseconds );
                ProcessFolder(ref fcount, foldersrc, folderdest);
            }

            if (FuncOnComplete != null)
                FuncOnComplete("Done", 100);
        }

        internal int GetFolderCount(string folder)
        {
            int count = 0;
            count += Directory.GetFiles(folder).Count();
            foreach (string fdir in Directory.GetDirectories(folder))
                count += GetFolderCount(fdir);
            return count;
        }

        private void ProcessFolder(ref int fcount,string srcpath, string srcdest )
        {
            string[] srcfiles = Directory.GetFiles(srcpath);
            string[] destfiles = Directory.GetFiles(srcdest);
            string[] srcdirs = Directory.GetDirectories(srcpath);
            string[] destdirs = Directory.GetDirectories(srcdest);
            foreach (string srcdir in srcdirs)
            {
                string src1 = GetLocalPath(foldersrc, srcdir);
                bool found = FindSrcInDest(srcdir, foldersrc, folderdest, destdirs);
                if(found)
                    ProcessFolder(ref fcount, srcdir, 
                        folderdest + "\\"+ src1);
            }


            foreach (string srcfile in srcfiles)
            {
                    
                string curFile = GetLocalPath(foldersrc, srcfile);
                FileDiffResult CurResult = new FileDiffResult(curFile);
                    
                if (FuncOnStatus != null)
                {
                    FuncOnStatus("Now " +srcfile, fcount++);
//                        Thread.Sleep(200);
                }

                if (FindSrcInDest(srcfile, foldersrc, folderdest, 
                                  destfiles))
                {
                    Stopwatch st  = new Stopwatch();
                    st.Reset();
                    bool dd =  FileDiff.FileEquals( srcfile,
                                                    folderdest + "\\" + curFile);
                    st.Stop();
                    Console.WriteLine( "Time : " + st.ElapsedMilliseconds.ToString());

                    if (!FileDiff.FileEquals( srcfile,
                                              folderdest + "\\" + curFile))
                         
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
                     
                if (!FindSrcInDest(destfile, folderdest, foldersrc ,
                                   srcfiles))
                    ItemsAll.Add(new FileDiffResult(curFile, EDiffResult.DestOnly)); ;
                if (!flagRunThread) break;
            }
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
