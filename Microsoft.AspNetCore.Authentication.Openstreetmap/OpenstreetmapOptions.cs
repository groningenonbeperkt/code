// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Openstreetmap
{
    /// <summary>
    /// Options for the Openstreetmap authentication handler.
    /// </summary>
    public class OpenstreetmapOptions : RemoteAuthenticationOptions
    {
        private const string DefaultStateCookieName = "__OpenstreetmapState";

        private CookieBuilder _stateCookieBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenstreetmapOptions"/> class.
        /// </summary>
        public OpenstreetmapOptions()
        {
            CallbackPath = new PathString("/signin-Openstreetmap");
            BackchannelTimeout = TimeSpan.FromSeconds(60);
            Events = new OpenstreetmapEvents();

            UseDevelopmentApi = false;

            ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);

            _stateCookieBuilder = new OpenstreetmapCookieBuilder(this)
            {
                Name = DefaultStateCookieName,
                SecurePolicy = CookieSecurePolicy.SameAsRequest,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
            };
        }

        /// <summary>
        /// Gets or sets the consumer key used to communicate with Openstreetmap.
        /// </summary>
        /// <value>The consumer key used to communicate with Openstreetmap.</value>
        public string ConsumerKey { get; set; }

        /// <summary>
        /// Gets or sets the consumer secret used to sign requests to Openstreetmap.
        /// </summary>
        /// <value>The consumer secret used to sign requests to Openstreetmap.</value>
        public string ConsumerSecret { get; set; }

        /// <summary>
        /// Enables the retrieval user details during the authentication process, including
        /// e-mail addresses. Retrieving e-mail addresses requires special permissions
        /// from Openstreetmap Support on a per application basis. The default is false.
        /// See https://dev.Openstreetmap.com/rest/reference/get/account/verify_credentials
        /// </summary>
        public bool RetrieveUserDetails { get; set; }

        /// <summary>
        /// Sets whether to use the Openstreetmap development api. Defaults to false.
        /// </summary>
        public bool UseDevelopmentApi { get; set; }

        /// <summary>
        /// A collection of claim actions used to select values from the json user data and create Claims.
        /// </summary>
        public ClaimActionCollection ClaimActions { get; } = new ClaimActionCollection();

        /// <summary>
        /// Gets or sets the type used to secure data handled by the handler.
        /// </summary>
        public ISecureDataFormat<RequestToken> StateDataFormat { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="OpenstreetmapEvents"/> used to handle authentication events.
        /// </summary>
        public new OpenstreetmapEvents Events
        {
            get => (OpenstreetmapEvents)base.Events;
            set => base.Events = value;
        }

        /// <summary>
        /// Determines the settings used to create the state cookie before the
        /// cookie gets added to the response.
        /// </summary>
        public CookieBuilder StateCookie
        {
            get => _stateCookieBuilder;
            set => _stateCookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
        }

        private class OpenstreetmapCookieBuilder : CookieBuilder
        {
            private readonly OpenstreetmapOptions _OpenstreetmapOptions;

            public OpenstreetmapCookieBuilder(OpenstreetmapOptions OpenstreetmapOptions)
            {
                _OpenstreetmapOptions = OpenstreetmapOptions;
            }

            public override CookieOptions Build(HttpContext context, DateTimeOffset expiresFrom)
            {
                var options = base.Build(context, expiresFrom);
                if (!Expiration.HasValue)
                {
                    options.Expires = expiresFrom.Add(_OpenstreetmapOptions.RemoteAuthenticationTimeout);
                }
                return options;
            }
        }
    }
}
