using UmbracoBase.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UmbracoBase.Core.Components
{
    public class BrocadeEdgeSeriesViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(BrocadeSwitchViewModel model)
        {
            return View(model);
        }
    }
}