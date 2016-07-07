using System;
using Microsoft.Azure.NotificationHubs;
using PushNotificationsClientServerShared;

namespace PushNotificationsServer
{
	public static class Extensions
	{
		public static PLATFORM ToDeviceInfoPlatform(this NotificationPlatform platform)
		{
			switch (platform)
			{
				case NotificationPlatform.Apns:
					return PLATFORM.iOS;
				case NotificationPlatform.Gcm:
					return PLATFORM.Android;
				default:
					throw new InvalidOperationException($"Platform not supported: {platform.ToString()}");
			}
		}

		public static NotificationPlatform ToNotificationPlatform(this PLATFORM devicePlatform)
		{
			switch (devicePlatform)
			{
				case PLATFORM.iOS:
					return NotificationPlatform.Apns;
				case PLATFORM.Android:
					return NotificationPlatform.Gcm;
				default:
					throw new InvalidOperationException($"Platform not supported: {devicePlatform.ToString()}");
			}
		}
	}
}