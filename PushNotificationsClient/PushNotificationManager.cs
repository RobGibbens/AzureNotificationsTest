using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Text;
using System.Diagnostics;
using PushNotificationsClientServerShared;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace PushNotificationsClient
{
	/// <summary>
	/// Communicates with the backend to allow device registration and notification sending.
	/// </summary>
	public sealed class PushNotificationManager
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="serverUrl">The base URL of the backend (e.g. http://192.168.178.44:8080)</param>
		public PushNotificationManager (string serverUrl)
		{
			this.client.BaseAddress = new Uri(serverUrl);
			this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			this.client.Timeout = TimeSpan.FromSeconds(120);
		}

		readonly HttpClient client = new HttpClient();

		/// <summary>
		/// Registers or updates a device.
		/// </summary>
		/// <returns>Deice information. NULL if registration fails.</returns>
		/// <param name="deviceInfo">Device info. For new registrations the DeviceInformation.Id property must be NULL.</param>
		/// <param name="token">Token.</param>
		/// <exception cref="System.OperationCanceledException">if cancellation was requested</exception>
		public async Task<DeviceInformation> RegisterOrUpdateDeviceAsync(DeviceInformation deviceInfo, CancellationToken token = default(CancellationToken))
		{
			Debug.Assert(deviceInfo != null, "DeviceInfo required");

			var json = JsonConvert.SerializeObject(deviceInfo);

			var response = await this.SendHttpRequestAsync(HttpMethod.Post, "api/register", new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(false);

			Debug.WriteLineIf(!response.IsSuccessStatusCode, $"[{nameof(RegisterOrUpdateDeviceAsync)}] Error registering device: {response.ReasonPhrase}");

			var ret = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			// Json-encoded string is expected as the return value.
			DeviceInformation updatedDeviceInfo = null;
			try
			{
				updatedDeviceInfo = JsonConvert.DeserializeObject<DeviceInformation>(ret);
			}
			catch(Exception ex)
			{
				Debug.WriteLine($"[{nameof(RegisterOrUpdateDeviceAsync)}] Failed to deserialize return value: '{ret}'; {ex}");
			}
			return updatedDeviceInfo;
		}

		/// <summary>
		/// Ises the device registered async.
		/// </summary>
		/// <returns>The device registered async.</returns>
		/// <param name="uniqueDeviceId">Unique device identifier.</param>
		/// <param name="token">Token.</param>
		public async Task<bool> IsDeviceRegisteredAsync(string uniqueDeviceId, CancellationToken token = default(CancellationToken))
		{
			Debug.Assert(!string.IsNullOrWhiteSpace(uniqueDeviceId), "Device ID is required!");

			var response = await this.SendHttpRequestAsync(HttpMethod.Get, $"api/register/{uniqueDeviceId}", null, token).ConfigureAwait(false);

			Debug.WriteLineIf(!response.IsSuccessStatusCode, $"[{nameof(IsDeviceRegisteredAsync)}] Error checking if device is registered: {response.ReasonPhrase}");

			if(!response.IsSuccessStatusCode)
			{
				throw new InvalidOperationException($"Failed to check device ID {uniqueDeviceId}");
			}

			var ret = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			var result = JsonConvert.DeserializeObject<bool>(ret);

			return result;
		}

		/// <summary>
		/// Unregisters a device.
		/// </summary>
		/// <returns>information about the deleted device</returns>
		/// <param name="uniqueDeviceId">Unique device identifier</param>
		/// <param name="token">Token</param>
		public async Task<DeviceInformation> UnregisterDeviceAsync(string uniqueDeviceId, CancellationToken token = default(CancellationToken))
		{
			Debug.Assert(uniqueDeviceId != null, "Device ID required");

			var response = await this.SendHttpRequestAsync(HttpMethod.Delete, $"api/register/{uniqueDeviceId}", null, token).ConfigureAwait(false);

			Debug.WriteLineIf(!response.IsSuccessStatusCode, $"[{nameof(UnregisterDeviceAsync)}] Error unregisterings device: {response.ReasonPhrase}");

			if(!response.IsSuccessStatusCode)
			{
				return null;
			}

			var ret = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			var result = JsonConvert.DeserializeObject<DeviceInformation>(ret);

			return result;
		}

		/// <summary>
		/// Sends a notification.
		/// </summary>
		/// <param name="senderId">Sender ID. Required. This is the unique ID and not the device token.</param>
		/// <param name="message">Message. If null or empty, sending will fail.</param>
		/// <param name="token">Token.</param>
		/// <param name="template">Template to use.</param>
		/// <param name="recipientId">Individual recipient. Use the device's unique ID. Using NULL will send to all.</param>
		public async Task<bool> SendNotificationAsync(string senderId, string message, CancellationToken token = default(CancellationToken), NotificationTemplate template = NotificationTemplate.Neutral, string recipientId = null)
		{
			if(string.IsNullOrWhiteSpace(senderId))
			{
				throw new InvalidOperationException("No sender ID set.");
			}

			if(string.IsNullOrEmpty(message))
			{
				return false;
			}

			var sendData = new SendData
			{
				SenderId = senderId,
				Message = message,
				RecipientId = recipientId,
				Template = template
			};
			var json = JsonConvert.SerializeObject(sendData);

			var response = await this.SendHttpRequestAsync(HttpMethod.Post, "api/send", new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(false);

			Debug.WriteLineIf(!response.IsSuccessStatusCode, $"[{nameof(SendNotificationAsync)}] Error sending notification: {response.ReasonPhrase}");

			return response.IsSuccessStatusCode;
		}

		/// <summary>
		/// Helper to send a request.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="method">Method.</param>
		/// <param name="url">URL.</param>
		/// <param name="content">Content.</param>
		/// <param name="token">Token.</param>
		async Task<HttpResponseMessage> SendHttpRequestAsync(HttpMethod method, string url, HttpContent content, CancellationToken token = default(CancellationToken))
		{
			var request = new HttpRequestMessage(method, url);
			request.Content = content;
			var response = await this.client.SendAsync(request, token).ConfigureAwait(false);
			return response;
		}
	}
}

