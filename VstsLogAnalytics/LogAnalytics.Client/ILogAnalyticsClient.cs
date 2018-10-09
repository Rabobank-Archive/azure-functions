using System.Threading.Tasks;

namespace VstsLogAnalytics.Client
{
    public interface ILogAnalyticsClient
    {
        Task AddCustomLogJsonAsync(string logName, string json, string timefield);
    }
}