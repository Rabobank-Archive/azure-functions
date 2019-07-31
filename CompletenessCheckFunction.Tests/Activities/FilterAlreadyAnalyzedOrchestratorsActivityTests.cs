using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using CompletenessCheckFunction.Activities;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;
using Shouldly;
using Xunit;

namespace CompletenessCheckFunction.Tests.Activities
{
    public class FilterAlreadyAnalyzedOrchestratorsActivityTests
    {
        [Fact]
        public void ShouldReturnOnlyInstancesThatHaveNotBeenScanned()
        {
            //Arrange
            var fixture = new Fixture();
            var instances = fixture.CreateMany<OrchestrationInstance>(10).ToList();
            var instanceIds = new List<string>
            {
                instances[0].InstanceId,
                instances[3].InstanceId,
                instances[6].InstanceId,
                fixture.Create<string>()
            };
            var request = new FilterAlreadyAnalyzedOrchestratorsActivityRequest
            {
                InstancesToAnalyze = instances,
                InstanceIdsAlreadyAnalyzed = instanceIds
            };

            //Act
            var fun = new FilterAlreadyAnalyzedOrchestratorsActivity();
            var filteredInstances = fun.Run(request);

            //Assert
            filteredInstances.Count.ShouldBe(7);
        }
    }
}