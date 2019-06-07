// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.Openstreetmap
{
    /// <summary>
    /// Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.
    /// </summary>
    public class OpenstreetmapCreatingTicketContext : ResultContext<OpenstreetmapOptions>
    {
        /// <summary>
        /// Initializes a <see cref="OpenstreetmapCreatingTicketContext"/>
        /// </summary>
        /// <param name="context">The HTTP environment</param>
        /// <param name="scheme">The scheme data</param>
        /// <param name="options">The options for Openstreetmap</param>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/>.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
        /// <param name="userId">Openstreetmap user ID</param>
        /// <param name="screenName">Openstreetmap screen name</param>
        /// <param name="accessToken">Openstreetmap access token</param>
        /// <param name="accessTokenSecret">Openstreetmap access token secret</param>
        /// <param name="user">User details</param>
        public OpenstreetmapCreatingTicketContext(
            HttpContext context,
            AuthenticationScheme scheme,
            OpenstreetmapOptions options,
            ClaimsPrincipal principal,
            AuthenticationProperties properties,
            string userId,
            string screenName,
            string accessToken,
            string accessTokenSecret,
            JObject user)
            : base(context, scheme, options)
        {
            UserId = userId;
            ScreenName = screenName;
            AccessToken = accessToken;
            AccessTokenSecret = accessTokenSecret;
            User = user ?? new JObject();
            Principal = principal;
            Properties = properties;
        }

        /// <summary>
        /// Gets the Openstreetmap user ID
        /// </summary>
        public string UserId { get; }

        /// <summary>
        /// Gets the Openstreetmap screen name
        /// </summary>
        public string ScreenName { get; }

        /// <summary>
        /// Gets the Openstreetmap access token
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// Gets the Openstreetmap access token secret
        /// </summary>
        public string AccessTokenSecret { get; }

        /// <summary>
        /// Gets the JSON-serialized user or an empty
        /// <see cref="JObject"/> if it is not available.
        /// </summary>
        public JObject User { get; }
    }
}
