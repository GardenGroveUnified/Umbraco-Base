using UmbracoBase.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UmbracoBase.Core.Components
{
    //[ViewComponent(Name = "MemberLogin")]
    public class MemberLoginViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(MemberLoginViewModel model)
        {
            return View(model);
        }
    }
}