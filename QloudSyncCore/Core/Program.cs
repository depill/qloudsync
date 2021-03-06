using System;
using System.Threading;
using System.IO;
using GreenQloud.Repository;
using System.Linq;
using QloudSyncCore;
using System.Diagnostics;
using System.Web.Util;

namespace GreenQloud.Core {

    public class Program {

        public static ApplicationController Controller;
        public static ApplicationUI UI;
        #if !__MonoCS__
        [STAThread]
        #endif
        public static void Run (ApplicationController controller, ApplicationUI ui)
        {
            HttpEncoder.Current = HttpEncoder.Default;
            Controller = controller;
            UI = ui;
            try {
                if (PriorProcess() != null)
                {
                    throw new AbortedOperationException("Another instance of the app is already running.");
                }

                Controller.Initialize ();
                try{
                    UI.Run ();
                }catch (AbortedOperationException){
                    Logger.LogInfo ("Init", "Operation aborted. Sending a QloudSync Kill.");
                    PriorProcess().Kill();
                }
            } catch (Exception e){
                Logger.LogInfo ("Init", e);
                Console.WriteLine (e.StackTrace);
                Environment.Exit (-1);
            }
            #if !__MonoCS__
            // Suppress assertion messages in debug mode
            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers ();
            #endif
        }


        public static Process PriorProcess()
            // Returns a System.Diagnostics.Process pointing to
            // a pre-existing process with the same name as the
            // current one, if any; or null if the current process
            // is unique.
        {
            Process curr = Process.GetCurrentProcess();
            Process[] procs = Process.GetProcessesByName(curr.ProcessName);
            foreach (Process p in procs)
            {
                if ((p.Id != curr.Id) &&
                    (p.MainModule.FileName == curr.MainModule.FileName))
                    return p;
            }
            return null;
        }

    }
}
