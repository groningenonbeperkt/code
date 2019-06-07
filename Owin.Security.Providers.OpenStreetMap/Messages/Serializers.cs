using Microsoft.Owin.Security.DataHandler.Serializer;

namespace Owin.Security.Providers.OpenStreetMap.Messages
{
    public class Serializers
    {
        static Serializers()
        {
            RequestToken = new RequestTokenSerializer();
        }

        /// <summary>
        /// Gets or sets a statically-avaliable serializer object. The value for this property will be <see cref="RequestTokenSerializer"/> by default.
        /// </summary>
        public static IDataSerializer<RequestToken> RequestToken { get; set; }
    }
}
