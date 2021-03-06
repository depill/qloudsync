﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using QloudSyncCore;
using GreenQloud.Model;
using GreenQloud.Persistence.SQLite;
using GreenQloud.UI.Setup;
using System.Threading;
using System.Reflection;

namespace GreenQloud.UI
{
    public class UIManager : Form, ApplicationUI
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        public Login LoginWindow;
        public Ready readyWindow;
        public ConfFolders confFoldersWindow;
        public AboutWindow About;
        private bool isLoged;
        private static UIManager instance;
        private bool isPaused = false;

        public static UIManager GetInstance(){
            if(instance == null)
                instance = new UIManager();

            return instance;
        }

        private UIManager()
        {
            this.AddToSystemTray();
            this.LoginWindow = new Setup.Login(this);
            this.readyWindow = new Ready();
            Program.Controller.ShowSetupWindowEvent += delegate(GreenQloud.AbstractApplicationController.PageType page_type)
            {
                if (page_type == GreenQloud.AbstractApplicationController.PageType.Login) {
                    this.LoginWindow.ShowDialog();
                }
                else if (page_type == GreenQloud.AbstractApplicationController.PageType.ConfigureFolders)
                {
                    this.confFoldersWindow = new ConfFolders();
                    this.confFoldersWindow.ShowDialog();
                }
            };
 

            this.LoginWindow.OnLoginDone += (() =>
            {
                this.isLoged = true;
                this.LoginWindow.Done();
                this.About = new AboutWindow();
                this.confFoldersWindow = new ConfFolders();
                Program.Controller.ShowAboutWindowEvent += (() => this.About.ShowDialog());
               
                this.confFoldersWindow.ShowDialog();
            });
            this.LoginWindow.FormClosed += ((sender, args) =>
            {
                if (this.isLoged)
                {
                    Application.DoEvents();
                }
            });
        }

        public void Run() {
            Program.Controller.UIHasLoaded();
        }

        private void AddToSystemTray()
        {
            this.trayMenu = new ContextMenuStrip();
            this.trayIcon = new NotifyIcon();
            this.trayIcon.Text = GlobalSettings.ApplicationName;
            this.trayIcon.Icon = Icon.FromHandle(((Bitmap)Icons.ResourceManager.GetObject("process_syncing_idle_active")).GetHicon());
            this.trayIcon.ContextMenuStrip = trayMenu;
            this.trayIcon.Visible = true;
        }

        public void BuildMenu()
        {
            ToolStripMenuItem savings = new ToolStripMenuItem("");
            savings.Visible = false;
            this.trayMenu.Items.Add(savings);
            ToolStripMenuItem sqFolder = new ToolStripMenuItem("StorageQloud Folder", Icons.qloudsync_folder , OpenStorageQloudFolder);
            this.trayMenu.Items.Add(sqFolder);
            ToolStripMenuItem shareview = new ToolStripMenuItem("Share/View Online...", Icons.process_syncing, OpenStorageQloudWebsite);
            this.trayMenu.Items.Add(shareview);
            this.trayMenu.Items.Add("-", null);

            ToolStripMenuItem recentlyChanged = new ToolStripMenuItem("Recently Changed");
            recentlyChanged.Enabled = false;
            this.trayMenu.Items.Add(recentlyChanged);
            
            //Dont remove this separators
            ToolStripSeparator recentlyChangedSeparator = new ToolStripSeparator();
            this.trayMenu.Items.Add(recentlyChangedSeparator);
            //place to load recently changes
            ToolStripSeparator recentlyChangedFinalSeparator = new ToolStripSeparator();
            this.trayMenu.Items.Add(recentlyChangedFinalSeparator);
            
            this.trayMenu.Items.Add("Help Center", null, OpenStorageQloudHelpCenter);
            this.trayMenu.Items.Add("About QloudSync", null, ShowAboutWindow);
            this.trayMenu.Items.Add("Pause Sync", null, PauseSyncronizers);
            this.trayMenu.Items.Add("Check for Updates", null);
            this.trayMenu.Items.Add("-", null);
            this.trayMenu.Items.Add("Quit", null, OnExit);

            this.trayIcon.MouseClick += (sender, args) =>
            {
                if (((MouseEventArgs)args).Button == System.Windows.Forms.MouseButtons.Left)
                {
                    MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                    mi.Invoke(this.trayIcon, null);
                }
            };

            this.trayMenu.Opening += (sender, args) => {
                LoadExtraItems(recentlyChangedSeparator, recentlyChangedFinalSeparator, savings);
            };
        }

        private string savingstext = "";
        private void LoadExtraItems(ToolStripSeparator separator,  ToolStripSeparator finalSeparator, ToolStripMenuItem savings)
        {

            //First load the recently changes
            int begin = this.trayMenu.Items.IndexOf(separator);
            int end = this.trayMenu.Items.IndexOf(finalSeparator);
            while(begin+1 < end) {
                this.trayMenu.Items.RemoveAt(begin + 1);
                begin = this.trayMenu.Items.IndexOf(separator);
                end = this.trayMenu.Items.IndexOf(finalSeparator);
            }

            if (Program.Controller.DatabaseLoaded())
            {
                SQLiteEventDAO eventDao = new SQLiteEventDAO();
                List<Event> events = eventDao.LastEvents;
                
                foreach (Event e in events)
                {
                    end = this.trayMenu.Items.IndexOf(finalSeparator);

                   ToolStripMenuItem current = new ToolStripMenuItem();
                    current.Text = e.ItemName;
                    
                    current.Image = Icons.folder_default;
                    if (e.ItemType == ItemType.IMAGE)
                        current.Image = Icons.folder_pics;
                    if (e.ItemType == ItemType.TEXT)
                        current.Image = Icons.folder_docs;
                    if (e.ItemType == ItemType.VIDEO)
                        current.Image = Icons.folder_movies;
                    if (e.ItemType == ItemType.AUDIO)
                        current.Image = Icons.folder_music;



                    current.Click += (sender, args) =>
                    {
                        Program.Controller.OpenFolder(e.ItemLocalFolderPath);
                    };
                    this.trayMenu.Items.Insert(end, current);
                }

                new Thread(() =>
                {
                    savingstext = GetSavings();
                }).Start();

                if (savingstext.Length > 0)
                {
                    savings.Text = savingstext;
                    savings.Visible = true;
                }
            }

        }
        public void ReadyToSync()
        {
            new Thread(delegate()
            {
                this.readyWindow.ShowDialog();
            }).Start();
            Program.Controller.SyncStart();
        }

        private string GetSavings()
        {
            try
            {
                CO2Savings saving = Statistics.EarlyCO2Savings;
                string spent = Statistics.TotalUsedSpace.Spent;
                return string.Format(spent + " used | " + "{0} CO₂ saved", saving.Saved);
            } catch {
                return ""; 
            }
        }

      



        public void OpenStorageQloudFolder(Object sender, EventArgs e)
        {
            Program.Controller.OpenSparkleShareFolder();
        }

        public void OpenStorageQloudRegistration(Object sender, EventArgs e)
        {
            Program.Controller.OpenWebsite("https://my.greenqloud.com/registration/qloudsync");
        }
        public void OpenStorageQloudWebsite(Object sender, EventArgs e){
            Program.Controller.OpenStorageQloudWebSite();
        }

        public void PauseSyncronizers(Object sender, EventArgs e)
        {
            Program.Controller.PauseSync();
        }

        public void OpenStorageQloudHelpCenter(Object sender, EventArgs e)
        {
            Program.Controller.OpenWebsite("http://support.greenqloud.com");
        }

        public void ShowAboutWindow(Object sender, EventArgs e)
        {
            Program.Controller.ShowAboutWindow();
        }

        protected override void OnLoad(EventArgs e)
        {
            this.Visible = false; // Hide form window.
            this.ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        public void OnPause(Object sender, EventArgs e)
        {
            Program.Controller.OnPaused += delegate()
            {
                //TODO Call OnPaused and change text
                isPaused = true;
            };
        }

        public void OnExit(Object sender, EventArgs e)
        {
            this.Dispose();
            //Program.Controller.StopSynchronizers();
            Program.Controller.Quit();
        }

        protected override void Dispose(bool isDisposing)
        {  
            if (isDisposing)
            {
                trayIcon.Dispose();
            }
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    if (!this.IsDisposed)
                        base.Dispose(isDisposing);
                }));
            }
        }

        internal void OnIdle()
        {
            this.trayIcon.Icon = Icon.FromHandle(((Bitmap)Icons.ResourceManager.GetObject("process_syncing_idle_active")).GetHicon()); 
        }
        internal void OnError()
        {
            this.trayIcon.Icon = Icon.FromHandle(((Bitmap)Icons.ResourceManager.GetObject("process_syncing_error_active")).GetHicon());
        }
        internal void OnSyncing()
        {
            this.trayIcon.Icon = Icon.FromHandle(((Bitmap)Icons.ResourceManager.GetObject("process_syncing_active")).GetHicon());
        }
        internal void OnPaused()
        {
            this.trayIcon.Icon = Icon.FromHandle(((Bitmap)Icons.ResourceManager.GetObject("process_pause_active")).GetHicon());
        }
    }
}