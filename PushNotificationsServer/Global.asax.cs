using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using PushNotificationsClientServerShared;
using PushNotificationsServer.Models;

namespace PushNotificationsServer
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


	public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
#if DEBUG
			// Always regenerate DB while in debug mode.
			Database.SetInitializer(new CustomDbInitializer());
#endif

			AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
