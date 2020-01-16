using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using Functions.Activities;
using System;
using DurableTask.Core;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Tests.Activities
{
    public class PurgeMultipleOrchestratorsActivityTests
    {
        [Fact]
        public async Task ShouldSendDeleteCall()
        {
            //Arrange
            var context = Substitute.For<IDurableActivityContext>();
            var client = Substitute.For<IDurableOrchestrationClient>();

            //Act
            var func = new PurgeMultipleOrchestratorsActivity();
            await func.RunAsync(context, client);

            //Assert
            await client.ReceivedWithAnyArgs().PurgeInstanceHistoryAsync(new DateTime(),
               new DateTime(), new List<OrchestrationStatus>());
        }
    }
}