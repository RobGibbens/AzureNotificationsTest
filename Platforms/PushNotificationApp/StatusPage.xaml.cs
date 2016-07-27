using System;
using System.Collections.Generic;

using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Threading;
using PushNotificationsClientServerShared;
using Plugin.DeviceInfo;
using System.Linq;

namespace PushNotificationApp
{
	public partial class StatusPage : ContentPage
	{
		public StatusPage ()
		{
			this.Padding = new Thickness (0, Device.OnPlatform (20, 0, 0), 0, 0);
			this.BindingContext = this;
			InitializeComponent ();

			this.txtDeviceName.Text = App.DeviceName;

			this.AddStatus ("Ready.");

			// Get informed whenever the native platform gives us a new device token.
			// In this case we have to (re)register the device with the backend.
			MessagingCenter.Subscribe<App, string> (this, App.RegisteredForRemoteNotificationsMessage, this.OnDeviceTokenChanged);
		}

		string currentDeviceToken;

		public ObservableCollection<StatusUpdate> StatusUpdates { get; } = new ObservableCollection<StatusUpdate> ();

		void AddStatus (string title, string message = null)
		{
			this.StatusUpdates.Add (new StatusUpdate {
				Title = title,
				Message = message
			});
			this.lstStatus.ScrollTo(this.StatusUpdates.Last(), ScrollToPosition.Start, true);
		}

		void OnDeviceTokenChanged (object sender, string deviceToken)
		{
			this.currentDeviceToken = deviceToken;
			this.AddStatus ("Received token", deviceToken ?? "(null)");
			if(App.DeviceToken != deviceToken && deviceToken != null)
			{
				this.AddStatus("Registration required!", "Device token changed - update registration.");
			}
			App.DeviceToken = deviceToken;
		}

		async void HandleRegisterDeviceClicked (object sender, EventArgs e)
		{
			App.DeviceName = this.txtDeviceName.Text;

			if(string.IsNullOrWhiteSpace(this.currentDeviceToken) || string.IsNullOrWhiteSpace(App.DeviceName))
			{
				this.DisplayAlert(string.Empty, "Device token and device name must be set to register.", "OK");
				return;
			}

			this.AddStatus ("Registering", $"Token: {this.currentDeviceToken}");

			// Every device is assigned a unique installation ID by the backend.
			// If the installation ID is null, a new installation will be created, otherwise an existing will be updated.
			var deviceInfo = new DeviceInformation {
				UniqueId = App.PushDeviceId,
				// The native token is needed by Azure to send a notification to the device.
				DeviceToken = this.currentDeviceToken,
				DeviceName = App.DeviceName
			};

			switch (CrossDeviceInfo.Current.Platform)
			{
			case Plugin.DeviceInfo.Abstractions.Platform.iOS:
				deviceInfo.Platform = Platform.iOS;
				break;
			case Plugin.DeviceInfo.Abstractions.Platform.Android:
				deviceInfo.Platform = Platform.Android;
				break;
			default:
				throw new InvalidOperationException ("Unsupported platform!");
			}

			var pushDeviceId = await App.PushManager.RegisterOrUpdateDeviceAsync (deviceInfo);

			if (pushDeviceId == null)
			{
				this.AddStatus ("Registration failed", "");
			}
			else
			{
				this.AddStatus ("Registration complete", $"Push device ID: {pushDeviceId}");
				// Remember new installation ID.
				App.PushDeviceId = pushDeviceId;
			}
		}

		async void HandleUnregisterDeviceClicked (object sender, System.EventArgs e)
		{
			if(string.IsNullOrWhiteSpace(this.currentDeviceToken))
			{
				this.DisplayAlert(string.Empty, "Device token must be set to unregister.", "OK");
				return;
			}

			this.AddStatus ("Unregistering", $"ID: {App.PushDeviceId}");
			var result = await App.PushManager.UnregisterDeviceAsync (App.PushDeviceId);

			if (result == null)
			{
				this.AddStatus ("Unregistering failed", $"Tried ID: {App.PushDeviceId}");
			}
			else
			{
				this.AddStatus ("Unregistering suceeded", $"Unregistered: {result.DeviceName}");
			}
			App.PushDeviceId = null;
		}
	}
}

