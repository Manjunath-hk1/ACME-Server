﻿using ACME.Protocol.Model;
using ACME.Protocol.Storage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ACME.Protocol.Services
{
    public class DefaultAccountService : IAccountService
    {
        private readonly IAccountStore _accountStore;

        public DefaultAccountService(IAccountStore accountStore)
        {
            _accountStore = accountStore;
        }

        public async Task<AcmeAccount> CreateAccountAsync(Jwk jwk, List<string> contact,
            bool termsOfServiceAgreed, CancellationToken cancellationToken)
        {
            var newAccount = new AcmeAccount
            {
                Jwk = jwk,
                Contact = contact,
                TOSAccepted = termsOfServiceAgreed ? DateTimeOffset.Now : (DateTimeOffset?)null
            };

            await _accountStore.SaveAccountAsync(newAccount, cancellationToken);
            return newAccount;
        }

        public Task<AcmeAccount> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<AcmeAccount> LoadAcountAsync(string accountId, CancellationToken cancellationToken)
        {
            return await _accountStore.LoadAccountAsync(accountId, cancellationToken);
        }
    }
}
