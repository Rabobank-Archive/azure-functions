using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using CompletenessCheckFunction.Activities;
using DurableFunctionsAdministration.Client;
using DurableFunctionsAdministration.Client.Model;
using DurableFunctionsAdministration.Client.Request;
using DurableFunctionsAdministration.Client.Response;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CompletenessCheckFunction.Tests.Activities
{
    public class GetCompletedOrchestratorsWithNameActivityTests
    {
        [Fact]
        public void ShouldReturnOnlyInstancesWithSpecifiedName()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoNSubstituteCustomization());
            fixture.Customize<OrchestrationInstance>(o => o.With(i => i.RuntimeStatus, RunTimeStatusses.Completed));
            
            var instances = fixture.CreateMany<OrchestrationInstance>(10).ToList();
            instances[3].Name = "ProjectScanSupervisor";
            
            var client = Substitute.For<IDurableFunctionsAdministrationClient>();
            client.Get(Arg.Any<IRestRequest<IEnumerable<OrchestrationInstance>>>()).Returns(instances);

            var func = new GetCompletedOrchestratorsWithNameActivity(client);
            var instancesToVerify = func.Run("ProjectScanSupervisor");

            instancesToVerify.Count.ShouldBe(1);
            instancesToVerify[0].Name.ShouldBe("ProjectScanSupervisor");
        }
        
        [Theory]
        [InlineData(RunTimeStatusses.Completed, 1)]
        [InlineData(RunTimeStatusses.Failed, 0)]
        [InlineData(RunTimeStatusses.Pending, 0)]
        [InlineData(RunTimeStatusses.Running, 0)]
        [InlineData(RunTimeStatusses.Terminated, 0)]
        [InlineData(RunTimeStatusses.ContinuedAsNew, 0)]
        public void ShouldReturnOnlyCompletedOrchestrators(string runtimeStatus, int expectedCount)
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoNSubstituteCustomization());
            fixture.Customize<OrchestrationInstance>(o => o.With(i => i.Name, "ProjectScanSupervisor"));
            
            var instances = fixture.CreateMany<OrchestrationInstance>(10).ToList();
            instances[3].RuntimeStatus = runtimeStatus;
            
            var client = Substitute.For<IDurableFunctionsAdministrationClient>();
            client.Get(Arg.Any<IRestRequest<IEnumerable<OrchestrationInstance>>>()).Returns(instances);

            var func = new GetCompletedOrchestratorsWithNameActivity(client);
            var instancesToVerify = func.Run("ProjectScanSupervisor");

            instancesToVerify.Count.ShouldBe(expectedCount);
        }
    }
}