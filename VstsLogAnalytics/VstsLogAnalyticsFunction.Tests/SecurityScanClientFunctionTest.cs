using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Build.Framework;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Xunit;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace VstsLogAnalyticsFunction.Tests
{
    public class SecurityScanClientFunctionTest
    {
        [Fact]
        public void SecurityScanGetProjectsShouldCallGetProjects()
        {
            var allProjects = CreateProjectsResponse();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Project>>>()))
                .Returns(allProjects);

            SecurityScanClientFunction.SecurityScanGetProjects(client.Object, new Mock<ILogger>().Object);

            client.Verify(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Project>>>()),
                Times.AtLeastOnce());
        }

        [Fact]
        public void SecurityScanGetProjectsShouldReturnProjects()
        {
            var allProjects = CreateProjectsResponse();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Project>>>()))
                .Returns(allProjects);

            var securityScanGetProjects = SecurityScanClientFunction.SecurityScanGetProjects(client.Object, new Mock<ILogger>().Object);

            Assert.Equal(2, securityScanGetProjects.Count());
        }

        private static Multiple<Project> CreateProjectsResponse()
        {
            var project1 = new Project
            {
                Id = "1",
                Name = "TAS"
            };

            var project2 = new Project
            {
                Id = "2",
                Name = "TASSIE"
            };
            var allProjects = new Multiple<Project>(project1, project2);
            return allProjects;
        }
    }
}