﻿using ACME.Protocol.HttpModel.Requests;
using ACME.Protocol.Server.Extensions;
using ACME.Protocol.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ACME.Protocol.Server.Filters
{
    public class ValidateNonceFilter : IAsyncActionFilter
    {
        private readonly INonceService _nonceService;
        private readonly ILogger<ValidateNonceFilter> _logger;

        public ValidateNonceFilter(INonceService nonceService, ILogger<ValidateNonceFilter> logger)
        {
            _nonceService = nonceService;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (HttpMethods.IsPost(context.HttpContext.Request.Method))
            {
                _logger.LogInformation("Attempting to validate Nonce");
                var acmeRequest = context.GetAcmeRequest();
                await _nonceService.ValidateNonceAsync(acmeRequest.Header.Nonce, context.HttpContext.RequestAborted);
            }

            await next();
        }
    }
}
