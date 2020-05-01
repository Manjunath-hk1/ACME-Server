﻿using System.Threading;
using System.Threading.Tasks;
using TG_IT.ACME.Protocol.Model;

namespace TG_IT.ACME.Protocol.Storage
{
    public interface INonceStore
    {
        Task SaveNonceAsync(Nonce nonce, CancellationToken cancellationToken);
        Task<bool> TryRemoveNonceAsync(Nonce nonce, CancellationToken cancellationToken);
    }
}
