using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;
using System;
using Microsoft.Azure.WebJobs;

namespace Functions.Tests.Activities
{
    public class CreateCompletenessLogItemActivityTests
    {
        private readonly Fixture _fixture;

        public CreateCompletenessLogItemActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
            _fixture.Customize<Orchestrator>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(6)]
        [InlineData(23)]
        public void ShouldReturnCountIfTotalProjectCountPresent(int count)
        {
            // Arrange
            _fixture.Customize<Orchestrator>(x => x
                .With(d => d.CustomStatus,
                    JToken.FromObject(new SupervisorOrchestrationStatus { TotalProjectCount = count })));

            var analysisCompleted = new DateTime();
            var supervisor = _fixture.Create<Orchestrator>();
            var projectScanners = _fixture.CreateMany<Orchestrator>(1).ToList();

            //Act
            var fun = new CreateCompletenessLogItemActivity();
            var analysisResult = fun.Run((analysisCompleted, supervisor, projectScanners));

            // Assert
            analysisResult.TotalProjectCount.ShouldBe(count);
        }

        [Fact]
        public void ShouldReturnNullIfNoTotalProjectCountPresent()
        {
            // Arrange
            _fixture.Customize<Orchestrator>(x => x
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));

            var analysisCompleted = new DateTime();
            var supervisor = _fixture.Create<Orchestrator>();
            var projectScanners = _fixture.CreateMany<Orchestrator>(1).ToList();

            //Act
            var fun = new CreateCompletenessLogItemActivity();
            var analysisResult = fun.Run((analysisCompleted, supervisor, projectScanners));

            // Assert
            analysisResult.TotalProjectCount.ShouldBeNull();
        }

        [Fact]
        public void ShouldReturnNullIfNoCustomStatusSet()
        {
            // Arrange
            _fixture.Customize<Orchestrator>(x => x
                .Without(d => d.CustomStatus));

            var analysisCompleted = new DateTime();
            var supervisor = _fixture.Create<Orchestrator>();
            var projectScanners = _fixture.CreateMany<Orchestrator>(1).ToList();

            //Act
            var fun = new CreateCompletenessLogItemActivity();
            var analysisResult = fun.Run((analysisCompleted, supervisor, projectScanners));

            // Assert
            analysisResult.TotalProjectCount.ShouldBeNull();
        }

        [Fact]
        public void ShouldOnlyCountCompletedProjectScans()
        {
            // Arrange
            var projectScanners = _fixture.CreateMany<Orchestrator>(5).ToList();
            projectScanners[0].RuntimeStatus = OrchestrationRuntimeStatus.Failed;
            projectScanners[1].RuntimeStatus = OrchestrationRuntimeStatus.Completed;
            projectScanners[2].RuntimeStatus = OrchestrationRuntimeStatus.Canceled;
            projectScanners[3].RuntimeStatus = OrchestrationRuntimeStatus.Terminated;
            projectScanners[4].RuntimeStatus = OrchestrationRuntimeStatus.Completed;

            var analysisCompleted = new DateTime();
            var supervisor = _fixture.Create<Orchestrator>();

            //Act
            var fun = new CreateCompletenessLogItemActivity();
            var analysisResult = fun.Run((analysisCompleted, supervisor, projectScanners));

            //Assert
            analysisResult.ScannedProjectCount.ShouldBe(2);
        }

        [Fact]
        public void ShouldGetProjectIdsFromFailedScans()
        {
            // Arrange
            var projectScanners = _fixture.CreateMany<Orchestrator>(5).ToList();
            projectScanners[0].InstanceId = "supervisorid0:projectid0";
            projectScanners[0].RuntimeStatus = OrchestrationRuntimeStatus.Failed;
            projectScanners[1].InstanceId = "supervisorid1:projectid1";
            projectScanners[1].RuntimeStatus = OrchestrationRuntimeStatus.Completed;
            projectScanners[2].InstanceId = "supervisorid2:projectid2";
            projectScanners[2].RuntimeStatus = OrchestrationRuntimeStatus.Canceled;
            projectScanners[3].InstanceId = "supervisorid3:projectid3";
            projectScanners[3].RuntimeStatus = OrchestrationRuntimeStatus.Terminated;
            projectScanners[4].InstanceId = "supervisorid4:projectid4";
            projectScanners[4].RuntimeStatus = OrchestrationRuntimeStatus.Completed;

            var analysisCompleted = new DateTime();
            var supervisor = _fixture.Create<Orchestrator>();

            //Act
            var fun = new CreateCompletenessLogItemActivity();
            var analysisResult = fun.Run((analysisCompleted, supervisor, projectScanners));

            //Assert
            analysisResult.FailedProjectIds.ShouldBe("projectid0, projectid2, projectid3");
        }
    }
}