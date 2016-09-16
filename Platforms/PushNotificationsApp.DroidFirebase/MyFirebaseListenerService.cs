using Android.App;
using Android.Content;
using Android.OS;
using Firebase.Messaging;

namespace PushNotificationApp.Droid
{
	// This service is only required for certain scenarios, as describe here: https://developers.google.com/cloud-messaging/android/android-migrate-fcm#migrate_your_gcmlistenerservice
	[Service (Exported = false), IntentFilter (new [] { "com.google.android.c2dm.intent.RECEIVE" })]
	public class MyFirebaseListenerService : FirebaseMessagingService
	{
		public override void OnMessageReceived (RemoteMessage message)
		{
			base.OnMessageReceived (message);

			// Extract the message.
			string msg = null;
			message.Data.TryGetValue("msg", out msg);

			MainActivity.formsApp.OnNativeReceivedRemoteNotification(msg);
		}
	}
}

