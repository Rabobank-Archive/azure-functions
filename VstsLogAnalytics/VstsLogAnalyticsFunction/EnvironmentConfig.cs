namespace VstsLogAnalyticsFunction
{
    public class EnvironmentConfig
    {
        public string ExtensionName { get; set; }
        public string Organization { get; set; }
        public string FunctionAppHostname { get; set; }
        public string StorageAccountConnectionString { get; set; }
    }
}