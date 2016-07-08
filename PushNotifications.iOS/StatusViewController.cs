using Foundation;
using System;
using UIKit;
using Plugin.Settings;
using PushNotificationsClient;
using PushNotificationsClientServerShared;
using System.Collections.Generic;
using System.Threading;

namespace PushNotifications.iOS
{
    public partial class StatusViewController : UIViewController
	{
		#region Store Device Token
		const string SETTING_INSTALLATION_ID = "InstallationId";

		static void SaveInstallationId(string token)
		{
			if(string.IsNullOrWhiteSpace(token))
			{
				CrossSettings.Current.Remove(SETTING_INSTALLATION_ID);
				return;
			}
			CrossSettings.Current.AddOrUpdateValue(SETTING_INSTALLATION_ID, token);
		}

		static string GetSavedInstallationId() => CrossSettings.Current.GetValueOrDefault<string>(SETTING_INSTALLATION_ID, null);
		#endregion

        public StatusViewController (IntPtr handle) : base (handle)
        {
        }

		PushNotificationManager pushManager = new PushNotificationManager();
		NSObject registeredForRemoteNotifToken;

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			this.tableView.WeakDataSource = this;
		}

		List<Tuple<string, string>> eventData = new List<Tuple<string, string>>();

		[Export("tableView:cellForRowAtIndexPath:")]
		public UITableViewCell GetCell(UITableView tv, NSIndexPath indexPath)
		{
			var cell = tv.DequeueReusableCell("EventCell") ?? new UITableViewCell(UITableViewCellStyle.Subtitle, "EventCell");
			cell.TextLabel.Text = this.eventData[indexPath.Row].Item1;
			cell.DetailTextLabel.Text = this.eventData[indexPath.Row].Item2;
			return cell;
		}

		[Export("tableView:numberOfRowsInSection:")]
		public nint NumberOfRows(UITableView tv, nint section) => this.eventData.Count;

		void LogEvent(string title, string message)
		{
			this.eventData.Add(new Tuple<string, string>(title, message));
			this.tableView.InsertRows(new NSIndexPath[] { NSIndexPath.FromRowSection(this.eventData.Count - 1, 0) }, UITableViewRowAnimation.Automatic);
		}


		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
			this.btnRegisterDevice.Enabled = false;
			this.LogEvent("Waiting", "Apple's server should call us back...");
			this.registeredForRemoteNotifToken = NSNotificationCenter.DefaultCenter.AddObserver(AppDelegate.RegisteredForRemoteNotificationsMessage, notif => this.OnDeviceRegistered((NSData)notif.Object));
		}

		void OnDeviceRegistered (NSData token)
		{
			this.btnRegisterDevice.Enabled = true;
			this.deviceToken = token;
			this.LogEvent("Received token", token.ToString());
		}

		NSData deviceToken;


		public override void ViewDidDisappear (bool animated)
		{
			this.registeredForRemoteNotifToken.Dispose();
			this.registeredForRemoteNotifToken = null;
			base.ViewDidDisappear (animated);
		}

		async partial void OnRegisterUpdateClicked (UIButton sender)
		{
			// Azure SDK uses internally something hacky (https://github.com/Azure/azure-mobile-services/blob/4c3556d3fd3c89cacf9645b936ed495ec882eb02/sdk/Managed/src/Microsoft.WindowsAzure.MobileServices.iOS/Push/ApnsRegistration.cs#L72):
			// Surprisingly, this seems to work. The token must be a string of hexadecimal numbers, otherwise registering with Azure will fail.
			var newDeviceToken = this.deviceToken.Description.Trim('<','>').Replace(" ", string.Empty).ToUpperInvariant();

			this.LogEvent("Registering", $"Token: {newDeviceToken}");

			// Every device is assigned a unique installation ID by the backend.
			// If the installation ID is null, a new installation will be created, otherwise an existing will be updated.
			var uniqueDeviceId = GetSavedInstallationId();
			var newInstallationId = await this.pushManager.RegisterOrUpdateDeviceAsync(new DeviceInformation
			{
				Id = uniqueDeviceId,
				DeviceToken = newDeviceToken,
				Platform = Platform.iOS,
				DeviceName = UIDevice.CurrentDevice.Name
			});

			if(newInstallationId == null)
			{
				this.LogEvent("Registration failed", "");
			}
			else
			{
				this.LogEvent("Registration complete", $"InstalationID: {newInstallationId}");
				// Remember new installation ID.
				SaveInstallationId(newInstallationId);
			}
		}

		async partial void OnUnregisterClicked (UIButton sender)
		{
			var installationId = GetSavedInstallationId();
			this.LogEvent("Unregistering", $"ID: {installationId}");
			var result = await this.pushManager.UnregisterDeviceAsync(installationId);

			if(result == null)
			{
				this.LogEvent("Unregistering failed", $"Tried ID: {installationId}");
			}
			else
			{
				this.LogEvent("Unregistering suceeded", $"Unregistered: {result.DeviceName}");
			}
			SaveInstallationId(null);
		}

		async partial void OnSendClicked (UIButton sender)
		{
			var installationId = GetSavedInstallationId();
			this.LogEvent("Sending", $"Sender ID: {installationId}");
			await this.pushManager.SendNotificationAsync(installationId, "Hello world!", default(CancellationToken), NotificationTemplate.Neutral);
		}
	}
}