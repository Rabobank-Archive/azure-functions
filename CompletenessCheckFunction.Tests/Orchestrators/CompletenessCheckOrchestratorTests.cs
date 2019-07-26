using System.Collections.Generic;
using CompletenessCheckFunction.Activities;
using CompletenessCheckFunction.Orchestrators;
using Microsoft.Azure.WebJobs;
using NSubstitute;
using System.Threading.Tasks;
using DurableFunctionsAdministration.Client.Response;
using Xunit;

namespace CompletenessCheckFunction.Tests.Orchestrators
{
    public class CompletenessCheckOrchestratorTests
    {
        //GetSupervisorOrchestrators
        //FilterOnNotYetScanned (later)
        //Foreach
        //GetSubOrchestrators
        //Supervisor.CustomStatus.TotalProjects == GetSubOrchestrators.Where(state=completed).Count
        //PostResultToLogAnalytics
        
        [Fact]
        public async Task ShouldStartActivities()
        {
            //Arrange
            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();

            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.Run(orchestrationContext);

            //Assert
            await orchestrationContext.Received().CallActivityAsync<List<OrchestrationInstance>>(nameof(GetOrchestratorsToVerifyActivity), null);
            await orchestrationContext.Received().CallActivityAsync<List<string>>(nameof(GetCompletedScansFromLogAnalyticsActivity), null);
        }
    }
}
