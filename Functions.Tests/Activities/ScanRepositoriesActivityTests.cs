using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.VstsService;
using Response = SecurePipelineScan.VstsService.Response;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using AzureDevOps.Compliance.Rules;
using Functions.Helpers;

namespace Functions.Tests.Activities
{
    public class ScanRepositoriesActivityTests
    {
        [Fact]
        public async Task RunShouldReturnItemExtensionDataForRepository()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var request = fixture.Create<(Response.Project, Response.Repository)>();

            // Act
            var activity = new ScanRepositoriesActivity(
                fixture.Create<EnvironmentConfig>(),
                fixture.CreateMany<IRepositoryRule>());

            var result = await activity.RunAsync(request);

            // Assert
            result.ShouldNotBeNull();
        }
    }
}