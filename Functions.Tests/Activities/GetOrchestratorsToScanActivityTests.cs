using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Functions.Tests.Activities
{
    public class GetOrchestratorsToScanActivityTests
    {
        private readonly Fixture _fixture;
        private readonly string _finalContinuationToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("null"));

        public GetOrchestratorsToScanActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public async Task ShouldReturnOnlyInstancesWithSpecifiedName()
        {
            //Arrange
            _fixture.Customize<DurableOrchestrationStatus>(o => o
                .With(i => i.RuntimeStatus, OrchestrationRuntimeStatus.Completed)
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));

            var instances = _fixture.CreateMany<DurableOrchestrationStatus>(10).ToList();
            instances[3].Name = "ProjectScanSupervisor";
            instances[6].Name = "ProjectScanOrchestrator";
            instances[9].Name = "ProjectScanOrchestrator";

            var client = Substitute.For<DurableOrchestrationClientBase>();
            client
                .GetStatusAsync(new DateTime(), new DateTime(), new List<OrchestrationRuntimeStatus>(), 1000, string.Empty)
                .ReturnsForAnyArgs(new OrchestrationStatusQueryResult
                {
                    DurableOrchestrationState = instances,
                    ContinuationToken = _finalContinuationToken
                });

            //Act
            var func = new GetOrchestratorsToScanActivity();
            (var supervisors, var projectScanOrchestrators) = await func.RunAsync(null, client);

            //Assert
            supervisors.Count.ShouldBe(1);
            supervisors[0].Name.ShouldBe("ProjectScanSupervisor");
            projectScanOrchestrators.Count.ShouldBe(2);
            projectScanOrchestrators[0].Name.ShouldBe("ProjectScanOrchestrator");
        }

        [Fact]
        public async Task ShouldNotBreakIfNoCustomStatus()
        {
            //Arrange
            _fixture.Customize<DurableOrchestrationStatus>(o => o
                .With(i => i.Name, "ProjectScanSupervisor")
                .With(i => i.RuntimeStatus, OrchestrationRuntimeStatus.Completed)
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .Without(d => d.CustomStatus));

            var client = Substitute.For<DurableOrchestrationClientBase>();
            client
                .GetStatusAsync(new DateTime(), new DateTime(), new List<OrchestrationRuntimeStatus>(), 1000, string.Empty)
                .ReturnsForAnyArgs(new OrchestrationStatusQueryResult
                {
                    DurableOrchestrationState = _fixture.CreateMany<DurableOrchestrationStatus>(1).ToList(),
                    ContinuationToken = _finalContinuationToken
                });

            //Act
            var func = new GetOrchestratorsToScanActivity();
            var (supervisors, _) = await func.RunAsync(null, client);

            // Assert
            supervisors[0].CustomStatus.ShouldBeNull();
        }
    }
}