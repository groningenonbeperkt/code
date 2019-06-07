using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GroningenOnbeperkt.NetCore.Website.Configuration
{
    public class DockerSecretsConfigurationProvider : FileConfigurationProvider
    {
        public DockerSecretsConfigurationProvider(DockerSecretsConfigurationSource source) : base(source) { }

        public override void Load(Stream stream)
        {
            const string DOCKER_SECRET_PATH = "/var/run/secrets/";
            if (Directory.Exists(DOCKER_SECRET_PATH))
            {
                IFileProvider provider = new PhysicalFileProvider(DOCKER_SECRET_PATH);

                foreach (var fileInfo in provider.GetDirectoryContents(string.Empty))
                {
                    using (var dockerStream = fileInfo.CreateReadStream())
                    using (var streamReader = new StreamReader(dockerStream))
                    {
                        var temp = streamReader.ReadToEnd();

                        Data.Add(fileInfo.Name, temp);
                    }
                }
            }
        }
    }
}
