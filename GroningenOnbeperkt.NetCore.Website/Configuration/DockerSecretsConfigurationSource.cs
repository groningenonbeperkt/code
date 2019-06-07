using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GroningenOnbeperkt.NetCore.Website.Configuration
{
    public class DockerSecretsConfigurationSource : FileConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            FileProvider = FileProvider ?? builder.GetFileProvider();
            return new DockerSecretsConfigurationProvider(this);
        }
    }
}
