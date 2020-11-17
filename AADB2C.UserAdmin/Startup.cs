using System;
using System.Threading.Tasks;
using System.Web.Helpers;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(AADB2C.UserAdmin.Startup))]

namespace AADB2C.UserAdmin
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            AntiForgeryConfig.UniqueClaimTypeIdentifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            ConfigureAuth(app);
        }
    }
}
