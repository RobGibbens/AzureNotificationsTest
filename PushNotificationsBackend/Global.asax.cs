using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using PushNotificationsBackend.Models;
using PushNotificationsClientServerShared;

namespace PushNotificationsBackend
{
	public class CustomDbInitializer : DropCreateDatabaseAlways<PushNotificationContext>
	{
		protected override void Seed(PushNotificationContext context)
		{
			// Always initialize with a device that can be used to test sending.
			var sendTestDevice = new DbDeviceInformation
			{
				DeviceName = "Send Test Device",
				UniqueId = "sendtest",
				DeviceToken = "sendtest",
				Platform = Platform.Unknown
			};
			context.RegisteredDevices.Add(sendTestDevice);

			base.Seed(context);
		}
	}


	public class WebApiApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
#if DEBUG
			// Always regenerate DB while in debug mode.
			Database.SetInitializer(new CustomDbInitializer());
#endif

			// The project has a folder "API" and we are using attribute driven routing to "api" in the
			// PushNotificationsController. However if there is a physical folders that matches the route,
			// the attribute based ones will be ignored. We can disable this behavior with the call below.
			// See also: http://www.grumpydev.com/2013/09/17/403-14-error-when-trying-to-access-a-webapi-route/
			RouteTable.Routes.RouteExistingFiles = true;

			GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
