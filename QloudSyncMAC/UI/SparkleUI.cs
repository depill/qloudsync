//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

    
using System;
using System.Drawing;
using System.IO;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
//using MonoMac.Growl;
using QloudSyncCore;

namespace GreenQloud {

    public class SparkleUI : AppDelegate, ApplicationUI {

        public IconController StatusIcon;

        public SparkleSetup Setup;
        public AboutWindow About;
        public static NSFont Font = NSFontManager.SharedFontManager.FontWithFamily (
			"Lucida Grande", NSFontTraitMask.Condensed, 0, 13);
		
        public static NSFont BoldFont = NSFontManager.SharedFontManager.FontWithFamily (
			"Lucida Grande", NSFontTraitMask.Bold, 0, 13);
		

        public SparkleUI ()
        {
            using (var a = new NSAutoreleasePool ())
            {
                NSApplication.SharedApplication.ApplicationIconImage = NSImage.ImageNamed ("qloudsync-app.icns");
                Setup      = new SparkleSetup ();
                About      = new AboutWindow ();
                StatusIcon = new IconController ();
            }
        }
    

        public void SetFolderIcon ()
        {
            using (var a = new NSAutoreleasePool ())
            {
                NSImage folder_icon = NSImage.ImageNamed ("qloudsync-folder.icns");
                //NSWorkspace.SharedWorkspace.SetIconforFile (folder_icon, RuntimeSettings.HomePath, 0);
            }
        }


        public void Run ()
        {
            using (var a = new NSAutoreleasePool ())
            {
                Program.Controller.UIHasLoaded ();
                NSApplication.Main (new string [0]);
            }
        }


        public void UpdateDockIconVisibility ()
        {
            if (Setup.IsVisible ||  About.IsVisible)
                ShowDockIcon ();
            else
                HideDockIcon ();
        }


        private void HideDockIcon ()
        {
            // Currently not supported, here for completeness sake (see Apple's docs)
            NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Prohibited;
        }


        private void ShowDockIcon ()
        {
            NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Regular;
        }
    }


    public partial class AppDelegate : NSApplicationDelegate {

        public override void WillBecomeActive (NSNotification notification)
        {
            if (NSApplication.SharedApplication.DockTile.BadgeLabel != null) {
                Program.Controller.ShowEventLogWindow ();
                NSApplication.SharedApplication.DockTile.BadgeLabel = null;
            }
        }


        public override void WillTerminate (NSNotification notification)
        {
            Program.Controller.Quit ();
        }
    }
}
