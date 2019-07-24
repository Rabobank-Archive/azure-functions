using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace Functions.Tests
{
    public class StartupTests
    {
        [Fact]
        public void TestDependencyInjectionResolve()
        {
            var fixture = new Fixture();
            Environment.SetEnvironmentVariable("TOKEN_SECRET", fixture.Create<string>());
            Environment.SetEnvironmentVariable("WEBSITE_HOSTNAME", fixture.Create<string>());
            Environment.SetEnvironmentVariable("vstsPat", fixture.Create<string>());

            var startup = new Startup();

            var services = new ServiceCollection();

            var builder = new Mock<IWebJobsBuilder>();
            builder
                .Setup(x => x.Services)
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

            startup.Configure(builder.Object);
            var provider = services.BuildServiceProvider();

            functions.ForEach(f => provider.GetService(f));
        }
    }
}