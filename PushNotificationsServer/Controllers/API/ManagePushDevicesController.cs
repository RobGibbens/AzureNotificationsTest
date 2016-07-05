using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
		/// Gets all registered devices.
		/// </summary>
		/// <returns></returns>
		[Route("")]
		[HttpGet]
		public IQueryable<CustomDeviceInstallation> GetAllRegisteredDevices()
		{
			return this.db.CustomDeviceInstallations;
		}

		/// <summary>
		/// Registers a new device or updates and existing one.
		/// 
		/// Headers:
		/// Accept:application/json
		/// Content-Type:application/json
		/// 
		/// Body:
		/// {
		///		"Platform": 0,
		///		"DeviceToken": "ABC123",
		///		"UserData": "Hello World!"
		/// }
		/// </summary>
		/// <param name="platform"></param>
		/// <param name="deviceToken"></param>
		/// <returns>the installation ID of the device. Must be stored by client to allow updating the installation.</returns>
		[Route("")]
		[HttpPost]
		[ResponseType(typeof(string))]
		public IHttpActionResult RegisterDevice(DeviceInformation deviceInfo)
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

			if (deviceInfo.ID != null)
			{
				// Check for existing device.
				installation = this.db.CustomDeviceInstallations.Where(d => d.InstallationId == deviceInfo.ID).FirstOrDefault();
			}
			else
			{
				installation = new CustomDeviceInstallation();
				installation.Id = Guid.NewGuid().ToString(); 
			}

			// Convert out platform information over to Azure's NotificationPlatform.
			var parsedPlatform = NotificationPlatform.Adm;
			switch (deviceInfo.Platform)
			{
				case PLATFORM.iOS:
					parsedPlatform = NotificationPlatform.Apns;
					break;

				case PLATFORM.Android:
					parsedPlatform = NotificationPlatform.Gcm;
					break;

				default:
					return BadRequest("Platform must be iOS or Android.");
			}

			installation.Platform = parsedPlatform;
			// The token must be a string of hexadecimal numbers, otherwise registering with Azure will fail.
			installation.PushChannel = deviceInfo.DeviceToken;
			installation.LastUpdated = DateTime.UtcNow;
			installation.AddOrUpdateDefaultTemplate();

			// Save to local DB. 
			this.db.CustomDeviceInstallations.Add(installation);
			this.db.SaveChanges();

			// Register with Azure Notification Hub.
			this.notificationHubClient.CreateOrUpdateInstallation(installation);

			// Return the unique ID.
			return this.Ok(installation.Id);
		}

		/// <summary>
		/// Sends a notification to all registered devices.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		[Route("")]
		[HttpPost]
		public async Task<IHttpActionResult> SendNotification(string message)
		{
			var result = await this.notificationHubClient.SendTemplateNotificationAsync(
				// Set placeholders of templates.
				properties:	new Dictionary<string, string> {
					["message"] = message
				},
				// No specific tags to send to.
				tagExpression: null
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