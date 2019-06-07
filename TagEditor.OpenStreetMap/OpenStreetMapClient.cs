using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TagEditor.OpenStreetMap.Models;

namespace TagEditor.OpenStreetMap
{
    public class OpenStreetMapClient
    {
        private OpenStreetMapHandler osmHandler;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accessToken">Oauth 1.0a AccessToken</param>
        /// <param name="accessTokenSecret">Oauth 1.0a AccessTokenSecret</param>
        public OpenStreetMapClient(string accessToken, string accessTokenSecret)
        {
            var accessData = new OpenStreetMapAccessModel(ConfigurationManager.AppSettings["appkey"], ConfigurationManager.AppSettings["appsecret"], accessToken, accessTokenSecret);
            var httpClient = new HttpClient();
            osmHandler = new OpenStreetMapHandler(httpClient, accessData);

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accessToken">Oauth 1.0a AccessToken</param>
        /// <param name="accessTokenSecret">Oauth 1.0a AccessTokenSecret</param>
        /// <param name="debug"> Determines the use of the development server of openstreetmap  </param> 
        public OpenStreetMapClient(string accessToken, string accessTokenSecret, bool debug) : this(accessToken, accessTokenSecret)
        {
            if (osmHandler != null)
                osmHandler.SetDebug(debug);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ways"></param>
        /// <param name="wheelchairAccessibility"></param>
        /// <param name="description"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<bool> SetTags(List<String> ways, string wheelchairAccessibility, string description, string language)
        {
            if (string.IsNullOrEmpty(language) || ways.Count() <= 0) 
                return false;

            var descriptionKey = !string.IsNullOrEmpty(description) ? "wheelchair:description:" + language : "";
            var changesetId = await osmHandler.CreateChangeset();

            foreach (string id in ways)
            {
                var wayXmlDetails = await osmHandler.GetWayData("4303058092");
                if (!string.IsNullOrEmpty(descriptionKey))
                    wayXmlDetails = osmHandler.getWayXmlWithNewTag(wayXmlDetails, descriptionKey, description);
                if (wheelchairAccessibility == "yes" || wheelchairAccessibility == "no" || wheelchairAccessibility == "limited")
                    wayXmlDetails = osmHandler.getWayXmlWithNewTag(wayXmlDetails, "wheelchair", wheelchairAccessibility);

                var uploadXml = osmHandler.getWayXmlAsOsmChangeset(wayXmlDetails, changesetId);
                await osmHandler.setChangesetData(changesetId, uploadXml);
            }
            await osmHandler.StopChangeset(changesetId);

            return true;
        }




    }
}

