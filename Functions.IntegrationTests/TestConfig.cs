using Microsoft.Extensions.Configuration;

namespace Functions.IntegrationTests
{
    public class TestConfig
    {
        public TestConfig()
        {
            new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.user.json", true)
                .AddEnvironmentVariables()
                .Build()
                .Bind(this);
        }
        public string Organization { get; set; }
        public string Token { get; set; }
        public string ExtensionName { get; set; }
        public string ExtensionSecret { get; set; }
        public string ProjectId { get; set; }
        public string ReleasePipelineId { get; set; }
        public string BuildPipelineId { get; set; }
        public string RepositoryId { get; set; }
        public string CmdbEndpoint { get; set; }
        public string CmdbApiKey { get; set; }
    }
}