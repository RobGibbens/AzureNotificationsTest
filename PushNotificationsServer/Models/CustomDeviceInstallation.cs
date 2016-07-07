using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.NotificationHubs;

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
		public enum TEMPLATES
		{
			Default = 0
		}
		
		void AddOrUpdateTemplate(TEMPLATES templateType, string template)
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

			this.Templates.Add(key, new InstallationTemplate
			{
				Body = template,
				Tags = new List<string> { $"template-for-{key}", $"platform-{this.Platform.ToString()}" }
			});
		}

		public void AddOrUpdateDefaultTemplate()
		{
			string template = null;
			switch (this.Platform)
			{
				// iOS
				case NotificationPlatform.Apns:
					template = "{\"aps\":{\"alert\":\"$(message)\"}}";
					break;

				// Android
				case NotificationPlatform.Gcm:
					template = "{\"data\":{\"msg\":\"$(message)\"}}";
					break;
					
				default:
					throw new InvalidOperationException("Unsupported target platform.");
			}

			this.AddOrUpdateTemplate(TEMPLATES.Default, template);
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