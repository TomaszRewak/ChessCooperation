using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CheesCooperation.Startup))]
namespace CheesCooperation
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
			app.MapSignalR();
        }
    }
}
