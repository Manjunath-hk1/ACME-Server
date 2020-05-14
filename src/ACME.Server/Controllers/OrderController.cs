﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TG_IT.ACME.Protocol.HttpModel.Requests;
using TG_IT.ACME.Protocol.Model;
using TG_IT.ACME.Protocol.Services;
using TG_IT.ACME.Server.Filters;

namespace TG_IT.ACME.Server.Controllers
{
    [ApiController]
    [AddNextNonce]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IAccountService _accountService;

        public OrderController(IOrderService orderService, IAccountService accountService)
        {
            _orderService = orderService;
            _accountService = accountService;
        }

        [Route("/new-order", Name = "NewOrder")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.Order>> CreateOrder(AcmeHttpRequest<CreateOrderRequest> request)
        {
            var account = await _accountService.FromRequestAsync(request, HttpContext.RequestAborted);

            var orderRequest = request.Payload!;

            var identifiers = orderRequest.Identifiers.Select(x =>
                new Protocol.Model.Identifier
                {
                    Type = x.Type,
                    Value = x.Value
                });

            var order = await _orderService.CreateOrderAsync(
                account, identifiers,
                orderRequest.NotBefore, orderRequest.NotAfter,
                HttpContext.RequestAborted);

            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);
            var orderResponse = new Protocol.HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);

            var orderUrl = Url.RouteUrl("GetOrder", new { orderId = order.OrderId }, "https");
            return new CreatedResult(orderUrl, orderResponse);
        }

        private void GetOrderUrls(Order order, out IEnumerable<string> authorizationUrls, out string finalizeUrl, out string certificateUrl)
        {
            authorizationUrls = order.Authorizations
                .Select(x => Url.RouteUrl("GetAuthorization", new { orderId = order.OrderId, authId = x.AuthorizationId }, "https"));
            finalizeUrl = Url.RouteUrl("FinalizeOrder", new { orderId = order.OrderId }, "https");
            certificateUrl = Url.RouteUrl("GetCertificate", new { orderId = order.OrderId }, "https");
        }

        [Route("/order/{orderId}", Name = "GetOrder")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.Order>> GetOrder(string orderId, AcmeHttpRequest request)
        {
            var account = await _accountService.FromRequestAsync(request, HttpContext.RequestAborted);
            var order = await _orderService.GetOrderAsync(account, orderId, HttpContext.RequestAborted);

            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);
            var orderResponse = new Protocol.HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);

            return orderResponse;
        }

        [Route("/order/{orderId}/auth/{authId}", Name = "GetAuthorization")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.Authorization>> GetAuthorization(string orderId, string authId, AcmeHttpRequest request)
        {
            var account = await _accountService.FromRequestAsync(request, HttpContext.RequestAborted);
            var order = await _orderService.GetOrderAsync(account, orderId, HttpContext.RequestAborted);

            var authZ = order.Authorizations.FirstOrDefault(x => x.AuthorizationId == authId);
            if (authZ == null)
                return NotFound();

            var challenges = authZ.Challenges
                .Select(x =>
                {
                    var challengeUrl = GetChallengeUrl(orderId, authId, x);

                    return new Protocol.HttpModel.Challenge(x, challengeUrl);
                });

            var authZResponse = new Protocol.HttpModel.Authorization(authZ, challenges);

            return authZResponse;
        }

        private string GetChallengeUrl(string orderId, string authId, Challenge challenge)
        {
            return Url.RouteUrl("AcceptChallenge",
                new { orderId = orderId, authId = authId, challangeId = challenge.ChallengeId }, "https");
        }

        [Route("/order/{orderId}/auth/{authId}/chall/{challengeId}", Name = "AcceptChallenge")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.Challenge>> AcceptChallenge(string orderId, string authId, string challengeId, AcmeHttpRequest request)
        {
            var account = await _accountService.FromRequestAsync(request, HttpContext.RequestAborted);
            var challenge = await _orderService.ProcessChallengeAsync(account, orderId, authId, challengeId, HttpContext.RequestAborted);

            var challengeResponse = new Protocol.HttpModel.Challenge(challenge, GetChallengeUrl(orderId, authId, challenge));
            return challengeResponse;
        }

        [Route("/order/{orderId}/finalize", Name = "FinalizeOrder")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.Order>> FinalizeOrder(string orderId, AcmeHttpRequest<FinalizeOrderRequest> request)
        {
            var account = await _accountService.FromRequestAsync(request, HttpContext.RequestAborted);
            var order = await _orderService.ProcessCsr(account, orderId, request.Payload.Csr, HttpContext.RequestAborted);
            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);

            var orderResponse = new Protocol.HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);
            return orderResponse;
        }

        [Route("/order/{orderId}/certificate", Name = "GetCertificate")]
        [HttpPost]
        public async Task<IActionResult> GetCertficate(string orderId, AcmeHttpRequest request)
        {
            var account = await _accountService.FromRequestAsync(request, HttpContext.RequestAborted);
            var certificate = await _orderService.GetCertificate(account, orderId, HttpContext.RequestAborted);

            return File(certificate, "pem/certificate");
        }
    }
}
