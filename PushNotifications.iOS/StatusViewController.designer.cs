// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace PushNotifications.iOS
{
    [Register ("StatusViewController")]
    partial class StatusViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton btnRegisterDevice { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITableView tableView { get; set; }

        [Action ("OnRegisterUpdateClicked:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void OnRegisterUpdateClicked (UIKit.UIButton sender);

        [Action ("OnUnregisterClicked:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void OnUnregisterClicked (UIKit.UIButton sender);

        [Action ("OnSendClicked:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void OnSendClicked (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (btnRegisterDevice != null) {
                btnRegisterDevice.Dispose ();
                btnRegisterDevice = null;
            }

            if (tableView != null) {
                tableView.Dispose ();
                tableView = null;
            }
        }
    }
}