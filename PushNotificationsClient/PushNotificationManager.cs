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
		}

		readonly HttpClient client = new HttpClient();

		public async Task<bool> RegisterDeviceAsync(DeviceInformation deviceInfo, CancellationToken token = default(CancellationToken))
		{
			Debug.Assert(deviceInfo != null, "DeviceInfo required");

			var json = JsonConvert.SerializeObject(deviceInfo);

			var request = new HttpRequestMessage(HttpMethod.Post, "api");
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");
			var response = await this.client.SendAsync(request).ConfigureAwait(false);

			Debug.WriteLineIf(!response.IsSuccessStatusCode, $"Error registering device: {response.ReasonPhrase}");

			return response.IsSuccessStatusCode;
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

