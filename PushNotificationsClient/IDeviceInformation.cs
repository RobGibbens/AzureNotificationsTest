using System;

namespace PushNotificationsClientServerShared
{
	/// <summary>
	/// Device information used by the client and the server when registering a device.
	/// </summary>
	public interface IDeviceInformation
	{
		/// <summary>
		/// Unique ID of each device. Use NULL to register a new device.
		/// </summary>
		string UniqueId { get; set; }

		/// <summary>
		/// Gets or sets the device token ("push channel").
		/// </summary>
		string DeviceToken { get; set; }

		/// <summary>
		/// Gets or sets the platform.
		/// </summary>
		Platform Platform { get; set; }

		/// <summary>
		/// Optional device name. Required when registering a new device. 
		/// When updating an existing device, NULL will keep the current name.
		/// </summary>
		string DeviceName { get; set; }
		
		/// <summary>
		/// Stores when this device last registered with the backend.
		/// </summary>
		DateTime? LastUpdated { get; set; }
	}
}