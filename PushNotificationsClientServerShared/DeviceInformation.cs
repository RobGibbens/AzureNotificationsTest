using System;
using System.ComponentModel.DataAnnotations;

namespace PushNotificationsClientServerShared
{
	/// <summary>
	/// Device information. This gets stored in the local DB on the server and is used to register a client device.
	/// </summary>
	public class DeviceInformation : IDeviceInformation
	{
		/// <summary>
		/// Unique ID of each device. Use NULL to register a new device.
		/// </summary>
		public virtual string UniqueId
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
		/// Gets or sets the device token ("push channel").
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

		/// <summary>
		/// Stores when this device last registered with the backend.
		/// </summary>
		public DateTime? LastUpdated { get; set; }

		public override string ToString() => $"[{nameof(DeviceInformation)}] Unique ID = {this.UniqueId}, Platform = {this.Platform}, DeviceToken = {this.DeviceToken}, DeviceName={this.DeviceName}";
	}
}

