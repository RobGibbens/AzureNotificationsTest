using System;
using Foundation;
using Plugin.Settings;
using UIKit;
using PushNotificationsClient;
using PushNotificationsClientServerShared;

namespace PushNotifications.iOS
{
	public class AppDelegate : UIApplicationDelegate
	{
		#region Store Device Token
		const string SETTING_TOKEN = "DeviceToken";

		static void SaveDeviceToken(string token)
		{
			if(string.IsNullOrWhiteSpace(token))
			{
				CrossSettings.Current.Remove(SETTING_TOKEN);
				return;
			}
			CrossSettings.Current.AddOrUpdateValue(SETTING_TOKEN, token);
		}

		static string GetSavedDeviceToken() => CrossSettings.Current.GetValueOrDefault<string>(SETTING_TOKEN, null);
		#endregion

		public override UIWindow Window
		{
			get;
			set;
		}

		PushNotificationManager pushManager = new PushNotificationManager();

		public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
		{
			// Guidelines for notifications: https://developer.apple.com/library/ios/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/Chapters/Introduction.html#//apple_ref/doc/uid/TP40008194

			// App must ask the user for permission, no matter if we're using local or remote notifications.
			// There is also RegisterForRemoteNotificationTypes() but it is deprecated in iOS8.
			application.RegisterUserNotificationSettings(UIUserNotificationSettings.GetSettingsForTypes(
				UIUserNotificationType.Alert
				| UIUserNotificationType.Badge
				| UIUserNotificationType.Sound,
				null));

			// Request registration for remote notifications. This will talk to Apple's push server and the app will receive a token
			// when RegisteredForRemoteNotifications() gets called.
			application.RegisterForRemoteNotifications();

			return true;
		}

		/// <summary>
		/// After you call the registerForRemoteNotifications method of the UIApplication object,
		/// the app calls this method when device registration completes successfully.
		/// In your implementation of this method, connect with your push notification server and give the token to it.
		/// APNs pushes notifications only to the device represented by the token.
		/// The app might call this method in other rare circumstances, such as when the user launches an app after having
		/// restored a device from data that is not the device’s backup data. In this exceptional case, the app won’t know
		/// the new device’s token until the user launches it.
		/// </summary>
		/// <returns>The for remote notifications.</returns>
		/// <param name="application">Application.</param>
		/// <param name="deviceToken">Device token.</param>
		public async override void RegisteredForRemoteNotifications (UIApplication application, NSData deviceToken)
		{
			if(deviceToken == null)
			{
				// Can happen in rare conditions e.g. after restoring a device.
				return;
			}
			// The device token is your key to sending push notifications to your app on a specific device.
			// Device tokens can change, so your app needs to reregister every time it is launched and pass the received token back to your server.

			// Azure SDK uses internally something hacky (https://github.com/Azure/azure-mobile-services/blob/4c3556d3fd3c89cacf9645b936ed495ec882eb02/sdk/Managed/src/Microsoft.WindowsAzure.MobileServices.iOS/Push/ApnsRegistration.cs#L72):
			//deviceToken.Description.Trim('<','>').Replace(" ", string.Empty).ToUpperInvariant();
			//var newDeviceToken = deviceToken.GetBase64EncodedString(NSDataBase64EncodingOptions.None);
			var newDeviceToken = deviceToken.Description.Trim('<','>').Replace(" ", string.Empty).ToUpperInvariant();

			// Unregister previous device.
			var previousDeviceToken = GetSavedDeviceToken();
			await this.pushManager.RegisterDeviceAsync(new DeviceInformation
			{
				DeviceToken = newDeviceToken,
				Platform = PLATFORM.iOS
			});

			// Remember new token.
			SaveDeviceToken(newDeviceToken);
		}

		/// <summary>
		/// After you call the RegisterForRemoteNotifications() method of the UIApplication object,
		/// the app calls this method when there is an error in the registration process.
		/// </summary>
		/// <param name="application">Application.</param>
		/// <param name="error">Error.</param>
		public override void FailedToRegisterForRemoteNotifications (UIApplication application, NSError error)
		{
			Console.WriteLine($"Failed to register for remote notifications: {error.Description}");
		}

		/// <summary>
		/// Will be called if app received a remote notification.
		/// According to Apple's docs this is the preferred method to use instead of ReceivedRemoteNotification().
		/// See: https://developer.apple.com/library/ios/documentation/UIKit/Reference/UIApplicationDelegate_Protocol/#//apple_ref/occ/intfm/UIApplicationDelegate/application:didReceiveRemoteNotification:fetchCompletionHandler:
		/// </summary>
		public override void DidReceiveRemoteNotification (UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
		{
			// This will be called if the app is in the background/not running and if in the foreground.
			// However, it will not display a notification visually if the app is in the foreground.

			// TODO: Handle
			// TODO: When to call the completionHandler?
		}

		/// <summary>
		/// If the notifications contains custom actions ("buttons"), this method will be called.
		/// The notification has to contain a "category" key in the payload.
		/// </summary>
		/// <returns>The action.</returns>
		/// <param name="application">Application.</param>
		/// <param name="actionIdentifier">Action identifier.</param>
		/// <param name="remoteNotificationInfo">Remote notification info.</param>
		/// <param name="completionHandler">Completion handler.</param>
		public override void HandleAction (UIApplication application, string actionIdentifier, NSDictionary remoteNotificationInfo, Action completionHandler)
		{
			// For details see: https://developer.apple.com/library/ios/documentation/UIKit/Reference/UIApplicationDelegate_Protocol/index.html#//apple_ref/occ/intfm/UIApplicationDelegate/application:handleActionWithIdentifier:forRemoteNotification:completionHandler:
		}
	}
}


