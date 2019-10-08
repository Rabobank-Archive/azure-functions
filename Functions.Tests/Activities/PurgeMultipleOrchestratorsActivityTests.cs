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
            var context = Substitute.For<DurableActivityContextBase>();
            var client = Substitute.For<DurableOrchestrationClientBase>();

            //Act
            var func = new PurgeMultipleOrchestratorsActivity();
            await func.RunAsync(context, client);

            //Assert
            await client.ReceivedWithAnyArgs().PurgeInstanceHistoryAsync(new DateTime(),
               new DateTime(), new List<OrchestrationStatus>());
        }
    }
}