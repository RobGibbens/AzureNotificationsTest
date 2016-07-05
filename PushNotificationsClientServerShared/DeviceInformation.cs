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
		public string ID
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the platform.
		/// </summary>
		public PLATFORM Platform
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the device token.
		/// </summary>
		public string DeviceToken
		{
			get;
			set;
		}

		/// <summary>
		/// Optional user data.
		/// </summary>
		public string UserData
		{
			get;
			set;
		}

		public override string ToString()
		{
			return $"[{nameof(DeviceInformation)}] Platform = {this.Platform}, DeviceToken = {this.DeviceToken}, UserData={this.UserData}";
		}
	}
}

