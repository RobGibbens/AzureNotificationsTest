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


// Cloud Messaging requires the Internet, WakeLock, and com.google.android.c2dm.permission.RECEIVE permissions.
[assembly: UsesPermission ("com.google.android.c2dm.permission.RECEIVE")]
[assembly: UsesPermission (Android.Manifest.Permission.Internet)]
[assembly: UsesPermission (Android.Manifest.Permission.WakeLock)]
// Cloud messaging also requires us to declare and use a special permission (@PACKAGE_NAME@.permission.C2D_MESSAGE).
[assembly: Permission (Name = "@PACKAGE_NAME@.permission.C2D_MESSAGE")]
[assembly: UsesPermission ("@PACKAGE_NAME@.permission.C2D_MESSAGE")]


// Server API key: AIzaSyBm3rres9hbvLA2QeWoqwmwzva5GlBz7P0
// Sender ID: 287960406832

namespace PushNotificationApp.Droid
{
	[Activity (Label = "PushNotificationApp.Droid", Icon = "@drawable/icon", Theme = "@style/MyTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		public static App formsApp;

		protected override void OnCreate (Bundle bundle)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate (bundle);

			Xamarin.Forms.Forms.Init (this, bundle);

			MainActivity.formsApp = new App ();
			LoadApplication (MainActivity.formsApp);

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
				var token = instanceID.GetToken ("29713501615", GoogleCloudMessaging.InstanceIdScope, null);

				WriteLine ($"RegisterDeviceService - GCM Registration Token: {token}");

				// Let out Forms app know that we have a token.
				MainActivity.formsApp.OnNativeRegisteredForRemoteNotifications (token);
			}
			catch (Exception e)
			{
				MainActivity.formsApp.OnNativeFailedToRegisterForRemoteNotifications($"Failed to get a registration token: {e.Message}");
			}
		}
	}
}

