// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.OpenStreetMap
{
    /// <summary>
    /// Specifies callback methods which the <see cref="TwitterMiddleware"></see> invokes to enable developer control over the authentication process. />
    /// </summary>
    public interface IOpenStreetMapEvents : IRemoteAuthenticationEvents
    {
        /// <summary>
        /// Invoked whenever Twitter succesfully authenticates a user
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        Task CreatingTicket(OpenStreetMapCreatingTicketContext context);

        /// <summary>
        /// Called when a Challenge causes a redirect to authorize endpoint in the Twitter middleware
        /// </summary>
        /// <param name="context">Contains redirect URI and <see cref="Http.Authentication.AuthenticationProperties"/> of the challenge </param>
        Task RedirectToAuthorizationEndpoint(OpenStreetMapRedirectToAuthorizationEndpointContext context);
    }
}
