using System;
using System.Text;
using Microsoft.Azure.NotificationHubs;
using PushNotificationsClientServerShared;

namespace PushNotificationsServer
{
	public static class Extensions
	{
		public static Platform ToDeviceInfoPlatform(this NotificationPlatform platform)
		{
			switch (platform)
			{
				case NotificationPlatform.Apns:
					return Platform.iOS;
				case NotificationPlatform.Gcm:
					return Platform.Android;
				default:
					throw new InvalidOperationException($"Platform not supported: {platform.ToString()}");
			}
		}

		public static NotificationPlatform ToNotificationPlatform(this Platform devicePlatform)
		{
			switch (devicePlatform)
			{
				case Platform.iOS:
					return NotificationPlatform.Apns;
				case Platform.Android:
					return NotificationPlatform.Gcm;
				default:
					throw new InvalidOperationException($"Platform not supported: {devicePlatform.ToString()}");
			}
		}

		public static string ToBase64String(this string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
	}
}