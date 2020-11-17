using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Web.Mvc;
using System.Web.Mvc.Filters;

namespace AADB2C.UserAdmin.Controllers
{
    public class BaseController : Controller
    {
        public CurrentUser CurrentUser { get; set; }

        protected override void OnAuthentication(AuthenticationContext filterContext)
        {
            base.OnAuthentication(filterContext);
            CurrentUser = new CurrentUser(filterContext.Controller.ControllerContext.HttpContext.User);
        }
    }

    public class CurrentUser
    {
        private ClaimsPrincipal _claimsPrincipal;
        private readonly string _organization;
        private readonly string _userRole;

        private CurrentUser() { }

        internal CurrentUser(IPrincipal user)
        {
            if (user.Identity.IsAuthenticated)
            {
                _claimsPrincipal = (ClaimsPrincipal)user;
                foreach (Claim claim in _claimsPrincipal.Claims)
                {
                    if (claim.Type == "extension_Organization") _organization = claim.Value;
                    if (claim.Type == "extension_UserRole") _userRole = claim.Value;
                }
            }
        }

        public IEnumerable<Claim> Claims
        {
            get
            {
                return _claimsPrincipal.Claims;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                if (_claimsPrincipal == null) return false;
                else return true;
            }
        }

        public IIdentity Identity
        {
            get
            {
                if (_claimsPrincipal == null) return null;
                else return _claimsPrincipal.Identity;
            }
        }

        public string Organization
        {
            get
            {
                return _organization;
            }
        }

        public string UserRole
        {
            get
            {
                return _userRole;
            }
        }

        public bool IsInRole(string roleName)
        {
            if (_claimsPrincipal == null) return false;
            else return _userRole.ToLower() == roleName.ToLower();
        }
    }
}