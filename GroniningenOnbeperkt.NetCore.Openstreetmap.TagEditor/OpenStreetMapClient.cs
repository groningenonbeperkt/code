using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GroniningenOnbeperkt.NetCore.Openstreetmap.TagEditor.Models;

namespace GroniningenOnbeperkt.NetCore.Openstreetmap.TagEditor
{
    public class OpenStreetMapClient
    {
        private OpenStreetMapHandler _osmHandler;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accessToken">Oauth 1.0a AccessToken</param>
        /// <param name="accessTokenSecret">Oauth 1.0a AccessTokenSecret</param>
        public OpenStreetMapClient(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {
            var accessData = new OpenStreetMapAccessModel(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            var httpClient = new HttpClient();
            _osmHandler = new OpenStreetMapHandler(httpClient, accessData);

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accessToken">Oauth 1.0a AccessToken</param>
        /// <param name="accessTokenSecret">Oauth 1.0a AccessTokenSecret</param>
        /// <param name="useDevelopmentApi"> Determines the use of the development server of openstreetmap  </param> 
        public OpenStreetMapClient(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, bool useDevelopmentApi) : this(consumerKey, consumerSecret, accessToken, accessTokenSecret)
        {
            if (_osmHandler != null)
                _osmHandler.UseDevelopmentApi = useDevelopmentApi;
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
            var changesetId = await _osmHandler.CreateChangeset();

            foreach (string id in ways)
            {
                var wayXmlDetails = await _osmHandler.GetWayData("4303058092");
                if (!string.IsNullOrEmpty(descriptionKey))
                    wayXmlDetails = _osmHandler.GetWayXmlWithNewTag(wayXmlDetails, descriptionKey, description);
                if (wheelchairAccessibility == "yes" || wheelchairAccessibility == "no" || wheelchairAccessibility == "limited")
                    wayXmlDetails = _osmHandler.GetWayXmlWithNewTag(wayXmlDetails, "wheelchair", wheelchairAccessibility);

                var uploadXml = _osmHandler.GetWayXmlAsOsmChangeset(wayXmlDetails, changesetId);
                await _osmHandler.setChangesetData(changesetId, uploadXml);
            }
            await _osmHandler.StopChangeset(changesetId);

            return true;
        }




    }
}

