using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Completeness.Activities;
using Functions.Completeness.Requests;
using Functions.Completeness.Model;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Functions.Tests.Completeness.Activities
{
    public class FilterOrchestratorsForParentIdActivityTests
    {
        private readonly Fixture _fixture;
        public FilterOrchestratorsForParentIdActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
            _fixture.Customize<SimpleDurableOrchestrationStatus>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
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
                InstancesToFilter = _fixture.CreateMany<SimpleDurableOrchestrationStatus>(10).ToList()
            };
            
            //Act
            var fun = new FilterOrchestratorsForParentIdActivity();
            var filteredInstances = fun.Run(request);
            
            //Assert
            filteredInstances.Count.ShouldBe(0);
        }

        private List<SimpleDurableOrchestrationStatus> CreateInstancesList(string parentId, 
            int countWithParentId, int countWithoutParentId)
        {
            var withParentId = _fixture.Build<SimpleDurableOrchestrationStatus>()
                .With(o => o.InstanceId, $"{parentId}:{_fixture.Create<string>()}")
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase()))
                .CreateMany(countWithParentId);
            
            var withoutParentId = _fixture.Build<SimpleDurableOrchestrationStatus>()
                .With(o => o.InstanceId, $"Not{parentId}:{_fixture.Create<string>()}")
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase()))
                .CreateMany(countWithoutParentId);
            
            return withParentId.Union(withoutParentId).ToList();
        }
    }
}