using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Azure.NotificationHubs;
using Microsoft.WindowsAzure.Storage.Blob;
using PushNotificationsClientServerShared;
using PushNotificationsServer.Models;

namespace PushNotificationsServer.Controllers.API
{
	/// <summary>
	/// The backend which talks to the Azure Notification Hub.
	/// This is what the client apps communicate with in order to register for push notifications.
	/// The backend is using the "Installation" model and not the "Registration" model.
	/// There is little documentation about the installation model. Some info can be found at https://msdn.microsoft.com/en-us/magazine/dn948105.aspx
	/// The Installation API is alternative mechanism for registration management. Instead of maintaining multiple registrations which is not trivial and may be easily
	/// done wrongly or inefficiently, it is now possible to use SINGLE Installation object.
	/// Installation contains everything you need: push channel (device token), tags, templates, secondary tiles (for WNS and APNS).
	/// You don't need to call thes ervice to get ID anymore - just generate GUID or any other identifier, keep it on device and send to your backend together with push channel (device token).
	/// </summary>
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

		/// <summary>
		/// Change this to the full access connection string of the Notification Hub instance used. This can be found at the Access Policies page of your Azure Notification Hub via portal.azure.com
		/// </summary>
		const string NotificationHubConnectionString = "Endpoint=sb://xamupushnotificationshub.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=ABj04HkW6HFxkV00HXYdEuiArKuE9hllWltmKFBJrAA=";
		
		/// <summary>
		/// Even though this is called a path it is just the name of notification hub used. This can be found via portal.azure.com
		/// </summary>
		const string NotificationHubPath = "XamUPushNotificationsHub";

		/// <summary>
		/// Local database to store information about registered devices.
		/// </summary>
		readonly PushNotificationContext db = new PushNotificationContext();

		/// <summary>
		/// Gives access to the Azure Push Notifications service. From package https://www.nuget.org/packages/Microsoft.Azure.NotificationHubs/
		/// Note: using this client simplifies things a lot compared to the REST API which is also available (https://msdn.microsoft.com/en-us/library/azure/dn495827.aspx)
		/// </summary>
		readonly NotificationHubClient notificationHubClient = NotificationHubClient.CreateClientFromConnectionString(
			connectionString: NotificationHubConnectionString,
			notificationHubPath: NotificationHubPath,
			enableTestSend: USE_TEST_SENDING);

		/// <summary>
		/// Gets all installed devices from the database.
		/// Note: it is not possible to query Azure for installations! "Registrations" can be retrieved, but
		/// we are not using the registration model.
		/// </summary>
		/// <returns>device information</returns>
		[Route("")]
		[HttpGet]
		public IQueryable<IDeviceInformation> GetAllRegisteredDevices() => this.db.RegisteredDevices;

		/// <summary>
		/// The idea behind this method is to get information from Apple, Google etc about expired push channels ("device tokens") so
		/// we can delete them from our local DB. Unfortunately, nobody really seems to know how this works, particularly not if
		/// using the new "Installation" model. So for now (2016-07-21), this is a NOP.
		/// See: http://stackoverflow.com/questions/38108730/how-to-get-all-installations-when-using-azure-notification-hubs-installation-mod
		/// </summary>
		/// <returns></returns>
		[Route("housekeeping")]
		[HttpGet]
		[ResponseType(typeof(IEnumerable<IDeviceInformation>))]
		public async Task<IHttpActionResult> PerformHouseKeepingAsync()
		{
			// To use this API we must use a paid subscription of the Notifiation Hub. As of July 2016, This can only be configured on
			// the old Azure portal at  https://manage.windowsazure.com/portal: In there, select the namespace, select the hub,
			// choose scale tab and select the  “Standard”  tier and save.
			// The returned URL points to a blob container which holds the information about unused devices. We have to get the blob content
			// and parse it (XML). The blob URL is a shared type (https://www.simple-talk.com/cloud/platform-as-a-service/azure-blob-storage-part-9-shared-access-signatures/).
			// The format of the URL look like this: https://pushpnsfb1dc97e338026d3.blob.core.windows.net/00000000002000027386?sv=2015-07-08&sr=c&sig=sl3OaSdUOdcCy7z%2FWeMjHfHmEEuid8lLfn9a8tAHqxE%3D&se=2016-07-16T07:14:49Z&sp=rl
			var uri = await this.notificationHubClient.GetFeedbackContainerUriAsync().ConfigureAwait(false);

			// We can use the Azure Storage Client to get details about the blob.
			var container = new CloudBlobContainer(uri);
			foreach (var blobItem in container.ListBlobs())
			{
				var blob = new CloudBlob(blobItem.Uri);
				using (var blobStream = blob.OpenRead())
				using (var reader = new StreamReader(blobStream))
				{
					var xml = reader.ReadToEnd();

				}
			}

			return this.BadRequest("This method is currently not implemented.");
		}

		/// <summary>
		/// Deletes the installation associated with the ID. Deletes from the Azure Nofification Hub and from the local DB.
		/// </summary>
		/// <param name="uniqueId">unique ID of the device to delete. Note: this is NOT the device token</param>
		/// <returns>NULL if deletion failed, otherwise the deleted device information.</returns>
		[Route("register/{installationId}")]
		[HttpDelete]
		[ResponseType(typeof(IDeviceInformation))]
		public IHttpActionResult UnregisterDevice(string uniqueId)
		{
			if (string.IsNullOrWhiteSpace(uniqueId))
			{
				return this.BadRequest("Installation ID is required.");
			}

			// Delete from Azure.
			// Note: the ID is the GUID the backend assigns to each device. Don't confuse with the device token.
			this.notificationHubClient.DeleteInstallation(uniqueId);

			var deviceInfo = this.db.GetDeviceInfo(uniqueId);
			if (deviceInfo == null)
			{
				return this.NotFound();
			}

			// Delete locally.
			this.db.RegisteredDevices.Remove(deviceInfo);
			this.db.SaveChanges();

			// Return the deleted object.
			return this.Ok(deviceInfo);
		}

		/// <summary>
		/// Registers or updates a device. Uses the "Installation" model.
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
		/// <returns>the <see cref="IDeviceInformation"/> object. The client must store the unique ID.</returns>
		[Route("register")]
		[HttpPost]
		[ResponseType(typeof(DeviceInformation))]
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

			// Check for existing device.
			if (deviceInfo.UniqueId != null && !this.db.IsDeviceRegistered(deviceInfo.UniqueId))
			{
				return this.BadRequest($"Cannot find device to update for ID [{deviceInfo.UniqueId}]. Did you mean to create a new one?");
			}

			if (string.IsNullOrWhiteSpace(deviceInfo.DeviceName))
			{
				return this.BadRequest("Device name is required when registering a new device.");
			}

			if (string.IsNullOrWhiteSpace(deviceInfo.UniqueId))
			{
				deviceInfo.UniqueId = Guid.NewGuid().ToString();
				var installation = new Installation
				{
					// Every installation gets a unique ID.
					InstallationId = deviceInfo.UniqueId,
					Platform = deviceInfo.Platform.ToAzureNotificationPlatform(),

					// The token must be a string of hexadecimal numbers, otherwise registering with Azure will fail.
					PushChannel = deviceInfo.DeviceToken,

					// Using tags: here we add a tag to identify the platform. A tag can be anything that groups devices together.
					//             When sending a templated notification, a tag expressions can be specified. Notifications will then only
					//             be sent to Installations with a matching tag. If multiple tags match, multiple notifications will be sent.
					// See: http://stackoverflow.com/questions/38107932/how-to-correctly-use-the-microsoft-azure-notificationhubs-installation-class/38262225#38262225
					Tags = new List<string> {
						// Tags only allow certain characters: A tag can be any string, up to 120 characters, containing alphanumeric and the following non-alphanumeric characters: ‘_’, ‘@’, ‘#’, ‘.’, ‘:’, ‘-’. 
						// Remember the platform as a tag.
						$"platform-{deviceInfo.Platform.ToString()}",
					}
				};

				// Generate the templates we use for sending.
				installation.AddOrUpdateTemplates();

				// Register with Azure Notification Hub.
				// Note: even though this is supposed to update an existing installation, it fails with a SocketException...work in progress, I guess.
				this.notificationHubClient.CreateOrUpdateInstallation(installation);
			}

			// Save to local DB. 
			deviceInfo.LastUpdated = DateTime.UtcNow;
			this.db.RegisteredDevices.AddOrUpdate(new DbDeviceInformation(deviceInfo));
			this.db.SaveChanges();

			// Return the device info, now with updated fields.
			return this.Ok(deviceInfo);
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

			var senderInstallation = this.db.GetDeviceInfo(sendData.SenderId);
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
				properties: new Dictionary<string, string>
				{
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