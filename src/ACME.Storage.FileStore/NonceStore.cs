﻿using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Storage;
using TGIT.ACME.Storage.FileStore.Configuration;

namespace TGIT.ACME.Storage.FileStore
{
    public class NonceStore : INonceStore
    {
        private readonly IOptions<FileStoreOptions> _options;

        public NonceStore(IOptions<FileStoreOptions> options)
        {
            _options = options;
        }

        public async Task SaveNonceAsync(Nonce nonce, CancellationToken cancellationToken)
        {
            if (nonce is null)
                throw new ArgumentNullException(nameof(nonce));

            var noncePath = System.IO.Path.Combine(_options.Value.NoncePath, nonce.Token);
            await System.IO.File.WriteAllTextAsync(noncePath, DateTime.Now.ToString("o", CultureInfo.InvariantCulture), cancellationToken);
        }

        public Task<bool> TryRemoveNonceAsync(Nonce nonce, CancellationToken cancellationToken)
        {
            if (nonce is null)
                throw new ArgumentNullException(nameof(nonce));

            var noncePath = System.IO.Path.Combine(_options.Value.NoncePath, nonce.Token);
            if (!System.IO.File.Exists(noncePath))
                return Task.FromResult(false);

            System.IO.File.Delete(noncePath);
            return Task.FromResult(true);
        }
    }
}
