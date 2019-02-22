using System.Threading.Tasks;

namespace VstsLogAnalytics.Common
{
    public interface IAzureServiceTokenProviderWrapper
    {
        Task<string> GetAccessTokenAsync();
    }
}