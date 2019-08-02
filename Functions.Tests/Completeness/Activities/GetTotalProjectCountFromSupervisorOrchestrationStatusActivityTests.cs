using AutoFixture;
using AzDoCompliancy.CustomStatus;
using Functions.Completeness.Activities;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Functions.Tests.Completeness.Activities
{
    public class GetTotalProjectCountFromSupervisorOrchestrationStatusActivityTests
    {
        private readonly Fixture _fixture;

        public GetTotalProjectCountFromSupervisorOrchestrationStatusActivityTests()
        {
            _fixture = new Fixture();
                
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
            _fixture.Customize<DurableOrchestrationStatus>(x => x
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .With(d => d.CustomStatus,
                    JToken.FromObject(new SupervisorOrchestrationStatus {TotalProjectCount = count})));
            
            var instanceToGetCountFrom = _fixture.Create<DurableOrchestrationStatus>();
            
            // Act
            var fun = new GetTotalProjectCountFromSupervisorOrchestrationStatusActivity();
            var result = fun.Run(instanceToGetCountFrom);
            
            // Assert
            result.ShouldBe(count);
        }
        
        [Fact]
        public void ShouldReturnNullIfNoTotalProjectCountPresent()
        {
            // Arrange
            _fixture.Customize<DurableOrchestrationStatus>(x => x
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .With(d => d.CustomStatus,
                    JToken.FromObject(new CustomStatusBase())));
            
            var instanceToGetCountFrom = _fixture.Create<DurableOrchestrationStatus>();
            
            // Act
            var fun = new GetTotalProjectCountFromSupervisorOrchestrationStatusActivity();
            var result = fun.Run(instanceToGetCountFrom);
            
            // Assert
            result.ShouldBeNull();
        }
        
        [Fact]
        public void ShouldReturnNullIfNoCustomStatusSet()
        {
            // Arrange
            _fixture.Customize<DurableOrchestrationStatus>(x => x
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .Without(d => d.CustomStatus));
            
            var instanceToGetCountFrom = _fixture.Create<DurableOrchestrationStatus>();
            
            // Act
            var fun = new GetTotalProjectCountFromSupervisorOrchestrationStatusActivity();
            var result = fun.Run(instanceToGetCountFrom);
            
            // Assert
            result.ShouldBeNull();
        }
    }
}