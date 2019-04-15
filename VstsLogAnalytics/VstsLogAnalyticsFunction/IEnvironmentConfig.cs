namespace VstsLogAnalyticsFunction
{
    public interface IEnvironmentConfig
    {
        string ExtensionName { get; }
        string Organisation { get; }
        string FunctionAppHostname { get; }
    }

    public class EnvironmentConfig : IEnvironmentConfig
    {
        public string ExtensionName { get; set; }
        public string Organisation { get; set; }
        public string FunctionAppHostname { get; set; }
    }
}