// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.OpenStreetMap
{
    /// <summary>
    /// Base class for other Twitter contexts.
    /// </summary>
    public class BaseOpenStreetMapContext : BaseContext
    {
        /// <summary>
        /// Initializes a <see cref="BaseTwitterContext"/>
        /// </summary>
        /// <param name="context">The HTTP environment</param>
        /// <param name="options">The options for Twitter</param>
        public BaseOpenStreetMapContext(HttpContext context, OpenStreetMapOptions options)
            : base(context)
        {
            Options = options;
        }

        public OpenStreetMapOptions Options { get; }
    }
}
