using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.HttpModel.Requests;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Services;
using TGIT.ACME.Server.Filters;
using TGIT.ACME.Server.Extensions;
using Microsoft.Extensions.Logging;

namespace TGIT.ACME.Server.Controllers
{
    [AddNextNonce]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAccountService accountService, ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _logger = logger;   
        }

        [Route("/new-account", Name = "NewAccount")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.Account>> CreateOrGetAccount(AcmeHeader header, AcmePayload<CreateOrGetAccount> payload)
        {

            if (payload.Value.OnlyReturnExisting)
            {
                return await FindAccountAsync(payload);
            }

            

            return await CreateAccountAsync(header, payload);
        }

        private async Task<ActionResult<Protocol.HttpModel.Account>> CreateAccountAsync(AcmeHeader header, AcmePayload<CreateOrGetAccount> payload)
        {
            _logger.LogInformation(1001, $"New account creation starting with Kid {header.Kid} and nounce {header.Nonce}.");

            if (payload == null)
                throw new MalformedRequestException("Payload was empty or could not be read.");

            var account = await _accountService.CreateAccountAsync(
                header.Jwk!, //Post requests are validated, JWK exists.
                payload.Value.Contact,
                payload.Value.TermsOfServiceAgreed,
                HttpContext.RequestAborted);

            var ordersUrl = Url.RouteUrl("OrderList", new { accountId = account.AccountId }, HttpContext.GetProtocol());
            var accountResponse = new Protocol.HttpModel.Account(account, ordersUrl);

            var accountUrl = Url.RouteUrl("Account", new { accountId = account.AccountId }, HttpContext.GetProtocol());
            _logger.LogInformation(1002, $"New account creation complete with account ID {@account.AccountId}.");
            return new CreatedResult(accountUrl, accountResponse);
        }

        private Task<ActionResult<Protocol.HttpModel.Account>> FindAccountAsync(AcmePayload<CreateOrGetAccount> payload)
        {
            throw new NotImplementedException();
        }

        [Route("/account/{accountId}", Name = "Account")]
        [HttpPost, HttpPut]
        public Task<ActionResult<Protocol.HttpModel.Account>> SetAccount(string accountId)
        {
            throw new NotImplementedException();
        }

        [Route("/account/{accountId}/orders", Name = "OrderList")]
        [HttpPost]
        public Task<ActionResult<Protocol.HttpModel.OrdersList>> GetOrdersList(string accountId, AcmePayload<object> payload)
        {
            throw new NotImplementedException();
        }
    }
}
