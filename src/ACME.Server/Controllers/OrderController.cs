using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.HttpModel.Requests;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Services;
using TGIT.ACME.Server.Filters;
using TGIT.ACME.Server.Extensions;
using Microsoft.Extensions.Logging;

namespace TGIT.ACME.Server.Controllers
{
    [AddNextNonce]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IAccountService _accountService;
        private readonly ILogger<OrderController> _logger;
        public OrderController(IOrderService orderService, IAccountService accountService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _accountService = accountService;
            _logger = logger;
        }

        [Route("/new-order", Name = "NewOrder")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.Order>> CreateOrder(AcmePayload<CreateOrderRequest> payload)
        {
            _logger.LogInformation(2001, "New order request starting");

            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);

            var orderRequest = payload.Value;

            if (orderRequest.Identifiers?.Any() != true)
                throw new MalformedRequestException("No identifiers submitted");

            foreach (var i in orderRequest.Identifiers)
                if(string.IsNullOrWhiteSpace(i.Type) || string.IsNullOrWhiteSpace(i.Value))
                    throw new MalformedRequestException($"Malformed identifier: (Type: {i.Type}, Value: {i.Value})");

            var identifiers = orderRequest.Identifiers.Select(x =>
                new Protocol.Model.Identifier(x.Type!, x.Value!)
            );

            var order = await _orderService.CreateOrderAsync(
                account, identifiers,
                orderRequest.NotBefore, orderRequest.NotAfter,
                HttpContext.RequestAborted);

            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);
            var orderResponse = new Protocol.HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);

            var orderUrl = Url.RouteUrl("GetOrder", new { orderId = order.OrderId }, HttpContext.GetProtocol());
            _logger.LogInformation(2001, $"New order request complete : {order.OrderId}.");
            return new CreatedResult(orderUrl, orderResponse);
        }

        private void GetOrderUrls(Order order, out IEnumerable<string> authorizationUrls, out string finalizeUrl, out string certificateUrl)
        {
            authorizationUrls = order.Authorizations
                .Select(x => Url.RouteUrl("GetAuthorization", new { orderId = order.OrderId, authId = x.AuthorizationId }, HttpContext.GetProtocol()));
            finalizeUrl = Url.RouteUrl("FinalizeOrder", new { orderId = order.OrderId }, HttpContext.GetProtocol());
            certificateUrl = Url.RouteUrl("GetCertificate", new { orderId = order.OrderId }, HttpContext.GetProtocol());
        }

        [Route("/order/{orderId}", Name = "GetOrder")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.Order>> GetOrder(string orderId)
        {
            _logger.LogInformation(2002, $"Get order request starting Order ID {orderId}.");
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var order = await _orderService.GetOrderAsync(account, orderId, HttpContext.RequestAborted);

            if (order == null)
            {
                _logger.LogInformation(2003, $"Order request for  order ID {orderId} is not found.");
                return NotFound();
            }

            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);
            var orderResponse = new Protocol.HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);
            _logger.LogInformation(2004, $"Order fetch request for  Order ID {orderId} is successful.");
            return orderResponse;
        }

        [Route("/order/{orderId}/auth/{authId}", Name = "GetAuthorization")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.Authorization>> GetAuthorization(string orderId, string authId)
        {
            _logger.LogInformation(2005, $"Get authorization request starting Order ID {orderId} and Auth Id {authId}.");

            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var order = await _orderService.GetOrderAsync(account, orderId, HttpContext.RequestAborted);

            if (order == null)
            {
                _logger.LogInformation(2006, $"Order request for  Order ID {orderId} is not found.");
                return NotFound();
            }

            var authZ = order.GetAuthorization(authId);
            if (authZ == null)
            {
                _logger.LogInformation(2007, $"Authorization request for Auth ID {authId} is not found.");
                return NotFound();
            }

            var challenges = authZ.Challenges
                .Select(challenge =>
                {
                    var challengeUrl = GetChallengeUrl(challenge);

                    return new Protocol.HttpModel.Challenge(challenge, challengeUrl);
                });

            var authZResponse = new Protocol.HttpModel.Authorization(authZ, challenges);
        
            _logger.LogInformation(2008, $"Authorization request is complete for Auth ID {authId} with status {authZResponse.Status}.");

            return authZResponse;
        }

        private string GetChallengeUrl(Challenge challenge)
        {
            return Url.RouteUrl("AcceptChallenge",
                new { 
                    orderId = challenge.Authorization.Order.OrderId,
                    authId = challenge.Authorization.AuthorizationId,
                    challengeId = challenge.ChallengeId },
                HttpContext.GetProtocol());
        }

        [Route("/order/{orderId}/auth/{authId}/chall/{challengeId}", Name = "AcceptChallenge")]
        [HttpPost]
        [AcmeLocation("GetOrder")]
        public async Task<ActionResult<Protocol.HttpModel.Challenge>> AcceptChallenge(string orderId, string authId, string challengeId)
        {
            _logger.LogInformation(2009, $"Acme challenge request starting order ID {orderId} , Auth Id {authId} and Challenge Id {challengeId}.");

            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);

            var challenge = await _orderService.ProcessChallengeAsync(account, orderId, authId, challengeId, HttpContext.RequestAborted);

            if (challenge == null)
            {
                _logger.LogInformation(2010, $"Acme challenge request is not found for Order ID {orderId} , Auth Id {authId} and Challenge Id {challengeId}.");
                throw new NotFoundException();
            }

            var linkHeaderUrl = Url.RouteUrl("GetAuthorization", new { orderId = orderId, authId = authId }, HttpContext.GetProtocol());
            var linkHeader = $"<{linkHeaderUrl}>;rel=\"up\"";

            HttpContext.Response.Headers.AddOrMerge("Link", linkHeader);

            var challengeResponse = new Protocol.HttpModel.Challenge(challenge, GetChallengeUrl(challenge));

            _logger.LogInformation(2011, $"Acme challenge request is complete  for order ID {orderId} , Auth Id {authId} and Challenge Id {challengeId} with challenge status {challengeResponse.Status}.");

            return challengeResponse;
        }

        [Route("/order/{orderId}/finalize", Name = "FinalizeOrder")]
        [HttpPost]
        [AcmeLocation("GetOrder")]
        public async Task<ActionResult<Protocol.HttpModel.Order>> FinalizeOrder(string orderId, AcmePayload<FinalizeOrderRequest> payload)
        {
            _logger.LogInformation(2012, $"Finalize order request starting for Order ID {orderId}.");
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var order = await _orderService.ProcessCsr(account, orderId, payload.Value.Csr, HttpContext.RequestAborted);

            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);

            var orderResponse = new Protocol.HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);

            _logger.LogInformation(2013, $"Finalize order request complete for Order ID {orderId} with status {orderResponse.Status}.");

            return orderResponse;
        }

        [Route("/order/{orderId}/certificate", Name = "GetCertificate")]
        [HttpPost]
        [AcmeLocation("GetOrder")]
        public async Task<IActionResult> GetCertificate(string orderId)
        {
            _logger.LogInformation(2014, $"Get certificate request starting for Order ID {orderId}.");
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var certificate = await _orderService.GetCertificate(account, orderId, HttpContext.RequestAborted);
            if(certificate.Length > 0)
            _logger.LogInformation(2014, $"Get certificate request complete for Order ID {orderId} is successful.");

            return File(certificate, "application/pem-certificate-chain");
        }
    }
}
