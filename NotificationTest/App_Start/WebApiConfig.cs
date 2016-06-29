using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json.Serialization;

namespace NotificationTest
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            // Configure Web API to use only bearer token authentication.
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

			config.Services.Replace(typeof(IHttpControllerSelector), new DebugControllerSelector(config));
        }
    }

	public class DebugControllerSelector : DefaultHttpControllerSelector
	{
		public DebugControllerSelector(HttpConfiguration configuration) : base(configuration)
		{
		}

		public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
		{
			var desc = base.SelectController(request);
			return desc;
		}
	}
}
