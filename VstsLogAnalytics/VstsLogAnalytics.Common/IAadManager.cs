using System.Threading.Tasks;

namespace VstsLogAnalytics.Common
{
    public interface IAadManager
    {
        Task<string> GetAccessTokenAsync();
    }
}