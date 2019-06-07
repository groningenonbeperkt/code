using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Provider;
namespace Owin.Security.Providers.OpenStreetMap.Provider
{
    public class OpenStreetMapReturnEndpointContext: ReturnEndpointContext {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context">OWIN environment</param>
        /// <param name="ticket">The authentication ticket</param>
        public OpenStreetMapReturnEndpointContext(
            IOwinContext context,
            AuthenticationTicket ticket)
            : base(context, ticket) {
        }
    }
}
