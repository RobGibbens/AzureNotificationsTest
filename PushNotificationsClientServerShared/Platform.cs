namespace PushNotificationsClientServerShared
{
	/// <summary>
	/// Supported client platforms.
	/// </summary>
	public enum Platform
	{
		Unknown = -1,
		/// <summary>
		/// Registers an iOS device or can be used to limit sending to iOS devices only.
		/// </summary>
		iOS,
		/// <summary>
		/// Registers an Android device or can be used to limit sending to Android devices only.
		/// </summary>
		Android
	}
}

