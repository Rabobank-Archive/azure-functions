using Polly;
using Polly.Retry;
using SecurePipelineScan.VstsService;

namespace Functions.Helpers
{
    public class RetryHelper
    {
        public static readonly RetryPolicy InvalidDocumentVersionPolicy = Policy
            .Handle<VstsException>(ex => ex.Message.Contains("InvalidDocumentVersionException"))
            .Retry(3);
    }
}
