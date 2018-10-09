using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using VstsLogAnalytics.Client;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests
{
    public class GitPushToLogAnalyticsTests
    {
        [Fact]
        public void GitPushToLogAnalyticsRun()
        {
            string jsonEvent = CreateGitPushJson();
            var logger = new Mock<ILogger>();
            var client = new Mock<ILogAnalyticsClient>();

            GitPushToLogAnalytics.Run(jsonEvent, client.Object, logger.Object);
        }

        private string CreateGitPushJson()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "gitpushexample.json");
            return File.ReadAllText(path);
        }
    }
}