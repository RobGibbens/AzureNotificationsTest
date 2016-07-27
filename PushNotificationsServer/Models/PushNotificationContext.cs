using System.Data.Entity;
using System.Linq;
using PushNotificationsClientServerShared;

namespace PushNotificationsServer.Models
{
	public class PushNotificationContext : DbContext
    {
        // You can add custom code to this file. Changes will not be overwritten.
        // 
        // If you want Entity Framework to drop and regenerate your database
        // automatically whenever you change your model schema, please use data migrations.
        // For more information refer to the documentation:
        // http://msdn.microsoft.com/en-us/data/jj591621.aspx
    
        public PushNotificationContext() : base("name=PushNotificationContext")
        {
        }

        public DbSet<DbDeviceInformation> RegisteredDevices { get; set; }

		/// <summary>
		/// Helper for find if a device/platform combination is already registered.
		/// </summary>
		/// <param name="uniqueDeviceId"></param>
		/// <returns></returns>
		public bool IsDeviceRegistered(string uniqueDeviceId) => this.RegisteredDevices.Any(e => e.UniqueId == uniqueDeviceId);

		/// <summary>
		/// Gets a device info by its unique device ID.
		/// </summary>
		/// <param name="uniqueId"></param>
		/// <returns></returns>
		public DbDeviceInformation GetDeviceInfo(string uniqueId) => this.RegisteredDevices.FirstOrDefault(d => d.UniqueId == uniqueId);

	}
}
