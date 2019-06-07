namespace Owin.Security.Providers.OpenStreetMap.Messages
{
    public class AccessToken : RequestToken
    {
        /// <summary>
        /// Gets or sets the OpenStreetMap User ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the OpenStreetMap User Name
        /// </summary>
        public string UserName { get; set; }

    }
}
