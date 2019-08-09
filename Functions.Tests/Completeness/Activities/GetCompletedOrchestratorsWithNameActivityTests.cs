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
    public class GetCompletedOrchestratorsWithNameActivityTests
    {
        private readonly Fixture _fixture;
        private readonly string _finalContinuationToken = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("null"));

        public GetCompletedOrchestratorsWithNameActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }
        
        [Fact]
        public async Task ShouldReturnOnlyInstancesWithSpecifiedName()
        {
            
            _fixture.Customize<DurableOrchestrationStatus>(o => o
                .With(i => i.RuntimeStatus, OrchestrationRuntimeStatus.Completed)
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
            
            var instances = _fixture.CreateMany<DurableOrchestrationStatus>(10).ToList();
            instances[3].Name = "ProjectScanSupervisor";
            
            var client = Substitute.For<DurableOrchestrationClientBase>();
            client.GetStatusAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<OrchestrationRuntimeStatus>>(), Arg.Any<int>(), string.Empty)
                .Returns(new OrchestrationStatusQueryResult { DurableOrchestrationState = instances, ContinuationToken = _finalContinuationToken });

            var func = new GetOrchestratorsByNameActivity();
            var instancesToVerify = await func.Run("ProjectScanSupervisor", client);

            instancesToVerify.Count.ShouldBe(1);
            instancesToVerify[0].Name.ShouldBe("ProjectScanSupervisor");
        }
        
        [Theory]
        [InlineData(OrchestrationRuntimeStatus.Completed, 2)]
        [InlineData(OrchestrationRuntimeStatus.Failed, 2)]
        [InlineData(OrchestrationRuntimeStatus.Pending, 0)]
        [InlineData(OrchestrationRuntimeStatus.Running, 0)]
        [InlineData(OrchestrationRuntimeStatus.Terminated, 2)]
        [InlineData(OrchestrationRuntimeStatus.ContinuedAsNew, 0)]
        public async Task ShouldReturnOnlyOrchestratorsWithStatus(OrchestrationRuntimeStatus runtimeStatus, int expectedCount)
        {
            _fixture.Customize<DurableOrchestrationStatus>(o => o
                .With(i => i.Name, "ProjectScanSupervisor")
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .Without(d => d.RuntimeStatus)
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
            
            var instances = _fixture.CreateMany<DurableOrchestrationStatus>(10).ToList();
            instances[3].RuntimeStatus = runtimeStatus;
            instances[6].RuntimeStatus = runtimeStatus;

            var client = Substitute.For<DurableOrchestrationClientBase>();
            client.GetStatusAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<OrchestrationRuntimeStatus>>(), Arg.Any<int>(), string.Empty)
                .Returns(new OrchestrationStatusQueryResult { DurableOrchestrationState = instances, ContinuationToken = _finalContinuationToken });

            var func = new GetOrchestratorsByNameActivity();
            var instancesToVerify = await func.Run("ProjectScanSupervisor", client);

            instancesToVerify.Count.ShouldBe(expectedCount);
        }
    }
}