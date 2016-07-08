using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.NotificationHubs;
using PushNotificationsClientServerShared;

namespace PushNotificationsServer.Models
{
	/// <summary>
	/// An "Installation" represents a registered device in Azure notificatin services.
	/// This custom subclass stores additional data and has some helper methods.
	/// The Installation instances and the type are never exposed to the client.
	/// Clients only get <see cref="PushNotificationsClientServerShared.DeviceInformation"/> objects.
	/// </summary>
	public sealed class CustomDeviceInstallation : Installation
    {
		void AddOrUpdateTemplate(NotificationTemplate templateType, string template)
		{
			if (this.Templates == null)
			{
				this.Templates = new Dictionary<string, InstallationTemplate>();
			}

			string key = templateType.ToString();

			if (this.Templates.ContainsKey(key))
			{
				this.Templates.Remove(key);
			}

			// Add the template. For unknown reasons a Dictionary<string, InstallationTemplate> is used here and
			// not a List<InstallationTemplate>. The key (string) seems to be unused when evaluating template expressions.
			// See: http://stackoverflow.com/questions/38107932/how-to-correctly-use-the-microsoft-azure-notificationhubs-installation-class/38262225#38262225
			this.Templates.Add(key, new InstallationTemplate
			{
				Body = template,
				Tags = new List<string> { $"template-{key}" }
			});
		}

		/// <summary>
		/// Configures the templates of the installation.
		/// </summary>
		public void AddOrUpdateTemplates()
		{
			// About templates and template expressions: https://azure.microsoft.com/en-us/documentation/articles/notification-hubs-templates-cross-platform-push-messages/
			string happyTemplate = null;
			string unhappyTemplate = null;
			switch (this.Platform)
			{
				// iOS
				case NotificationPlatform.Apns:
					// Possible payloads for iOS: https://developer.apple.com/library/ios/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/Chapters/TheNotificationPayload.html
					happyTemplate = "{\"aps\":{\"alert\":\"{'😀 ' + $(message)}\"}}";
					unhappyTemplate = "{\"aps\":{\"alert\":\"{'🙁 ' + $(message)}\"}}";
					break;

				// Android
				case NotificationPlatform.Gcm:
					// GCM payloads: https://developers.google.com/cloud-messaging/concept-options#notifications_and_data_messages
					happyTemplate = "{\"data\":{\"msg\":\"{'😀 ' + $(message)}\"}}";
					unhappyTemplate = "{\"data\":{\"msg\":\"{'🙁 ' + $(message)}\"}}";
					break;
					
				default:
					throw new InvalidOperationException("Unsupported target platform.");
			}

			this.AddOrUpdateTemplate(NotificationTemplate.Happy, happyTemplate);
			this.AddOrUpdateTemplate(NotificationTemplate.Unhappy, unhappyTemplate);
		}

		/// <summary>
		/// Contains when this device was last updated  by the client.
		/// </summary>
		public DateTime LastUpdated { get; set; }

		[Key]
        public string Id
        {
            get
            {
                return base.InstallationId;
            }
            set
            {
                base.InstallationId = value;
            }
        }

		/// <summary>
		/// Optional device name.
		/// </summary>
		public string DeviceName
		{
			get;
			set;
		}
    }
}