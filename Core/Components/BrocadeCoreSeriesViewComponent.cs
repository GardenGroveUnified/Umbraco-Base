using UmbracoBase.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace UmbracoBase.Core.Components
{
    public class BrocadeCoreSeriesViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(BrocadeSwitchViewModel model)
        {
            return View(model);
        }
    }
}