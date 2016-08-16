using System;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlTypes;
using PushNotificationsClientServerShared;
 
namespace PushNotificationsServer.Models
{
	public class DbDeviceInformation : DeviceInformation
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		public DbDeviceInformation()
		{
			this.LastUpdated = DateTime.UtcNow;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="deviceInfo"></param>
		public DbDeviceInformation(IDeviceInformation deviceInfo)
		{
			this.UniqueId = deviceInfo.UniqueId;
			this.DeviceName = deviceInfo.DeviceName;
			this.DeviceToken = deviceInfo.DeviceToken;
			this.Platform = deviceInfo.Platform;
			this.LastUpdated = deviceInfo.LastUpdated;
			if (this.LastUpdated < (DateTime)SqlDateTime.MinValue)
			{
				this.LastUpdated = (DateTime)SqlDateTime.MinValue;
			}
		}

		[Key]
		public override string UniqueId
		{
			get
			{
				return base.UniqueId;
			}

			set
			{
				base.UniqueId = value;
			}
		}
	}
}