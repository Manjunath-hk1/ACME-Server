﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Primitives;
using System.Linq;
using TGIT.ACME.Server.Extensions;

namespace TGIT.ACME.Server.Filters
{
    public class AcmeIndexLinkFilter : IActionFilter
    {
        private readonly IUrlHelperFactory _urlHelperFactory;

        public AcmeIndexLinkFilter(IUrlHelperFactory urlHelperFactory)
        {
            _urlHelperFactory = urlHelperFactory;
        }

        public void OnActionExecuted(ActionExecutedContext context) { }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var urlHelper = _urlHelperFactory.GetUrlHelper(context);

            var linkHeaderUrl = urlHelper.RouteUrl("Directory", null, context.HttpContext.GetProtocol());
            var linkHeader = $"<{linkHeaderUrl}>;rel=\"index\"";

            //context.HttpContext.Response.Headers.Add("Link", linkHeader);
            var headers = context.HttpContext.Response.Headers;
            headers.Add("Link", linkHeader);
        }
    }
}
