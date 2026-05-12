using UmbracoBase.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace UmbracoBase.Core.Controllers
{
    public class BrocadeSwitchSurfaceController : SurfaceController
    {
        public BrocadeSwitchSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services, AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider) : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
        }

        [HttpPost]
        public Task<IActionResult> GenerateBrocadeCoreSeriesScript(BrocadeSwitchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Task.FromResult(CurrentUmbracoPage() as IActionResult);
            }
            return Task.FromResult(View("~/Views/Components/BrocadeCoreSeries/GenerateBrocadeCoreSeriesScript.cshtml", model) as IActionResult);
        }

        [HttpPost]
        public Task<IActionResult> GenerateBrocadeEdgeSeriesScript(BrocadeSwitchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Task.FromResult(CurrentUmbracoPage() as IActionResult);
            }
            return Task.FromResult(View("~/Views/Components/BrocadeEdgeSeries/GenerateBrocadeEdgeSeriesScript.cshtml", model) as IActionResult);
        }
    }
}
