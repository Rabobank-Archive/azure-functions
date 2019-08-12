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
    public class FilterSupervisorsActivityTests
    {
        private readonly Fixture _fixture;

        public FilterSupervisorsActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
            _fixture.Customize<Orchestrator>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
        }
        
        [Fact]
        public void ShouldReturnOnlyInstancesThatHaveNotBeenScanned()
        {
            //Arrange
            var instances = _fixture.CreateMany<Orchestrator>(10).ToList();
            var instanceIds = new List<string>
            {
                instances[0].InstanceId,
                instances[3].InstanceId,
                instances[6].InstanceId,
                _fixture.Create<string>()
            };
            var request = new FilterSupervisorsRequest
            {
                AllSupervisors = instances,
                ScannedSupervisors = instanceIds
            };

            //Act
            var fun = new FilterSupervisorsActivity();
            var filteredInstances = fun.Run(request);

            //Assert
            filteredInstances.Count.ShouldBe(7);
        }
    }
}