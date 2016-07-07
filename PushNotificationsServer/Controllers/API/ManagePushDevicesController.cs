using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Azure.NotificationHubs;
using PushNotificationsClientServerShared;
using PushNotificationsServer.Models;

namespace PushNotificationsServer.Controllers.API
{
	[RoutePrefix("api")]
	public class ManagePushDevicesController : ApiController
	{
		/// <summary>
		/// If set to TRUE, Azure will throttle the sent notifications and limit them to a maximum of 10.
		/// In exchange, we get feedback if an attempt to send succeeded and how many devices were reached.
		/// </summary>
		#if DEBUG
		const bool USE_TEST_SENDING = true;
		#else
		const bool USE_TEST_SENDING = false;
		#endif

		readonly PushNotificationContext db = new PushNotificationContext();
		readonly NotificationHubClient notificationHubClient = NotificationHubClient.CreateClientFromConnectionString(
			connectionString: "Endpoint=sb://renepushnotificationnamespace.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=sO84pxrkbJnP0cwR9l6JCCW4mu064eCAwR/v44N6ZT4=",
			notificationHubPath: "RenePushNotificationHub",
			enableTestSend: USE_TEST_SENDING);

		/// <summary>
		/// Gets all registered devices from the database.
		/// </summary>
		/// <returns>device information</returns>
		[Route("")]
		[HttpGet]
		public IQueryable<DeviceInformation> GetAllRegisteredDevices()
		{
			var result = this.db.CustomDeviceInstallations.Select(install => new DeviceInformation
			{
				Id = install.Id,
				DeviceToken = install.PushChannel,
				
				// Cannot use this nice extension method because it cannot be turned into an expression.
				//Platform = install.Platform.ToDeviceInfoPlatform(),
				Platform =
					install.Platform == NotificationPlatform.Apns ? PLATFORM.iOS :
					install.Platform == NotificationPlatform.Gcm ? PLATFORM.Android :
					PLATFORM.Unknown,
				DeviceName = install.DeviceName
			});

			return result;
		}
		

		/// <summary>
		/// Deletes the installation associated with the ID.
		/// </summary>
		/// <param name="id">unique ID of the device to delete</param>
		/// <returns>NULL if deletion failed, otherwise the deleted device information.</returns>
		[Route("unregister/{id}")]
		[HttpDelete]
		[ResponseType(typeof(DeviceInformation))]
		public IHttpActionResult UnregisterDevice(string id)
		{
			var installation = this.db.CustomDeviceInstallations.FirstOrDefault(d => d.Id == id);
			if (installation == null)
			{
				return this.NotFound();
			}

			// Delete from Azure.
			// Note: the ID is the GUID the backend assigns to each device. Don't confuse with the device token.
			this.notificationHubClient.DeleteInstallation(installation.InstallationId);

			// Delete locally.
			this.db.CustomDeviceInstallations.Remove(installation);
			this.db.SaveChanges();

			// Return the deleted object.
			var info = new DeviceInformation
			{
				Id = installation.Id,
				DeviceToken = installation.PushChannel,
				Platform = installation.Platform.ToDeviceInfoPlatform(),
				DeviceName = installation.DeviceName
			};
			
			return this.Ok(info);
		}

		/// <summary>
		/// Registers or updates a device.
		/// 
		/// Headers:
		/// Accept:application/json
		/// Content-Type:application/json
		/// 
		/// Body:
		/// {
		///		"Platform": 0,
		///		"DeviceToken": "ABC123",
		///		"DeviceName": "Monkey's iPad"
		/// }
		/// </summary>
		/// <param name="deviceInfo">Information about the device. Set the 'Id' property to NULL to create a new device.</param>
		/// <returns>the installation ID of the device. Must be stored by client to allow updating the installation.</returns>
		[Route("register")]
		[HttpPost]
		[ResponseType(typeof(string))]
		public IHttpActionResult RegisterOrUpdateDevice([FromBody] DeviceInformation deviceInfo)
		{
			if (deviceInfo == null)
			{
				return BadRequest("Device information required.");
			}

			if (string.IsNullOrWhiteSpace(deviceInfo.DeviceToken))
			{
				return BadRequest("Device token must be specified.");
			}

			CustomDeviceInstallation installation;

			if (deviceInfo.Id != null)
			{
				// Check for existing device.
				installation = this.db.CustomDeviceInstallations.FirstOrDefault(d => d.InstallationId == deviceInfo.Id);
				if (installation == null)
				{
					return this.BadRequest($"Cannot find device to update for ID [{deviceInfo.Id}].");
				}
			}
			else
			{
				if (string.IsNullOrWhiteSpace(deviceInfo.DeviceName))
				{
					return this.BadRequest("Device name is required when registering a new device.");
				}

				// Every device/installation gets a unique ID.
				installation = new CustomDeviceInstallation();
				installation.Id = Guid.NewGuid().ToString(); 
			}

			// Convert our platform information over to Azure's NotificationPlatform.
			installation.Platform = deviceInfo.Platform.ToNotificationPlatform();
			// The token must be a string of hexadecimal numbers, otherwise registering with Azure will fail.
			installation.PushChannel = deviceInfo.DeviceToken;
			installation.LastUpdated = DateTime.UtcNow;
			installation.AddOrUpdateDefaultTemplate();

			// Save to local DB. 
			this.db.CustomDeviceInstallations.AddOrUpdate(installation);
			this.db.SaveChanges();

			// Register with Azure Notification Hub.
			this.notificationHubClient.CreateOrUpdateInstallation(installation);

			// Return the unique ID.
			return this.Ok(installation.Id);
		}

		/// <summary>
		/// Sends a notification to all registered devices.
		/// </summary>
		/// <param name="sendData">message to send</param>
		/// <returns></returns>
		[Route("send")]
		[HttpPost]
		public async Task<IHttpActionResult> SendNotification([FromBody] SendData sendData)
		{
			if (sendData == null)
			{
				return this.BadRequest("Send data is required.");
			}

			if (string.IsNullOrWhiteSpace(sendData.SenderId) || string.IsNullOrWhiteSpace(sendData.Message))
			{
				return this.BadRequest("Sender and message content are required.");
			}

			string tags = null;
			if (sendData.TargetPlatforms != null)
			{
				// If we have limited platforms to send to, create a tag expression. See: https://azure.microsoft.com/en-us/documentation/articles/notification-hubs-tags-segment-push-message/
				tags = string.Join("||", sendData.TargetPlatforms.Select(tp => $"platform-{tp.ToString()}"));
			}

			var result = await this.notificationHubClient.SendTemplateNotificationAsync(
				// Set placeholders of templates.
				properties:	new Dictionary<string, string> {
					["message"] = sendData.Message
				},
				tagExpression: tags
			).ConfigureAwait(false);

			// Will only be set if USE_TEST_SENDING is set to TRUE.
			if (result.Failure > 0)
			{
				Debug.WriteLine($"{nameof(SendNotification)}: Failed to send to {result.Failure} recipients. {result.Success} sent successfully.");
			}
			
			return this.Ok();
		}

		/// <summary>
		/// Helper for find if a device/platform combination is already registered.
		/// </summary>
		/// <param name="platform"></param>
		/// <param name="deviceToken"></param>
		/// <returns></returns>
		bool IsDeviceRegistered(NotificationPlatform platform, string deviceToken) => db.CustomDeviceInstallations.Any(e => e.Platform == platform && e.PushChannel == deviceToken);

		/*
		// GET: api/ManagePushDevices/5
		[ResponseType(typeof(CustomDeviceInstallation))]
		public IHttpActionResult GetRegisteredDevice(string installationId)
		{
			var device = this.db.CustomDeviceInstallations.Find(installationId);
			if (device == null)
			{
				return this.NotFound();
			}

			return this.Ok(device);
		}

		
		// PUT: api/ManagePushDevices/5
		[ResponseType(typeof(void))]
		public IHttpActionResult PutPushDeviceInstallation(string id, PushDeviceInstallation pushDeviceInstallation)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (id != pushDeviceInstallation.Id)
			{
				return BadRequest();
			}

			db.Entry(pushDeviceInstallation).State = EntityState.Modified;

			try
			{
				db.SaveChanges();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!PushDeviceInstallationExists(id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return StatusCode(HttpStatusCode.NoContent);
		}

		// POST: api/ManagePushDevices
		[ResponseType(typeof(PushDeviceInstallation))]
		public IHttpActionResult PostPushDeviceInstallation(PushDeviceInstallation pushDeviceInstallation)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			db.PushDeviceInstallations.Add(pushDeviceInstallation);

			try
			{
				db.SaveChanges();
			}
			catch (DbUpdateException)
			{
				if (PushDeviceInstallationExists(pushDeviceInstallation.Id))
				{
					return Conflict();
				}
				else
				{
					throw;
				}
			}

			return CreatedAtRoute("DefaultApi", new { id = pushDeviceInstallation.Id }, pushDeviceInstallation);
		}

		// DELETE: api/ManagePushDevices/5
		[ResponseType(typeof(PushDeviceInstallation))]
		public IHttpActionResult DeletePushDeviceInstallation(string id)
		{
			PushDeviceInstallation pushDeviceInstallation = db.PushDeviceInstallations.Find(id);
			if (pushDeviceInstallation == null)
			{
				return NotFound();
			}

			db.PushDeviceInstallations.Remove(pushDeviceInstallation);
			db.SaveChanges();

			return Ok(pushDeviceInstallation);
		}
		*/

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				db.Dispose();
			}
			base.Dispose(disposing);
		}

		
	}
}