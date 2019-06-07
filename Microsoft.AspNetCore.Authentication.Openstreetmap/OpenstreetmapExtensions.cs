// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Openstreetmap;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenstreetmapExtensions
    {
        public static AuthenticationBuilder AddOpenstreetmap(this AuthenticationBuilder builder)
            => builder.AddOpenstreetmap(OpenstreetmapDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddOpenstreetmap(this AuthenticationBuilder builder, Action<OpenstreetmapOptions> configureOptions)
            => builder.AddOpenstreetmap(OpenstreetmapDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddOpenstreetmap(this AuthenticationBuilder builder, string authenticationScheme, Action<OpenstreetmapOptions> configureOptions)
            => builder.AddOpenstreetmap(authenticationScheme, OpenstreetmapDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddOpenstreetmap(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<OpenstreetmapOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<OpenstreetmapOptions>, OpenstreetmapPostConfigureOptions>());
            return builder.AddRemoteScheme<OpenstreetmapOptions, OpenstreetmapHandler>(authenticationScheme, displayName, configureOptions);
        }
    }
}
