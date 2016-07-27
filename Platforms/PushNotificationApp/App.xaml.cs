using PushNotificationsClient;
using Xamarin.Forms;
using PushNotificationsClientServerShared;
using System;
using Plugin.DeviceInfo;

namespace PushNotificationApp
{
	public partial class App : Application
	{
		public const string RegisteredForRemoteNotificationsMessage = "RegisteredForRemoteNotifications";
		public const string ReceivedRemoteNotificationMessage = "ReceivedRemoteNotification";

		public App ()
		{
			InitializeComponent ();

			var tabs = new TabbedPage ();
			tabs.Children.Add (new ChatPage ());
			MainPage = tabs;
		}

		/// <summary>
		/// The native token to register for remote notifications.
		/// </summary>
		public static string DeviceToken
		{
			get
			{
				object pushDeviceId = null;
				App.Current.Properties.TryGetValue ("DeviceToken", out pushDeviceId);
				return (string)pushDeviceId;
			}
			set
			{
				App.Current.Properties ["DeviceToken"] = value;
			}
		}

		/// <summary>
		/// The unique ID used to identify this app/device when registered for Azure push notifications.
		/// </summary>
		public static string PushDeviceId
		{
			get
			{
				object pushDeviceId = null;
				App.Current.Properties.TryGetValue ("PushDeviceId", out pushDeviceId);
				return (string)pushDeviceId;
			}
			set
			{
				App.Current.Properties ["PushDeviceId"] = value;
			}
		}

		/// <summary>
		/// The name of the device. Used during registration.
		/// </summary>
		/// <value>The push device identifier.</value>
		public static string DeviceName
		{
			get
			{
				object deviceName = null;
				App.Current.Properties.TryGetValue ("DeviceName", out deviceName);
				return (string)deviceName;
			}
			set
			{
				App.Current.Properties ["DeviceName"] = value;
			}
		}

		/// <summary>
		/// Client to communicate with Azure push notifications backend.
		/// </summary>
		/// <value>The push manager.</value>
		public static PushNotificationManager PushManager { get; } = new PushNotificationManager ("http://192.168.178.44:8080");

		/// <summary>
		/// Gets called by the native platforms to let the Forms app know about a device token that can be used
		/// for remote notifications.
		/// </summary>
		/// <param name="deviceToken">Device token.</param>
		public async void OnRegisteredForRemoteNotifications (string deviceToken)
		{
			if (string.IsNullOrWhiteSpace (deviceToken))
			{
				return;
			}

			// Create a random device name if we don't have one. Can be changed through the UI.
			if (string.IsNullOrWhiteSpace (App.DeviceName))
			{
				App.DeviceName = $"Unknown device {Guid.NewGuid ().ToString ()}";
			}

			// Every device is assigned a unique installation ID by the backend.
			// If the current device ID is null, a new installation will be created, otherwise an existing will be updated.
			var deviceInfo = new DeviceInformation {
				Id = App.PushDeviceId,
				// The native token is needed by Azure to send a notification to the device.
				DeviceToken = deviceToken,
				DeviceName = App.DeviceName
			};

			// Find out which platform we're on. This is required by the backend to use the correct template
			// when sending push notifications.
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

			try
			{
				// Register (or update) with the backend.
				var registeredDeviceInfo = await App.PushManager.RegisterOrUpdateDeviceAsync (deviceInfo);

				if (registeredDeviceInfo == null)
				{
					App.Current.MainPage.DisplayAlert ("Registration failed", "Failed to register for push notifications.", "OK");
				}
				else
				{
					App.Current.MainPage.DisplayAlert ("Registration complete", $"Unique device ID: {registeredDeviceInfo.Id} for token {deviceToken}", "OK");
					// Remember new installation ID.
					App.PushDeviceId = registeredDeviceInfo.Id;
				}

				MessagingCenter.Send (this, RegisteredForRemoteNotificationsMessage, deviceToken);
			}
			catch (Exception ex)
			{
				App.Current.MainPage.DisplayAlert ("Registration failed", ex.Message, "OK");
			}
		}


		/// <summary>
		/// Gets called by the native platforms to inform about an unsuccesful registration for remote notifications.
		/// </summary>
		/// <returns>The failed to register for remote notifications.</returns>
		/// <param name="">.</param>
		public void OnFailedToRegisterForRemoteNotifications (string error)
		{
			App.Current.MainPage.DisplayAlert ("Registration failed", error, "OK");
		}

		/// <summary>
		/// Gets called by the native platforms if a remote notification was received.
		/// </summary>
		/// <returns>The received remote notification.</returns>
		public void OnReceivedRemoteNotification (string message)
		{
			MessagingCenter.Send (this, ReceivedRemoteNotificationMessage, message);
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}

