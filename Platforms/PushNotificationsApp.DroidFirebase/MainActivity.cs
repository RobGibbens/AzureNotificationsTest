using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using static System.Console;
using System;
using Firebase.Iid;
using Firebase.Messaging;
using Android.Gms.Common;
using Firebase;

// This is the Android version that uses Firebase Cloud Messaging.
// Reference: https://azure.microsoft.com/en-us/documentation/articles/notification-hubs-android-push-notification-google-fcm-get-started/
// An existing GCM project can be migrated to Firebase using the Firebase console: http://firebase.google.com/console/
// The console also provides the required server key (AIzaSyCGD0_LsyLWCW1FOGpKFI8QrFUmUMj9FgA) and sender ID (90921695117).
// The sender ID will be identical to the GCM project number and the server key is the same as with GCM and must be inserted into the Azure portal
// as the GCM API Key.
// From Azure's perspective, there is no difference between GCM and FCM.
// The differences are on the client where the registration is different or (as Google claims) easier.
// This projects adds the alpha package "Xamarin.Firebase.Messaging".
// In general, this is a good migration doc: https://developers.google.com/cloud-messaging/android/android-migrate-fcm


namespace PushNotificationApp.Droid
{
	[Activity (Label = "PushNotificationApp.Droid", Icon = "@drawable/icon", Theme = "@style/MyTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		// The number specified here is the "sender ID". This can be found in the Firebase Console in the project settings.
		// This is identical to the "project ID" in GCM.
		public const string FirebaseSenderId = "90921695117";
	
		public static App formsApp;

		protected override void OnCreate (Bundle bundle)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate (bundle);

			// We need this with Xamarin.Android because we don't process a config file which could be downloaded from the console and
			// included into the project.
			var options = new FirebaseOptions.Builder()
			                                 .SetApplicationId("1:90921695117:android:1c3be840990d46d1")
			                                 .SetApiKey("AIzaSyCGD0_LsyLWCW1FOGpKFI8QrFUmUMj9FgA")
			                                 .SetGcmSenderId(FirebaseSenderId)
			                                 .Build();

			FirebaseApp app = FirebaseApp.InitializeApp(this, options);

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
	/// The service will be used if we get get informed about new token assignment from Firebase CM.
	/// It will be triggered by Android; we don't start it directly.
	/// This will only be called in case of a security issue. See here: https://developers.google.com/instance-id/#instance_id_lifecycle
	/// There seems to be a way to test it manually: http://stackoverflow.com/questions/30637347/when-will-instanceidlistenerservice-be-called-and-how-to-test-it
	/// For details on when and how this will be called: https://developers.google.com/instance-id/guides/android-implementation
	/// </summary>
	[Service (Exported = false)]
	[IntentFilter (new [] { "com.google.firebase.INSTANCE_ID_EVENT" })]
	public class InstanceListenerService : FirebaseInstanceIdService
	{
		public override void OnTokenRefresh ()
		{
			// We need to retrieve the token. This must happen asynchronously, so we use an IntentService.
			var intent = new Intent (this, typeof (RegisterDeviceService));
			this.StartService (intent);
		}
	}

	/// <summary>
	/// Service to retrieve the device/registration token from Firebase CM.
	/// This gets called by <see cref="InstanceListenerService"/>.
	/// </summary>
	[Service(Exported = false), IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
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
				var instanceID = FirebaseInstanceId.Instance;
				// The number specified here is the "sender ID". This can be found in the Google Developer Console in the project settings; it's the the "project number".
				var token = instanceID.GetToken (MainActivity.FirebaseSenderId, FirebaseMessaging.InstanceIdScope);

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

