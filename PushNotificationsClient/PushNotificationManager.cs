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
	public sealed class PushNotificationManager
	{
		public PushNotificationManager (string serverUrl = "http://192.168.178.44:8080")
		{
			this.client.BaseAddress = new Uri(serverUrl);
			this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			this.client.Timeout = TimeSpan.FromSeconds(120);
		}

		readonly HttpClient client = new HttpClient();

		public async Task<string> RegisterOrUpdateDeviceAsync(DeviceInformation deviceInfo, CancellationToken token = default(CancellationToken))
		{
			Debug.Assert(deviceInfo != null, "DeviceInfo required");

			var json = JsonConvert.SerializeObject(deviceInfo);

			var request = new HttpRequestMessage(HttpMethod.Post, "api/register");
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");
			var response = await this.client.SendAsync(request, token).ConfigureAwait(false);

			Debug.WriteLineIf(!response.IsSuccessStatusCode, $"[{nameof(RegisterOrUpdateDeviceAsync)}] Error registering device: {response.ReasonPhrase}");

			var installationId = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			// Json-encoded string is expected as the return value.
			string ret = null;
			try
			{
				ret = JsonConvert.DeserializeObject<string>(installationId);
			}
			catch(Exception ex)
			{
				Debug.WriteLine($"[{nameof(RegisterOrUpdateDeviceAsync)}] Failed to deserialize return value: '{installationId}'; {ex}");
			}
			return ret;
		}

		/*
		public async Task<bool> UnregisterDeviceAsync(string deviceToken, CancellationToken token = default(CancellationToken))
		{
			Debug.Assert(deviceToken != null, "Device token required");


			Debug.WriteLineIf(!response.IsSuccessStatusCode, $"Error unregistering device: {response.ReasonPhrase}");

			return response.IsSuccessStatusCode;
		}
		*/
	}
}

