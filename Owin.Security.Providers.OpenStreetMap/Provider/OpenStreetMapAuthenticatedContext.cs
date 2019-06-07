using System.Security.Claims;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Provider;
using Owin.Security.Providers.OpenStreetMap.Messages;

namespace Owin.Security.Providers.OpenStreetMap.Provider
{
    public class OpenStreetMapAuthenticatedContext: BaseContext {
         /// <summary>
        /// Initializes a <see cref="OpenStreetMapAuthenticatedContext"/>
        /// </summary>
        /// <param name="context">The OWIN environment</param>
        /// <param name="accessToken">OpenStreetMap access token</param>
        public OpenStreetMapAuthenticatedContext(IOwinContext context, AccessToken accessToken)
            : base(context)
        {
            UserId = accessToken.UserId;
            UserName = accessToken.UserName;
            AccessToken = accessToken.Token;
            AccessTokenSecret = accessToken.TokenSecret;
        }

        /// <summary>
        /// Gets the OpenStreetMap user ID
        /// </summary>
        public string UserId { get; private set; }

        /// <summary>
        /// Gets the OpenStreetMap username
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// Gets the OpenStreetMap access token
        /// </summary>
        public string AccessToken { get; private set; }

        /// <summary>
        /// Gets the OpenStreetMap access token secret
        /// </summary>
        public string AccessTokenSecret { get; private set; }

        /// <summary>
        /// Gets the <see cref="ClaimsIdentity"/> representing the user
        /// </summary>
        public ClaimsIdentity Identity { get; set; }

        /// <summary>
        /// Gets or sets a property bag for common authentication properties
        /// </summary>
        public AuthenticationProperties Properties { get; set; }
    }
}
