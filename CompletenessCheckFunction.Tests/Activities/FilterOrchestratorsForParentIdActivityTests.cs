using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using CompletenessCheckFunction.Activities;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;
using Shouldly;
using Xunit;

namespace CompletenessCheckFunction.Tests.Activities
{
    public class FilterOrchestratorsForParentIdActivityTests
    {
        private readonly Fixture _fixture;
        public FilterOrchestratorsForParentIdActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public void ShouldReturnOnlyInstancesForParent()
        {
            //Arrange
            var request = new FilterOrchestratorsForParentIdActivityRequest
            {
                ParentId = "1234-5678-90",
                InstancesToFilter = CreateInstancesList("1234-5678-90", 10, 20)
            };
            
            //Act
            var fun = new FilterOrchestratorsForParentIdActivity();
            var filteredInstances = fun.Run(request);
            
            //Assert
            filteredInstances.Count.ShouldBe(10);
        }

        [Fact]
        public void ShouldNotCrashWhenNoParentInInstanceId()
        {
            //Arrange
            var request = new FilterOrchestratorsForParentIdActivityRequest
            {
                ParentId = "1234-5678-90",
                InstancesToFilter = _fixture.CreateMany<OrchestrationInstance>(10).ToList()
            };
            
            //Act
            var fun = new FilterOrchestratorsForParentIdActivity();
            var filteredInstances = fun.Run(request);
            
            //Assert
            filteredInstances.Count.ShouldBe(0);
        }

        private List<OrchestrationInstance> CreateInstancesList(string parentId, int countWithParentId, int countWithoutParentId)
        {
            var withParentId = _fixture.Build<OrchestrationInstance>()
                .With(o => o.InstanceId, $"{parentId}:{_fixture.Create<string>()}").CreateMany(countWithParentId);
            var withoutParentId = _fixture.Build<OrchestrationInstance>()
                .With(o => o.InstanceId, $"Not{parentId}:{_fixture.Create<string>()}").CreateMany(countWithoutParentId);
            return withParentId.Union(withoutParentId).ToList();
        }
    }
}