using Android.App;
using Android.Content;

namespace Android.Gms.Gcm
{
	/// <summary>
	/// See: https://developers.google.com/android/reference/com/google/android/gms/gcm/GcmReceiver
	/// WakefulBroadcastReceiver that receives GCM messages and delivers them to an application-specific GcmListenerService (<see cref="PushNotificationApp.Droid.MyGcmListenerService"/>) subclass.
	/// The receiver will be called is a message from GCM has been received. It will then pass work over to a service and ensures that the device
	/// won't go back to sleep during that transition.
	/// </summary>
	//[BroadcastReceiver (
	//	Name = "com.google.android.gms.gcm.GcmReceiver",
	//	Exported = true,
	//	Permission = "com.google.android.c2dm.permission.SEND")]
	//[IntentFilter (new [] {
	//	"com.google.android.c2dm.intent.RECEIVE",
	//	"com.google.android.c2dm.intent.REGISTRATION" },
	//	Categories = new [] { "@PACKAGE_NAME@" })]
	//partial class GcmReceiver
	//{
	//}
}