using System;

namespace Owin.Security.Providers.OpenStreetMap
{
    public static class OpenStreetMapAuthenticationExtensions
    {
        public static IAppBuilder UseOpenStreetMapAuthentication(this IAppBuilder app, OpenStreetMapAuthenticationOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            app.Use(typeof(OpenStreetMapAuthenticationMiddleware), app, options);

            return app;
        }

        public static IAppBuilder UseOpenStreetMapAuthentication(this IAppBuilder app, string appKey, string appSecret)
        {
            return app.UseOpenStreetMapAuthentication(new OpenStreetMapAuthenticationOptions
            {
                AppKey = appKey,
                AppSecret = appSecret
            });
        }

    }
}
