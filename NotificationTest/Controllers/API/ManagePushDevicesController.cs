using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Results;
using Microsoft.Azure.NotificationHubs;
using NotificationTest.Models;

namespace NotificationTest.Controllers.API
{
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

		readonly NotificationTestContext db = new NotificationTestContext();
		readonly NotificationHubClient notificationHubClient = NotificationHubClient.CreateClientFromConnectionString(
			connectionString: "Endpoint=sb://renepushnotificationnamespace.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=sO84pxrkbJnP0cwR9l6JCCW4mu064eCAwR/v44N6ZT4=",
			notificationHubPath: "RenePushNotificationHub",
			enableTestSend: USE_TEST_SENDING);

		// GET: api/ManagePushDevices
		/*
		public IQueryable<PushDeviceInstallation> GetAllRegisteredDevices()
		{
			return this.db.PushDeviceInstallations;
		}
		*/
		
		/// <summary>
		/// Registers a new device.
		/// </summary>
		/// <param name="platform"></param>
		/// <param name="deviceToken"></param>
		/// <returns>the installation ID of the device. Must be stored by client to allow updating the installation.</returns>
		[ResponseType(typeof(string))]
		public IHttpActionResult PostRegisterNewDevice(string platform, string deviceToken)
		{
			if (string.IsNullOrWhiteSpace(platform))
			{
				return BadRequest("Platform must be specified.");
			}

			if (string.IsNullOrWhiteSpace(deviceToken))
			{
				return BadRequest("Device token must be specified.");
			}

			var installationId = Guid.NewGuid().ToString();

			var parsedPlatform = NotificationPlatform.Adm;
			switch (platform.ToLowerInvariant())
			{
				case "ios":
					parsedPlatform = NotificationPlatform.Apns;
					break;

				case "android":
					parsedPlatform = NotificationPlatform.Gcm;
					break;

				default:
					return BadRequest("Platform must be 'ios' or 'android'");
			}

			// Check the device is not already registered.
			if (this.IsDeviceRegistered(parsedPlatform, deviceToken))
			{
				return this.BadRequest($"A combination of platform '{platform}' and device token '{deviceToken}' is already registered. Did you mean to update your registration?");
			}

			// All good, create a new registration.
			var installation = new PushDeviceInstallation
			{
				Id = installationId,
				Platform = parsedPlatform,
				PushChannel = deviceToken,
			};
			installation.AddDefaultTemplate();

			// Register with Azure Notification Hub.
			this.notificationHubClient.CreateOrUpdateInstallation(installation);

			// Save to local DB. 
			//this.db.PushDeviceInstallations.Add(installation);
			//this.db.SaveChanges();

			return this.Ok(installationId);
		}

		/// <summary>
		/// Sends a notification to all registered devices.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public async Task<IHttpActionResult> PostSendNotification(string message)
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
				Debug.WriteLine($"{nameof(PostSendNotification)}: Failed to send to {result.Failure} recipients. {result.Success} sent successfully.");
			}
			
			return this.Ok();
		}

		/// <summary>
		/// Helper for find if a device/platform combination is already registered.
		/// </summary>
		/// <param name="platform"></param>
		/// <param name="deviceToken"></param>
		/// <returns></returns>
		bool IsDeviceRegistered(NotificationPlatform platform, string deviceToken) => db.PushDeviceInstallations.Any(e => e.Platform == platform && e.PushChannel == deviceToken);

		// GET: api/ManagePushDevices/5
		[ResponseType(typeof(PushDeviceInstallation))]
		public IHttpActionResult GetRegisteredDevice(string installationId)
		{
			var device = this.db.PushDeviceInstallations.Find(installationId);
			if (device == null)
			{
				return this.NotFound();
			}

			return this.Ok(device);
		}

		/*
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