// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenStreetMap;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Options for the Twitter authentication middleware.
    /// </summary>
    public class OpenStreetMapOptions : RemoteAuthenticationOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenStreetMapOptions"/> class.
        /// </summary>
        public OpenStreetMapOptions()
        {
            AuthenticationScheme = OpenStreetMapDefaults.AuthenticationScheme;
            DisplayName = AuthenticationScheme;
            CallbackPath = new PathString("/signin-openstreetmap");
            BackchannelTimeout = TimeSpan.FromSeconds(60);
            Events = new OpenStreetMapEvents();
            UseDevelopmentUrls = false;
        }

        /// <summary>
        /// Gets or sets the consumer key used to communicate with Twitter.
        /// </summary>
        /// <value>The consumer key used to communicate with Twitter.</value>
        public string ConsumerKey { get; set; }

        /// <summary>
        /// Gets or sets the consumer secret used to sign requests to Twitter.
        /// </summary>
        /// <value>The consumer secret used to sign requests to Twitter.</value>
        public string ConsumerSecret { get; set; }

        /// <summary>
        /// Enables the retrieval user details during the authentication process, including
        /// e-mail addresses. Retrieving e-mail addresses requires special permissions
        /// from Twitter Support on a per application basis. The default is false.
        /// See https://dev.twitter.com/rest/reference/get/account/verify_credentials
        /// </summary>
        public bool RetrieveUserDetails { get; set; }

        /// <summary>
        /// Gets or sets the type used to secure data handled by the middleware.
        /// </summary>
        public ISecureDataFormat<RequestToken> StateDataFormat { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ITwitterEvents"/> used to handle authentication events.
        /// </summary>
        public new IOpenStreetMapEvents Events
        {
            get { return (IOpenStreetMapEvents)base.Events; }
            set { base.Events = value; }
        }

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ISystemClock SystemClock { get; set; } = new SystemClock();

        public bool UseDevelopmentUrls
        {
            get;
            set;
        }
    }
}
