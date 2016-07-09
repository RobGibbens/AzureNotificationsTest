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
					install.Platform == NotificationPlatform.Apns ? Platform.iOS :
					install.Platform == NotificationPlatform.Gcm ? Platform.Android :
					Platform.Unknown,
				DeviceName = install.DeviceName
			});

			return result;
		}
		

		/// <summary>
		/// Deletes the installation associated with the ID.
		/// </summary>
		/// <param name="installationId">unique ID of the device to delete. Note: this is NOT the device token</param>
		/// <returns>NULL if deletion failed, otherwise the deleted device information.</returns>
		[Route("register/{installationId}")]
		[HttpDelete]
		[ResponseType(typeof(DeviceInformation))]
		public IHttpActionResult UnregisterDevice(string installationId)
		{
			if (string.IsNullOrWhiteSpace(installationId))
			{
				return this.BadRequest("Installation ID is required.");
			}

			// Delete from Azure.
			// Note: the ID is the GUID the backend assigns to each device. Don't confuse with the device token.
			this.notificationHubClient.DeleteInstallation(installationId);

			var installation = this.db.GetInstallation(installationId);
			if (installation == null)
			{
				return this.NotFound();
			}
			
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
			installation.DeviceName = deviceInfo.DeviceName;
			installation.LastUpdated = DateTime.UtcNow;
			// Using tags: here we add a tag to identify the platform. A tag can be anything that groups devices together.
			//             When sending a templated notification a tag expressions can be specified. Notifications will then only
			//             be sent to Installations with a matching tag. If multiple tags match, multiple notifications will be sent.
			// See: http://stackoverflow.com/questions/38107932/how-to-correctly-use-the-microsoft-azure-notificationhubs-installation-class/38262225#38262225
			installation.Tags = new List<string> {
				// Tags only allow certain characters: A tag can be any string, up to 120 characters, containing alphanumeric and the following non-alphanumeric characters: ‘_’, ‘@’, ‘#’, ‘.’, ‘:’, ‘-’. 
				// Remember the platform as a tag.
				$"platform-{deviceInfo.Platform.ToString()}",
				// Remember the device ID as tag. This allows sending to specific devices easily.
			};
			installation.AddOrUpdateTemplates();

			// Save to local DB. 
			this.db.CustomDeviceInstallations.AddOrUpdate(installation);
			this.db.SaveChanges();

			// Register with Azure Notification Hub.
			this.notificationHubClient.CreateOrUpdateInstallation(installation);

			// Return the unique ID.
			return this.Ok(installation.Id);
		}

		/// <summary>
		/// Sends a notification.
		/// The sender ID is the unique ID of the device and must be set and valid.
		/// The message must not be empty.
		/// The target platforms can NULL to send to all platforms.
		/// 
		/// Headers:
		/// Accept:application/json
		/// Content-Type:application/json
		/// 
		/// Body:
		/// {
		///		"SenderId": "6385d53f-c515-443a-8c62-898914d2bb4e",
		///		"Message": "Test Message",
		///		"Template" : 0,
		///		"RecipientId" : null
		/// }
		/// </summary>
		/// <param name="sendData">message to send</param>
		/// <returns>If the sender is invalid, returns a 404.</returns>
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

			if (!this.db.IsDeviceRegistered(sendData.SenderId))
			{
				return this.BadRequest($"Cannot find a registered device for ID '{sendData.SenderId}'");
			}

			if (sendData.RecipientId != null && !this.db.IsDeviceRegistered(sendData.RecipientId))
			{
				return this.NotFound();
			}

			var senderInstallation = this.db.GetInstallation(sendData.SenderId);
			Debug.Assert(senderInstallation != null, "Should never be NULL if we get here because IsDeviceRegistered() returned TRUE.");

			string tags = string.Empty;

			// Pick an individual recipient. Notification hub supports a special syntax to select an installation ID.
			if (sendData.RecipientId != null)
			{
				tags = "$InstallationId:{" + sendData.RecipientId + "}";
			}

			// Use tag expressions to select the right template. This is matched against the templates in the CustomDeviceInstallation.
			if (tags.Length > 0)
			{
				tags += "&&";
			}
			tags += $"template-{sendData.Template.ToString()}";

			var result = await this.notificationHubClient.SendTemplateNotificationAsync(
				// Set placeholders of templates.
				properties:	new Dictionary<string, string> {
					["sender"] = senderInstallation.DeviceName,
					["message"] = sendData.Message
				},
				// This filters for tags specified for the Installation object and not for tags specified in the Installation object's templates.
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
		/// Returns if a device has been registered for a given ID.
		/// </summary>
		/// <param name="uniqueDeviceId"></param>
		/// <returns></returns>
		[Route("register/{uniqueDeviceId}")]
		[HttpGet]
		[ResponseType(typeof(bool))]
		public IHttpActionResult IsDeviceRegistered(string uniqueDeviceId)
		{
			if (string.IsNullOrWhiteSpace(uniqueDeviceId))
			{
				return this.BadRequest("Invalid device ID.");
			}

			var exists = this.db.IsDeviceRegistered(uniqueDeviceId);

			return this.Ok(exists);
		}
		
	
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