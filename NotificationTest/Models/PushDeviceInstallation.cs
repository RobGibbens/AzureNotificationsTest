using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Microsoft.Azure.NotificationHubs;

namespace NotificationTest.Models
{
    public class PushDeviceInstallation : Installation
    {
		public enum TEMPLATES
		{
			Default = 0
		}
		
		void AddTemplate(TEMPLATES templateType, string template)
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
				Tags = new List<string> { $"template-for-{key}" }
			});
		}

		public void AddDefaultTemplate()
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

			this.AddTemplate(TEMPLATES.Default, template);
		}

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
    }
}