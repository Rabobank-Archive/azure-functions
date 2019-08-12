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
using NSubstitute;
using System;
using Microsoft.Azure.WebJobs;

namespace Functions.Tests.Completeness.Activities
{
    public class CreateAnalysisResultActivityTests
    {
        private readonly Fixture _fixture;
        public CreateAnalysisResultActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
            _fixture.Customize<SimpleDurableOrchestrationStatus>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
        }

        [Fact]
        public void ShouldCreateCorrectResult()
        {
            // Arrange
            var instances = _fixture.CreateMany<SimpleDurableOrchestrationStatus>(5).ToList();
            instances[0].InstanceId = "id0";
            instances[0].RuntimeStatus = OrchestrationRuntimeStatus.Failed;
            instances[1].InstanceId = "id1";
            instances[1].RuntimeStatus = OrchestrationRuntimeStatus.Completed;
            instances[2].InstanceId = "id2";
            instances[2].RuntimeStatus = OrchestrationRuntimeStatus.Canceled;
            instances[3].InstanceId = "id3";
            instances[3].RuntimeStatus = OrchestrationRuntimeStatus.Terminated;
            instances[4].InstanceId = "id4";
            instances[4].RuntimeStatus = OrchestrationRuntimeStatus.Completed;

            var request = new CreateAnalysisResultActivityRequest
            {
                AnalysisCompleted = Arg.Any<DateTime>(),
                SupervisorOrchestrator = _fixture.Create<SimpleDurableOrchestrationStatus>(),
                TotalProjectCount = Arg.Any<int>(),
                ProjectScanOrchestrators = instances
            };

            //Act
            var fun = new CreateAnalysisResultActivity();
            var analysisResult = fun.Run(request);

            //Assert
            analysisResult.ScannedProjectCount.ShouldBe(2);
            analysisResult.FailedProjectIds.ShouldBe("id0, id2, id3");
        }
    }
}