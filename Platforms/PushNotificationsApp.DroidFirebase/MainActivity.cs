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
// The console also provides the required server key (AIzaSyBqaf4R9Nsp0HVrZ52bARs8LibwAPJ-q0s) which must be copied from the "Cloud Messaging" tab!
// It's NOT the Web API Key from the "General Tab"!) and sender ID (1096275859011).
// The sender ID will be identical to the GCM project number and the server key is the same as with GCM and must be inserted into the Azure portal
// as the GCM API Key.
// From Azure's perspective, there is no difference between GCM and FCM.
// The differences are on the client where the registration is different or (as Google claims) easier.
// This projects adds the alpha package "Xamarin.Firebase.Messaging".
// In general, this is a good migration doc: https://developers.google.com/cloud-messaging/android/android-migrate-fcm
//
// The Xamarin repo for Firebase related stuff and a FCM sample:
//   https://github.com/xamarin/GooglePlayServicesComponents/tree/v9.4.0/firebase-messaging/samples/FirebaseMessagingQuickstart
//   Firebase CM requires some manual setup with Xamarin. Documented at: https://github.com/xamarin/GooglePlayServicesComponents/blob/v9.4.0/firebase-messaging/component/GettingStarted.template.md


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

			// The Xamarin Firebase component can read required initialization data directly from the config file which
			// is downloadable from the Firebase Console. However the name has to match. This works pretty much like the native "gradle" support.
			// The "google-services.json" file must be added to the root of the project and its build action set to "GoogleServicesJson".
			// The App ID is available in the Firebase Console.
			if (GetString (Resource.String.google_app_id) != "1:1096275859011:android:1c3be840990d46d1")
				throw new System.Exception ("Invalid google-services.json file.  Make sure you've downloaded your own config file and added it to your app project with the 'GoogleServicesJson' build action.");

			Xamarin.Forms.Forms.Init (this, bundle);

			MainActivity.formsApp = new App ();
			LoadApplication (MainActivity.formsApp);
		}

		protected override void OnStart ()
		{
			base.OnStart ();
			var activeToken = FirebaseInstanceId.Instance.Token;
		}
	}

	/// <summary>
	/// The service will be used if we get get informed about new token assignment from Firebase CM.
	/// It will be triggered by Android; we don't start it directly.
	/// For details on when and how this will be called: https://developers.google.com/instance-id/guides/android-implementation
	/// </summary>
	[Service (Exported = false)]
	[IntentFilter (new [] { "com.google.firebase.INSTANCE_ID_EVENT" })]
	public class InstanceListenerService : FirebaseInstanceIdService
	{
		public override void OnTokenRefresh ()
		{
			var refreshedToken = FirebaseInstanceId.Instance.Token;

			// Let out Forms app know that we have a token.
			// IntentService uses queued worker threads. To do something on the UI we must invoke on the main thread.
			var handler = new Handler(Looper.MainLooper);
			handler.Post(() => {
				MainActivity.formsApp.OnNativeRegisteredForRemoteNotifications (refreshedToken);
			});
		}
	}
}

