using System.Threading.Tasks;

namespace LogAnalytics.Client
{
    public interface ILogAnalyticsClient
    {
        Task AddCustomLogJsonAsync(string logName, object input, string timefield);
    }
}