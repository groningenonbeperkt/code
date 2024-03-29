// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.OpenStreetMap;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to add Twitter authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class OpenStreetMapAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="TwitterMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Twitter authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseOpenStreetMapAuthentication(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<OpenStreetMapMiddleware>();
        }

        /// <summary>
        /// Adds the <see cref="TwitterMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Twitter authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">An action delegate to configure the provided <see cref="OpenStreetMapOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseOpenStreetMapAuthentication(this IApplicationBuilder app, OpenStreetMapOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<OpenStreetMapMiddleware>(Options.Create(options));
        }
    }
}
