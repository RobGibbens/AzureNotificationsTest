using Android.App;
using Android.Content;
using Android.Gms.Gcm;
using Android.OS;

namespace PushNotificationApp.Droid
{
	[Service (Exported = false), IntentFilter (new [] { "com.google.android.c2dm.intent.RECEIVE" })]
	public class MyGcmListenerService : GcmListenerService
	{
		public override void OnMessageReceived (string from, Bundle data)
		{
			// Extract the message received from GCM.
			var message = data.GetString ("msg");

			// Forward the received message in a local notification.
			//SendNotification (message);

			MainActivity.formsApp.OnNativeReceivedRemoteNotification(message);
		}

		/// <summary>
		/// Helper to create a local notification and display it in the messaging center.
		/// </summary>
		/// <param name="message">Message.</param>
		void SendNotification (string message)
		{
			var intent = new Intent (this, typeof (MainActivity));
			intent.AddFlags (ActivityFlags.ClearTop);
			var pendingIntent = PendingIntent.GetActivity (this, 0, intent, PendingIntentFlags.OneShot);

			var notificationBuilder = new Notification.Builder (this)
			                                          // The icon is mandatory. If omitted, the notification will not be shown. No error either.
			                                          .SetSmallIcon (Resource.Drawable.icon)
			                                          // The title is mandatory.
													  .SetContentTitle ("GCM Message")
			                                          // The text is madatory.
													  .SetContentText (message)
													  .SetAutoCancel (true)
													  .SetContentIntent (pendingIntent);

			var notificationManager = (NotificationManager)GetSystemService (Context.NotificationService);
			notificationManager.Notify (0, notificationBuilder.Build ());
		}
	}
}

