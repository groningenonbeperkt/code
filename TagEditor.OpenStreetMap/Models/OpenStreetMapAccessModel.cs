namespace TagEditor.OpenStreetMap.Models
{
    public class OpenStreetMapAccessModel
    {
        public OpenStreetMapAccessModel(string appKey, string appSecret, string accessToken, string accessTokenSecret)
        {
            AppKey = appKey;
            AppSecret = appSecret;
            AccessToken = accessToken;
            AccessTokenSecret = accessTokenSecret;
        }

        /// <summary>
        /// Gets the OpenStreetMap application key
        /// </summary>
        public string AppKey { get; private set; }

        /// <summary>
        /// Gets the OpenStreetMap application secret
        /// </summary>
        public string AppSecret { get; private set; }

        /// <summary>
        /// Gets the OpenStreetMap access token
        /// </summary>
        public string AccessToken { get; private set; }

        /// <summary>
        /// Gets the OpenStreetMap access token secret
        /// </summary>
        public string AccessTokenSecret { get; private set; }
    }
}
