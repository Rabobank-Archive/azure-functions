namespace VstsLogAnalyticsFunction
{
    public interface IAzureDevOpsConfig
    {
        string ExtensionName { get; }
        string Organisation { get; }
    }

    public class AzureDevOpsConfig : IAzureDevOpsConfig
    {
        public string ExtensionName { get; set; }
        public string Organisation { get; set; }
    }
}