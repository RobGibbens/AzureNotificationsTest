using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PushNotificationsServer.Startup))]
namespace PushNotificationsServer
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
