using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.NotificationHubs;
using PushNotificationsClientServerShared;

namespace PushNotificationsServer
{
	/// <summary>
	/// Collection of extension methods used by the backend.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Converts APNS <see cref="NotificationPlatform"/> into the enumeration type used by the backend (<see cref="Platform"/>).
		/// </summary>
		/// <param name="devicePlatform">platform to convert</param>
		/// <returns>converted platform</returns>
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

		/// <summary>
		/// Converts the backends <see cref="Platform"/> into the enumeration type required by APNS (<see cref="NotificationPlatform"/>).
		/// </summary>
		/// <param name="devicePlatform">platform to convert</param>
		/// <returns>converted platform</returns>
		public static NotificationPlatform ToAzureNotificationPlatform(this Platform devicePlatform)
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

		/// <summary>
		/// Converts a string into its base64 representation.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string ToBase64String(this string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

		/// <summary>
		/// Helper to add a notification template to an installation.
		/// </summary>
		/// <param name="installation"></param>
		/// <param name="templateType"></param>
		/// <param name="template"></param>
		public static void AddOrUpdateTemplate(this Installation installation, NotificationTemplate templateType, string template)
		{
			if (installation.Templates == null)
			{
				installation.Templates = new Dictionary<string, InstallationTemplate>();
			}

			string key = templateType.ToString();

			if (installation.Templates.ContainsKey(key))
			{
				installation.Templates.Remove(key);
			}

			// Add the template. For unknown reasons a Dictionary<string, InstallationTemplate> is used here and
			// not a List<InstallationTemplate>. The key (string) seems to be unused when evaluating template expressions.
			// See: http://stackoverflow.com/questions/38107932/how-to-correctly-use-the-microsoft-azure-notificationhubs-installation-class/38262225#38262225
			installation.Templates.Add(key, new InstallationTemplate
			{
				Body = template,
				Tags = new List<string> { $"template-{key}" }
			});
		}

		/// <summary>
		/// Configures the templates of the installation.
		/// </summary>
		public static void AddOrUpdateTemplates(this Installation installation)
		{
			// About templates and template expressions: https://azure.microsoft.com/en-us/documentation/articles/notification-hubs-templates-cross-platform-push-messages/
			string neutralTemplate = null;
			string happyTemplate = null;
			string unhappyTemplate = null;
			switch (installation.Platform)
			{
				// iOS
				case NotificationPlatform.Apns:
					// Possible payloads for iOS: https://developer.apple.com/library/ios/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/Chapters/TheNotificationPayload.html
					neutralTemplate = "{\"aps\":{\"alert\":\"{ $(sender) + ': ' + $(message) }\" } }";
					happyTemplate = "{\"aps\":{\"alert\":\"{ $(sender) + ': \U0001F600 ' + $(message)}\" } }";
					unhappyTemplate = "{\"aps\":{\"alert\":\"{$(sender) + ': \U0001F61F ' + $(message)}\" } }";
					break;

				// Android
				case NotificationPlatform.Gcm:
					// GCM payloads: https://developers.google.com/cloud-messaging/concept-options#notifications_and_data_messages
					neutralTemplate = "{\"data\":{\"msg\":\"{ $(sender) + ': ' + $(message) }\" } }";
					happyTemplate = "{\"data\":{\"msg\":\"{ $(sender) + ': \U0001F600 ' + $(message)}\" } }";
					unhappyTemplate = "{\"data\":{\"msg\":\"{ $(sender) + ': \U0001F61F ' + $(message)}\" } }";
					break;

				default:
					throw new InvalidOperationException("Unsupported target platform.");
			}

			installation.AddOrUpdateTemplate(NotificationTemplate.Neutral, neutralTemplate);
			installation.AddOrUpdateTemplate(NotificationTemplate.Happy, happyTemplate);
			installation.AddOrUpdateTemplate(NotificationTemplate.Unhappy, unhappyTemplate);
		}
	}
}