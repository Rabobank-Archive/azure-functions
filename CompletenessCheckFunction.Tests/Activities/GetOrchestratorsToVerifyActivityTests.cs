using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using CompletenessCheckFunction.Activities;
using DurableFunctionsAdministration.Client;
using DurableFunctionsAdministration.Client.Model;
using DurableFunctionsAdministration.Client.Request;
using DurableFunctionsAdministration.Client.Response;
using Microsoft.Azure.WebJobs;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CompletenessCheckFunction.Tests.Activities
{
    public class GetOrchestratorsToVerifyActivityTests
    {
        [Fact]
        public void ShouldReturnOnlySupervisors()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoNSubstituteCustomization());
            
            var instances = fixture.CreateMany<OrchestrationInstance>(10).ToList();
            instances[3].Name = "ProjectScanSupervisor";
            
            var client = Substitute.For<IDurableFunctionsAdministrationClient>();
            client.Get(Arg.Any<IRestRequest<IEnumerable<OrchestrationInstance>>>()).Returns(instances);

            var context = Substitute.For<DurableOrchestrationContextBase>();
            
            var func = new GetOrchestratorsToVerifyActivity(client);
            var instancesToVerify = func.Run(context);

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
            
            var instances = fixture.CreateMany<OrchestrationInstance>(10).ToList();
            instances[3].RuntimeStatus = runtimeStatus;
            instances[3].Name = "ProjectScanSupervisor";
            
            var client = Substitute.For<IDurableFunctionsAdministrationClient>();
            client.Get(Arg.Any<IRestRequest<IEnumerable<OrchestrationInstance>>>()).Returns(instances);

            var context = Substitute.For<DurableOrchestrationContextBase>();
            
            var func = new GetOrchestratorsToVerifyActivity(client);
            var instancesToVerify = func.Run(context);

            instancesToVerify.Count.ShouldBe(expectedCount);
        }
    }
}