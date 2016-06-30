using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Text;
using System.Diagnostics;
using PushNotificationsClientServerShared;

namespace PushNotificationsClient
{
	public sealed class PushNotificationManager
	{
		public PushNotificationManager (string serverUrl = "http://192.168.178.44:8080")
		{
			this.serverUrl = serverUrl;
		}

		string serverUrl;

		readonly HttpClient client = new HttpClient();

		public async Task<bool> RegisterDeviceAsync(DeviceInformation deviceInfo, CancellationToken token = default(CancellationToken))
		{
			Debug.Assert(deviceInfo != null, "DeviceInfo required");

			var content = new StringContent($"/api/?platform={deviceInfo.Platform.ToString()}&deviceToken={deviceInfo.DeviceToken}", Encoding.UTF8);
			var response = await this.client.PostAsync(this.serverUrl, content, token).ConfigureAwait(false);

			Debug.WriteLineIf(!response.IsSuccessStatusCode, $"Error registering device: {response.ReasonPhrase}");

			return response.IsSuccessStatusCode;
		}

		public async Task<bool> UnregisterDeviceAsync(string deviceToken, CancellationToken token = default(CancellationToken))
		{
			Debug.Assert(deviceToken != null, "Device token required");

			var content = new StringContent($"/api/{deviceToken}", Encoding.UTF8);
			var response = await this.client.PostAsync(this.serverUrl, content, token).ConfigureAwait(false);

			Debug.WriteLineIf(!response.IsSuccessStatusCode, $"Error unregistering device: {response.ReasonPhrase}");

			return response.IsSuccessStatusCode;
		}
	}
}

