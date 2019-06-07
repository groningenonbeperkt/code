using Microsoft.Owin.Security;

namespace Owin.Security.Providers.OpenStreetMap.Messages
{
    /// <summary>
    /// OpenStreetMap request token
    /// </summary>
    public class RequestToken
    {
        /// <summary>
        /// Gets or sets the OpenStreetMap token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the OpenStreetMap token secret
        /// </summary>
        public string TokenSecret { get; set; }

        public bool CallbackConfirmed { get; set; }

        /// <summary>
        /// Gets or sets a property bag for common authentication properties
        /// </summary>
        public AuthenticationProperties Properties { get; set; }
    }
}
