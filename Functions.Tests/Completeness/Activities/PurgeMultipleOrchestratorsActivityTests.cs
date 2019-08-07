using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using NSubstitute;
using Xunit;
using Functions.Completeness.Activities;
using System;
using DurableTask.Core;
using System.Collections.Generic;

namespace Functions.Tests.Completeness.Activities
{
    public class PurgeMultipleOrchestratorsActivityTests
    {
        [Fact]
        public async Task ShouldSendDeleteCall()
        {
            //Arrange
            var client = Substitute.For<DurableOrchestrationClientBase>();

            //Act
            var func = new PurgeMultipleOrchestratorsActivity();
            await func.RunAsync(Substitute.For<DurableActivityContextBase>(), client);

            //Assert
            await client.Received().PurgeInstanceHistoryAsync(Arg.Any<DateTime>(), 
                Arg.Any<DateTime>(), Arg.Any<List<OrchestrationStatus>>());
        }
    }
}