using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Completeness.Activities;
using Functions.Completeness.Requests;
using Functions.Completeness.Model;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Functions.Tests.Completeness.Activities
{
    public class FilterAlreadyAnalyzedOrchestratorsActivityTests
    {
        private readonly Fixture _fixture;

        public FilterAlreadyAnalyzedOrchestratorsActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
            _fixture.Customize<SimpleDurableOrchestrationStatus>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
        }
        
        [Fact]
        public void ShouldReturnOnlyInstancesThatHaveNotBeenScanned()
        {
            //Arrange
            var instances = _fixture.CreateMany<SimpleDurableOrchestrationStatus>(10).ToList();
            var instanceIds = new List<string>
            {
                instances[0].InstanceId,
                instances[3].InstanceId,
                instances[6].InstanceId,
                _fixture.Create<string>()
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