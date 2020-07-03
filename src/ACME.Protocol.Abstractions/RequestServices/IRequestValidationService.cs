using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TGIT.ACME.Protocol.HttpModel.Requests;

namespace TGIT.ACME.Protocol.RequestServices
{
    public interface IRequestValidationService
    {
        Task ValidateRequestAsync(AcmeRawPostRequest request, AcmeHeader header,
            string requestUrl, CancellationToken cancellationToken);
    }
}
