using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;

namespace GroningenOnbeperkt.NetCore.Website.Configuration
{
    public static class DockerSecretsExtensionMethods
    {
        public static IConfigurationBuilder AddDockerSecrets(this IConfigurationBuilder builder)
        {
            var source = new DockerSecretsConfigurationSource
            {
                FileProvider = null,
                Path = string.Empty,
                Optional = true,
                ReloadOnChange = false
            };

            builder.Add(source);

            return builder;
        }
    }
}
