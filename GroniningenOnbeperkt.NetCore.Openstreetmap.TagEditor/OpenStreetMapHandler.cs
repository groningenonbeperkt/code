using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using GroniningenOnbeperkt.NetCore.Openstreetmap.TagEditor.Models;

namespace GroniningenOnbeperkt.NetCore.Openstreetmap.TagEditor
{
    internal class OpenStreetMapHandler
    {
        private string BaseUrl = "https://api.openstreetmap.org";
        private const string ChangesetBasePath = "/api/0.6/changeset/";
        private const string CreateChangesetPath = ChangesetBasePath + "create";
        private const string WayDetailPath = "/api/0.6/way/";

        private bool _useDevelopmentApi;

        private readonly HttpClient _httpClient;
        private readonly OpenStreetMapAccessModel _accessData;

        public OpenStreetMapHandler(HttpClient httpClient, OpenStreetMapAccessModel accessData)
        {
            _httpClient = httpClient;
            _accessData = accessData;
            _useDevelopmentApi = false;
        }

        public bool UseDevelopmentApi {
            get
            {
                return this._useDevelopmentApi;
            }

            set
            {
                this._useDevelopmentApi = value;

                if (this._useDevelopmentApi)
                    BaseUrl = "https://master.apis.dev.openstreetmap.org";
                else
                    BaseUrl = "https://api.openstreetmap.org";
            }
        }

        public async Task<string> CreateChangeset()
        {
            var url = BaseUrl + CreateChangesetPath;
            string xmlCreate = "<osm><changeset version=\"0.6\" generator=\"tfe\"><tag k=\"created_by\" v=\"tfe\"/>" +
                       "<tag k=\"comment\" v=\"surface gewijzigd\"/></changeset></osm>";

            var createRequest = OpenStreetMapAccessUrl.getHttpRequestMessage(_accessData, url, HttpMethod.Put);
            createRequest.Content = new StringContent(xmlCreate, Encoding.UTF8, "text/xml");

            var createResponse = await _httpClient.SendAsync(createRequest);

            if (!createResponse.IsSuccessStatusCode)
            {
                //_logger.WriteError("User details request failed with a status code of " + createResponse.StatusCode);
                createResponse.EnsureSuccessStatusCode(); // <-- checken of dit wel goed gaat bij een error ivm async. app moet niet volledig crashen!!
            }

            return await createResponse.Content.ReadAsStringAsync();
        }

        public async Task setChangesetData(string changesetId, string xml)
        {
            var url = BaseUrl + ChangesetBasePath + changesetId + "/upload";

            var uploadRequest = OpenStreetMapAccessUrl.getHttpRequestMessage(_accessData, url, HttpMethod.Post);
            uploadRequest.Content = new StringContent(xml, Encoding.UTF8, "text/xml");

            var uploadResponse = await _httpClient.SendAsync(uploadRequest);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                // _logger.WriteError("User details request failed with a status code of " + uploadResponse.StatusCode);
                uploadResponse.EnsureSuccessStatusCode(); // <-- checken of dit wel goed gaat bij een error ivm async. app moet niet volledig crashen!!
            }

        }

        public async Task StopChangeset(string changesetId)
        {
            string url = BaseUrl + ChangesetBasePath + changesetId + "/close";
            var closeRequest = OpenStreetMapAccessUrl.getHttpRequestMessage(_accessData, url, HttpMethod.Put);
            var closeResponse = await _httpClient.SendAsync(closeRequest);

            if (!closeResponse.IsSuccessStatusCode)
            {
                // _logger.WriteError("User details request failed with a status code of " + uploadResponse.StatusCode);
                closeResponse.EnsureSuccessStatusCode(); // <-- checken of dit wel goed gaat bij een error ivm async. app moet niet volledig crashen!!
            }

        }

        public async Task<XDocument> GetWayData(string wayId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + WayDetailPath + wayId);
            request.Headers.Add("Accept", "application/xml");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                //_logger.WriteError("Way details request failed with a status code of " + response.StatusCode);
                response.EnsureSuccessStatusCode(); // <-- checken of dit wel goed gaat bij een error ivm async. app moet niet volledig crashen!!
            }

            var responseText = await response.Content.ReadAsStringAsync();
            XDocument xdocWayDetails = XDocument.Parse(responseText);

            return xdocWayDetails;
        }

        public XDocument GetWayXmlWithNewTag(XDocument xml, string key, string value)
        {
            var target = xml
                 .Element("osm").Element("way")
                 .Elements("tag").Where(e => e.Attribute("k").Value == key).SingleOrDefault();

            if (target != null)
                target.Attribute("v").Value = value;
            else
                xml.Element("osm").Element("way").Add(new XElement("tag", new XAttribute("k", key), new XAttribute("v", value)));

            return xml;
        }

        public string GetWayXmlAsOsmChangeset(XDocument xml, string changesetId)
        {
            var target = xml.Element("osm").Element("way");          
            if (target.Attribute("changeset") != null)
                target.Attribute("changeset").Value = changesetId;
            else
                target.Add( new XAttribute("changeset", changesetId));

            target.Attributes().Where(a => a.Name != "id" && a.Name != "version" && a.Name != "changeset").Remove();
            var newXml = "<osmChange version =\"0.6\" generator=\"tfe\"><modify>" +target + "</modify></osmChange>";

            return newXml;
        }


    }
}

