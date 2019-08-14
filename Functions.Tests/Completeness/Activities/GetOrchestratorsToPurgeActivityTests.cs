using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Completeness.Activities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Functions.Tests.Completeness.Activities
{
    public class GetOrchestratorsToPurgeActivityTests
    {
        private readonly Fixture _fixture;
        private readonly string _finalContinuationToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("null"));

        public GetOrchestratorsToPurgeActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public async Task ShouldReturnNoSupervisorAndProjectScannerOrchestrators()
        {
            //Arrange
            _fixture.Customize<DurableOrchestrationStatus>(o => o
                .With(i => i.RuntimeStatus, OrchestrationRuntimeStatus.Completed)
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));

            var instances = _fixture.CreateMany<DurableOrchestrationStatus>(10).ToList();
            instances[3].Name = "ProjectScanSupervisor";
            instances[6].Name = "ProjectScanOrchestration";
            instances[9].Name = "ProjectScanOrchestration";

            var client = Substitute.For<DurableOrchestrationClientBase>();
            client
                .GetStatusAsync(new DateTime(), new DateTime(), new List<OrchestrationRuntimeStatus>(), 1000, string.Empty)
                .ReturnsForAnyArgs(new OrchestrationStatusQueryResult
                {
                    DurableOrchestrationState = instances,
                    ContinuationToken = _finalContinuationToken
                });

            //Act
            var func = new GetOrchestratorsToPurgeActivity();
            (var runningOrchestratorIds, var subOrchestratorIds) = await func.RunAsync(null, client);

            //Assert
            subOrchestratorIds.Count.ShouldBe(7);
        }

        [Theory]
        [InlineData(OrchestrationRuntimeStatus.Running, 10)]
        [InlineData(OrchestrationRuntimeStatus.Completed, 0)]
        [InlineData(OrchestrationRuntimeStatus.Failed, 0)]
        [InlineData(OrchestrationRuntimeStatus.Terminated, 0)]
        public async Task ShouldReturnOnlyRunningOrchestrators(OrchestrationRuntimeStatus status, int expected)
        {
            //Arrange
            _fixture.Customize<DurableOrchestrationStatus>(o => o
                .With(i => i.RuntimeStatus, status)
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));

            var instances = _fixture.CreateMany<DurableOrchestrationStatus>(10).ToList();

            var client = Substitute.For<DurableOrchestrationClientBase>();
            client
                .GetStatusAsync(new DateTime(), new DateTime(), new List<OrchestrationRuntimeStatus>(), 1000, string.Empty)
                .ReturnsForAnyArgs(new OrchestrationStatusQueryResult
                {
                    DurableOrchestrationState = instances,
                    ContinuationToken = _finalContinuationToken
                });

            //Act
            var func = new GetOrchestratorsToPurgeActivity();
            (var runningOrchestratorIds, var subOrchestratorIds) = await func.RunAsync(null, client);

            //Assert
            runningOrchestratorIds.Count.ShouldBe(expected);
        }
    }
}