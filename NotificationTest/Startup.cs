using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(NotificationTest.Startup))]
namespace NotificationTest
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
