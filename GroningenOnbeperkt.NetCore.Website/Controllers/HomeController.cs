using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Openstreetmap;

using GroningenOnbeperkt.NetCore.Website;
using GroningenOnbeperkt.NetCore.Website.Models;
using GroningenOnbeperkt.NetCore.Website.Models.Options;
using GroniningenOnbeperkt.NetCore.TagEditor.Openstreetmap;

namespace GroningenOnbeperkt.NetCore.Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly AuthorizationOptions _authorizationOptions;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(IHostingEnvironment hostingEnvironment, IOptions<AuthorizationOptions> authorizationOptions, UserManager<ApplicationUser> userManager)
        {
            this._hostingEnvironment = hostingEnvironment;
            this._authorizationOptions = authorizationOptions.Value;
            this._userManager = userManager;
        }

        public ActionResult Index()
        {
            ViewBag.OsrmServiceUrl = Startup.Configuration.GetSection("AppSettings")["OsrmServiceUrl"];
            ViewBag.RandomUrl = Startup.Configuration["dbsecret"];
            return View();
        }

        public ActionResult Faq()
        {
            ViewBag.Message = "Faq page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "";

            return View();
        }

        [HttpPost]
        public async Task<string> EditOpenstreetmapAccessibility(List<String> ways, string wheelchairAccessibility, string description, string language)
        {
            var currentClaims = this.User.Claims;
            var accessToken = currentClaims.Where(x => x.Type == OpenstreetmapClaimTypes.AccessToken).Select(x => x.Value).FirstOrDefault();
            var accessTokenSecret = currentClaims.Where(x => x.Type == OpenstreetmapClaimTypes.AccessTokenSecret).Select(x => x.Value).FirstOrDefault();

            OpenStreetMapClient osmClient = new OpenStreetMapClient(_authorizationOptions.ConsumerKey, _authorizationOptions.ConsumerSecret, accessToken, accessTokenSecret, _hostingEnvironment.IsDevelopment());
            var result = await osmClient.SetTags(ways, wheelchairAccessibility, description, language);
            var message = result ? (ways.Count() == 1 ? "Er is 1 weg gewijzigd." : "Er zijn " + ways.Count() + " wegen gewijzigd.") : "Niet alle gegevens zijn correct ingevuld.";
            return message;
        }
    }
}
