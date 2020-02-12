using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Functions.Tests.Activities
{
    public class FilterSupervisorsActivityTests
    {
        private readonly Fixture _fixture;

        public FilterSupervisorsActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
            _fixture.Customize<Orchestrator>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase()))
                .With(d => d.RuntimeStatus, OrchestrationRuntimeStatus.Completed));
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

            //Act
            var fun = new FilterSupervisorsActivity();
            var filteredInstances = fun.Run((instances, instanceIds), _fixture.Create<ILogger>());

            //Assert
            filteredInstances.Count.ShouldBe(7);
        }
    }
}