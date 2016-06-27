using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Azure.NotificationHubs;
using NotificationTest.Models;
using WindowsAzure.Messaging;

namespace NotificationTest.API.Controllers
{
	public class InstallationController : ApiController
	{
		private readonly NotificationHubClient notificationHubClient;
		private const string HubName = "RenePushNotificationHub";
		private const string ConnectionString = "Endpoint=sb://renepushnotificationnamespace.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=sO84pxrkbJnP0cwR9l6JCCW4mu064eCAwR/v44N6ZT4=";
		public InstallationController()
		{
			this.notificationHubClient = NotificationHubClient.CreateClientFromConnectionString(ConnectionString, HubName);
		}

		 
		public async Task<IEnumerable<string>> GetRegisteredDevicesAsync()
		{
			var registrations = await this.notificationHubClient.GetAllRegistrationsAsync(99999);
			return registrations.Select(r => r.RegistrationId);
		}

		[HttpPost]
		public IHttpActionResult CreateInstallation(string platformId, string comment, string token)
		{
			if(string.IsNullOrWhiteSpace(token))
			{
				return this.BadRequest("Token must be specified.");
			}

			NotificationPlatform platform = NotificationPlatform.Adm;
			switch(platformId)
			{
				case "ios":
					platform = NotificationPlatform.Apns;
					break;
				default:
					return this.BadRequest("Invalid platform.");
				
			}

			var installation = new InstallationModel
			{
				Comment = comment,
				Platform =platform,
				InstallationId = Guid.NewGuid().ToString(),
				Tags = null,
				PushChannel = token
			};
			notificationHubClient.CreateOrUpdateInstallation(installation);
			return Ok();
		}
	}

}