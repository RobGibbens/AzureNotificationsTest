namespace PushNotificationsClientServerShared
{
	/// <summary>
	/// Device information.
	/// </summary>
	public class DeviceInformation
	{
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
	}
}

