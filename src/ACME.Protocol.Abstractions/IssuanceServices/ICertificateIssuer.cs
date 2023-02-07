using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.IssuanceServices
{
    public interface ICertificateIssuer
    {
        Task<(byte[]? certificate, AcmeError? error)> IssueCertificate(string csr, string sans, CancellationToken cancellationToken);
    }
}
