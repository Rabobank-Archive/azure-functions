using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs;

namespace Functions.Routing
{
    public static class WebJobsBuilderExtensions
    {
        [ExcludeFromCodeCoverage]
        public static IWebJobsBuilder AddRoutePriority(this IWebJobsBuilder builder)
        {
            builder.AddExtension<RoutePriorityExtensionConfigProvider>();
            return builder;
        }
    }
}
