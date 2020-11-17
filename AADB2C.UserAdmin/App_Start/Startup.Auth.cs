using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Configuration;
using System.Net;
using System.Threading.Tasks;

namespace AADB2C.UserAdmin
{
    public partial class Startup
    {
		public static string Tenant = ConfigurationManager.AppSettings["ida:Tenant"];
		public static string ClientId = ConfigurationManager.AppSettings["ida:ClientId"];
		public static string RedirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
		public static string WellKnownMetadata = $"{ConfigurationManager.AppSettings["ida:AadInstance"]}/v2.0/.well-known/openid-configuration";
		public static string SignUpSignInPolicyId = ConfigurationManager.AppSettings["ida:SignUpSignInPolicyId"];

		public void ConfigureAuth(IAppBuilder app)
        {
			// Required for Azure webapps, as by default they force TLS 1.2 and this project attempts 1.0
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

			app.UseCookieAuthentication(new CookieAuthenticationOptions
			{
				// ASP.NET web host compatible cookie manager
				CookieManager = new SystemWebChunkingCookieManager()
			});

			app.UseOpenIdConnectAuthentication(
				new OpenIdConnectAuthenticationOptions
				{
					// Generate the metadata address using the tenant and policy information
					MetadataAddress = String.Format(WellKnownMetadata, Tenant, SignUpSignInPolicyId),

					// These are standard OpenID Connect parameters, with values pulled from web.config
					ClientId = ClientId,
					RedirectUri = RedirectUri,
					PostLogoutRedirectUri = RedirectUri,

					// Specify the callbacks for each type of notifications
					Notifications = new OpenIdConnectAuthenticationNotifications
					{
						RedirectToIdentityProvider = OnRedirectToIdentityProvider,
						AuthenticationFailed = OnAuthenticationFailed,
					},

					// Specify the claim type that specifies the Name property.
					TokenValidationParameters = new TokenValidationParameters
					{
						NameClaimType = "name",
						ValidateIssuer = false
					},

					// Specify the scope by appending all of the scopes requested into one string (separated by a blank space)
					Scope = $"openid profile offline_access",

					// ASP.NET web host compatible cookie manager
					CookieManager = new SystemWebCookieManager()
				}
			);
		}

		/*
         *  On each call to Azure AD B2C, check if a policy (e.g. the profile edit or password reset policy) has been specified in the OWIN context.
         *  If so, use that policy when making the call. Also, don't request a code (since it won't be needed).
         */
		private Task OnRedirectToIdentityProvider(RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
		{
			var policy = notification.OwinContext.Get<string>("Policy");

			if (!string.IsNullOrEmpty(policy) && !policy.Equals(SignUpSignInPolicyId))
			{
				notification.ProtocolMessage.Scope = OpenIdConnectScope.OpenId;
				notification.ProtocolMessage.ResponseType = OpenIdConnectResponseType.IdToken;
				notification.ProtocolMessage.IssuerAddress = notification.ProtocolMessage.IssuerAddress.ToLower().Replace(SignUpSignInPolicyId.ToLower(), policy.ToLower());
			}

			return Task.FromResult(0);
		}

		/*
         * Catch any failures received by the authentication middleware and handle appropriately
         */
		private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
		{
			notification.HandleResponse();

			// Handle the error code that Azure AD B2C throws when trying to reset a password from the login page
			// because password reset is not supported by a "sign-up or sign-in policy"
			if (notification.ProtocolMessage.ErrorDescription != null && notification.ProtocolMessage.ErrorDescription.Contains("AADB2C90118"))
			{
				// If the user clicked the reset password link, redirect to the reset password route
				notification.Response.Redirect("/Account/ResetPassword");
			}
			else if (notification.Exception.Message == "access_denied")
			{
				notification.Response.Redirect("/");
			}
			else
			{
				notification.Response.Redirect("/Home/Error?message=" + notification.Exception.Message);
			}

			return Task.FromResult(0);
		}
	}
}
