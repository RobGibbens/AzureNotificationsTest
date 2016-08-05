using Android.App;
using Android.Content;

// We need to declare the built-in GCM BroadcastReceiver in the manifest.
// The easiest way to do this is to add a new .cs file (the one you are currently looking at) to the with the following code in it: 
namespace Android.Gms.Gcm
{
	[BroadcastReceiver (
		Name = "com.google.android.gms.gcm.GcmReceiver",
		Exported = true,
		Permission = "com.google.android.c2dm.permission.SEND")]
	[IntentFilter (new [] { "com.google.android.c2dm.intent.RECEIVE", "com.google.android.c2dm.intent.REGISTRATION" }, Categories = new [] { "@PACKAGE_NAME@" })]
	partial class GcmReceiver
	{
	}
}

