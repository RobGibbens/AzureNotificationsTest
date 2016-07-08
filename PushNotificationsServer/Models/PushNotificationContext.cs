using System.Data.Entity;
using System.Linq;

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

        public DbSet<CustomDeviceInstallation> CustomDeviceInstallations { get; set; }

		/// <summary>
		/// Helper for find if a device/platform combination is already registered.
		/// </summary>
		/// <param name="uniqueDeviceId"></param>
		/// <returns></returns>
		public bool IsDeviceRegistered(string uniqueDeviceId) => this.CustomDeviceInstallations.Any(e => e.Id == uniqueDeviceId);
	}
}
