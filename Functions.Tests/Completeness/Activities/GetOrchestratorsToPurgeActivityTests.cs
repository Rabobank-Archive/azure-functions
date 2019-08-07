using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Completeness.Activities;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Functions.Tests.Completeness.Activities
{
    public class GetOrchestratorsToPurgeActivityTests
    {
        private readonly Fixture _fixture;

        public GetOrchestratorsToPurgeActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Theory]
        [InlineData(OrchestrationRuntimeStatus.Completed, 2)]
        [InlineData(OrchestrationRuntimeStatus.Failed, 0)]
        [InlineData(OrchestrationRuntimeStatus.Pending, 0)]
        [InlineData(OrchestrationRuntimeStatus.Running, 0)]
        [InlineData(OrchestrationRuntimeStatus.Terminated, 0)]
        [InlineData(OrchestrationRuntimeStatus.ContinuedAsNew, 0)]
        public async Task ShouldReturnOnlyOrchestratorsWithStatus(OrchestrationRuntimeStatus runtimeStatus, int expectedCount)
        {
            _fixture.Customize<DurableOrchestrationStatus>(o => o
                .With(i => i.CreatedTime, DateTime.Now.AddDays(-21))
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .Without(d => d.RuntimeStatus)
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));

            var instances = _fixture.CreateMany<DurableOrchestrationStatus>(10).ToList();
            instances[3].RuntimeStatus = runtimeStatus;
            instances[6].RuntimeStatus = runtimeStatus;

            var client = Substitute.For<DurableOrchestrationClientBase>();
            client.GetStatusAsync().Returns(instances);

            var func = new GetOrchestratorsToPurgeActivity();
            var instancesToVerify = await func.Run(Substitute.For<DurableActivityContextBase>(), client);

            instancesToVerify.Count.ShouldBe(expectedCount);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(7, 0)]
        [InlineData(14, 0)]
        [InlineData(15, 2)]
        [InlineData(30, 2)]
        public async Task ShouldReturnOnlyOrchestratorsOlderThenTwoWeeks(int daysOld, int expectedCount)
        {
            _fixture.Customize<DurableOrchestrationStatus>(o => o
                .With(i => i.CreatedTime, DateTime.Now)
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .With(d => d.RuntimeStatus, OrchestrationRuntimeStatus.Completed)
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));

            var instances = _fixture.CreateMany<DurableOrchestrationStatus>(10).ToList();
            instances[3].CreatedTime = DateTime.Now.AddDays(-daysOld);
            instances[6].CreatedTime = DateTime.Now.AddDays(-daysOld);

            var client = Substitute.For<DurableOrchestrationClientBase>();
            client.GetStatusAsync().Returns(instances);

            var func = new GetOrchestratorsToPurgeActivity();
            var instancesToVerify = await func.Run(Substitute.For<DurableActivityContextBase>(), client);

            instancesToVerify.Count.ShouldBe(expectedCount);
        }
    }
}