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
    public class FilterProjectScannersActivityTests
    {
        private readonly Fixture _fixture;
        public FilterProjectScannersActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
            _fixture.Customize<Orchestrator>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
        }

        [Fact]
        public void ShouldReturnOnlyInstancesForParent()
        {
            //Arrange
            var request = new SingleCompletenessCheckRequest
            {
                Supervisor = _fixture.Build<Orchestrator>()
                    .With(o => o.InstanceId, "1234-5678-90")
                    .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase()))
                    .Create(),
                AllProjectScanners = CreateInstancesList("1234-5678-90", 10, 20)
            };

            //Act
            var fun = new FilterProjectScannersActivity();
            var filteredInstances = fun.Run(request);

            //Assert
            filteredInstances.Count.ShouldBe(10);
        }

        [Fact]
        public void ShouldNotCrashWhenNoParentInInstanceId()
        {
            //Arrange
            var request = new SingleCompletenessCheckRequest
            {
                Supervisor = _fixture.Create<Orchestrator>(),
                AllProjectScanners = _fixture.CreateMany<Orchestrator>(10).ToList()
            };

            //Act
            var fun = new FilterProjectScannersActivity();
            var filteredInstances = fun.Run(request);

            //Assert
            filteredInstances.Count.ShouldBe(0);
        }

        private List<Orchestrator> CreateInstancesList(string parentId, int countWithParentId, int countWithoutParentId)
        {
            var withParentId = _fixture.Build<Orchestrator>()
                .With(o => o.InstanceId, $"{parentId}:{_fixture.Create<string>()}")
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase()))
                .CreateMany(countWithParentId);

            var withoutParentId = _fixture.Build<Orchestrator>()
                .With(o => o.InstanceId, $"Not{parentId}:{_fixture.Create<string>()}")
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase()))
                .CreateMany(countWithoutParentId);

            return withParentId.Union(withoutParentId).ToList();
        }
    }
}