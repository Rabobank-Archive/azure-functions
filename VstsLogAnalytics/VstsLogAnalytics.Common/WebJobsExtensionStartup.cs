
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using VstsLogAnalytics.Common;

[assembly: WebJobsStartup(typeof(WebJobsExtensionStartup), "Common startup extensions")]

namespace VstsLogAnalytics.Common
{
    public class WebJobsExtensionStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            
            //Registering an extension
            builder.AddExtension<InjectConfiguration>();
        }
    }
}
