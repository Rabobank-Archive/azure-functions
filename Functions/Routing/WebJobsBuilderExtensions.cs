using Microsoft.Azure.WebJobs;

namespace Functions.Routing
{
    public static class WebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddRoutePriority(this IWebJobsBuilder builder)
        {
            builder.AddExtension<RoutePriorityExtensionConfigProvider>();
            return builder;
        }
    }
}
