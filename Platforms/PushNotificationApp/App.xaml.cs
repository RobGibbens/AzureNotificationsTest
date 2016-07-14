using PushNotificationsClient;
using Xamarin.Forms;
using PushNotificationsClientServerShared;

namespace PushNotificationApp
{
	public partial class App : Application
	{
		public const string RegisteredForRemoteNotificationsMessage = "RegisteredForRemoteNotifications";
		public const string ReceivedRemoteNotificationMessage = "ReceivedRemoteNotification";

		public App ()
		{
			InitializeComponent ();

			var tabs = new TabbedPage();
			tabs.Children.Add(new ChatPage());
			tabs.Children.Add(new StatusPage());
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
				App.Current.Properties.TryGetValue("DeviceToken", out pushDeviceId);
				return (string)pushDeviceId;
			}
			set
			{
				App.Current.Properties["DeviceToken"] = value;
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
				App.Current.Properties.TryGetValue("PushDeviceId", out pushDeviceId);
				return (string)pushDeviceId;
			}
			set
			{
				App.Current.Properties["PushDeviceId"] = value;
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
				App.Current.Properties.TryGetValue("DeviceName", out deviceName);
				return (string)deviceName;
			}
			set
			{
				App.Current.Properties["DeviceName"] = value;
			}
		}

		/// <summary>
		/// Client to communicate with Azure push notifications backend.
		/// </summary>
		/// <value>The push manager.</value>
		public static PushNotificationManager PushManager { get; } = new PushNotificationManager("http://192.168.178.44:8080");

		/// <summary>
		/// Gets called by the native platforms to let the Forms app know about a device token that can be used
		/// for remote notifications.
		/// </summary>
		/// <param name="deviceToken">Device token.</param>
		public void OnRegisteredForRemoteNotifications(string deviceToken)
		{
			MessagingCenter.Send(this, RegisteredForRemoteNotificationsMessage, deviceToken);
		}


		/// <summary>
		/// Gets called by the native platforms to inform about an unsuccesful registration for remote notifications.
		/// </summary>
		/// <returns>The failed to register for remote notifications.</returns>
		/// <param name="">.</param>
		public void OnFailedToRegisterForRemoteNotifications(string error)
		{
			MessagingCenter.Send(this, RegisteredForRemoteNotificationsMessage, (string)null);
		}

		/// <summary>
		/// Gets called by the native platforms if a remote notification was received.
		/// </summary>
		/// <returns>The received remote notification.</returns>
		public void OnReceivedRemoteNotification(string message)
		{
			MessagingCenter.Send(this, ReceivedRemoteNotificationMessage, message);	
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

