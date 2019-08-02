using System;
using System.Linq;
using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace CompletenessCheckFunction.Tests
{
    public class StartupTests
    {
        [Fact]
        public void TestDependencyInjectionResolve()
        {
            var fixture = new Fixture();
            Environment.SetEnvironmentVariable("logAnalyticsWorkspace", fixture.Create<string>());
            Environment.SetEnvironmentVariable("logAnalyticsKey", fixture.Create<string>());
            Environment.SetEnvironmentVariable("durableBaseUri", fixture.Create<Uri>().ToString());
            Environment.SetEnvironmentVariable("durableTaskHub", fixture.Create<string>());
            Environment.SetEnvironmentVariable("durableMasterKey", fixture.Create<string>());
            Environment.SetEnvironmentVariable("tenantId", fixture.Create<string>());
            Environment.SetEnvironmentVariable("clientId", fixture.Create<string>());
            Environment.SetEnvironmentVariable("clientSecret", fixture.Create<string>());

            var startup = new Startup();

            var services = new ServiceCollection();

            var builder = Substitute.For<IWebJobsBuilder>();
            builder
                .Services
                .Returns(services);

            var functions = startup
                .GetType()
                .Assembly
                .GetTypes()
                .Where(type => type.GetMethods().Any(method =>
                        method.GetCustomAttributes(typeof(FunctionNameAttribute), false).Any() &&
                        !method.IsStatic))
                .ToList();

            functions.ForEach(f => services.AddScoped(f));

            startup.Configure(builder);
            var provider = services.BuildServiceProvider();

            functions.ForEach(f => provider.GetService(f));
        }
    }
}