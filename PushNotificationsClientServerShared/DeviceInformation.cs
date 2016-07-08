using System.ComponentModel.DataAnnotations;

namespace PushNotificationsClientServerShared
{
	/// <summary>
	/// Device information.
	/// </summary>
	public class DeviceInformation
	{
		/// <summary>
		/// Unique ID of each device. Use NULL to register a new device.
		/// </summary>
		public string Id
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the platform.
		/// </summary>
		[Required]
		public Platform Platform
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the device token.
		/// </summary>
		[Required]
		public string DeviceToken
		{
			get;
			set;
		}

		/// <summary>
		/// Optional device name. Required when registering a new device. 
		/// When updating an existing device, NULL will keep the current name.
		/// </summary>
		public string DeviceName
		{
			get;
			set;
		}

		public override string ToString() => $"[{nameof(DeviceInformation)}] Platform = {this.Platform}, DeviceToken = {this.DeviceToken}, DeviceName={this.DeviceName}";
	}
}

