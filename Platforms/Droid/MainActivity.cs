using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Gcm.Iid;
using Android.OS;
using static System.Console;
using Android.Gms.Gcm;
using Android.Gms.Common;
using System;

// The code used here is based on Xamarin documentation:
// https://developer.xamarin.com/guides/cross-platform/application_fundamentals/notifications/android/remote_notifications_in_android/
// https://github.com/xamarin/monodroid-samples/tree/master/RemoteNotifications/ClientApp
// and Google documentation:
// Create a GCM app at: https://developers.google.com/mobile/add?platform=android (The package ID of the XS project must match!)
// https://developer.xamarin.com/guides/cross-platform/application_fundamentals/notifications/android/google-cloud-messaging/#settingup
// How to configure GCM for Azure Push Notifications: https://azure.microsoft.com/en-us/documentation/articles/notification-hubs-android-push-notification-google-gcm-get-started/
// Cloud Messaging requires various permissions. See AndroidManifest.xml for details.


// Steps to setup GCM on the server with Google:
//    At https://developers.google.com/mobile/add?platform=android register a new project.
//    This must be an "Android" project (even though we are sending via a custom server!). Be sure to correctly enter the package name of the client app.
//    The project I am using has been generated with the account rene@c-sharx.net and has the name "AzureXamUPushDemo".
//    The project ID is 90921695117, which is used as the "Sender ID" for GCM.
// 	  The server API key is not used by the client app but by Azure. It is "AIzaSyCGD0_LsyLWCW1FOGpKFI8QrFUmUMj9FgA"

namespace PushNotificationApp.Droid
{
	[Activity (Label = "PushNotificationApp.Droid", Icon = "@drawable/icon", Theme = "@style/MyTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		// The number specified here is the "sender ID". This can be found in the Google Developer Console in the project settings; it's the the "project number".
		public const string GoogleApiProjectNumber = "90921695117";
	
		public static App formsApp;

		protected override void OnCreate (Bundle bundle)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate (bundle);

			Xamarin.Forms.Forms.Init (this, bundle);

			MainActivity.formsApp = new App ();
			LoadApplication (MainActivity.formsApp);
		}

		protected override void OnStart ()
		{
			base.OnStart ();

			// Check for Google Play Services on the device. We need it to use GCM.
			if (this.IsPlayServicesAvailable ())
			{
				// Start the registration intent service; try to get a token:
				var intent = new Intent (this, typeof (RegisterDeviceService));
				StartService (intent);
			}
		}

		public bool IsPlayServicesAvailable ()
		{
			int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable (this);
			if (resultCode != ConnectionResult.Success)
			{
				if (GoogleApiAvailability.Instance.IsUserResolvableError (resultCode))
				{
					MainActivity.formsApp.OnNativeFailedToRegisterForRemoteNotifications(GoogleApiAvailability.Instance.GetErrorString (resultCode));
				}
				else
				{
					MainActivity.formsApp.OnNativeFailedToRegisterForRemoteNotifications("Sorry, this device is not supported");
					this.Finish ();
				}
				return false;
			}

			return true;
		}
	}

	/// <summary>
	/// The service will be used if we get get informed about new token assignment from GCM.
	/// It will be triggered by Android; we don't start it directly.
	/// This will only be called in case of a security issue. See here: https://developers.google.com/instance-id/#instance_id_lifecycle
	/// There seems to be a way to test it manually: http://stackoverflow.com/questions/30637347/when-will-instanceidlistenerservice-be-called-and-how-to-test-it
	/// For details on when and how this will be called: https://developers.google.com/instance-id/guides/android-implementation
	/// </summary>
	[Service (Exported = false)]
	[IntentFilter (new [] { InstanceID.IntentFilterAction })]
	public class InstanceListenerService : InstanceIDListenerService
	{
		public override void OnTokenRefresh ()
		{
			// We need to retrieve the token. This must happen asynchronously, so we use an IntentService.
			var intent = new Intent (this, typeof (RegisterDeviceService));
			this.StartService (intent);
		}
	}

	/// <summary>
	/// Service to retrieve the device/registration token from GCM.
	/// This gets called by <see cref="InstanceListenerService"/>.
	/// </summary>
	[Service (Exported = false)]
	class RegisterDeviceService : IntentService
	{
		// Create the IntentService, name the worker thread for debugging purposes:
		public RegisterDeviceService () : base ("RegisterDeviceService")
		{
		}

		// OnHandleIntent is invoked on a worker thread.
		protected override void OnHandleIntent (Intent intent)
		{
			try
			{
				WriteLine ("RegisterDeviceService - Calling InstanceID.GetToken()");

				// Request a registration token.
				// What is InstanceID: https://developers.google.com/instance-id/
				// In a nutshell, Instance ID provides a unique ID for the app.
				var instanceID = InstanceID.GetInstance (this);
				// The number specified here is the "sender ID". This can be found in the Google Developer Console in the project settings; it's the the "project number".
				var token = instanceID.GetToken (MainActivity.GoogleApiProjectNumber, GoogleCloudMessaging.InstanceIdScope, null);

				WriteLine ($"RegisterDeviceService - GCM Registration Token: {token}");

				// Let out Forms app know that we have a token.
				// IntentService uses queued worker threads. To do something on the UI we must invoke on the main thread.
				var handler = new Handler(Looper.MainLooper);
				handler.Post(() => {
					MainActivity.formsApp.OnNativeRegisteredForRemoteNotifications (token);
				});
			}
			catch (Exception e)
			{
				MainActivity.formsApp.OnNativeFailedToRegisterForRemoteNotifications($"Failed to get a registration token: {e.Message}");
			}
		}
	}
}

