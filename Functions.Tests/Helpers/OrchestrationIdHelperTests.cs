using Xunit;
using Shouldly;
using static Functions.Helpers.OrchestrationIdHelper;

namespace Functions.Tests.Helpers
{
    public class OrchestrationIdHelperTests
    {
        [Fact]
        public void GetSupervisorIdShouldReturnNullWhenNoSupervisorId()
        {
            //Arrange
            var instanceId = "blablabla";

            //Act
            var result = GetSupervisorId(instanceId);

            //Assert
            result.ShouldBeNull();
        }
        
        [Fact]
        public void ProjectScanOrchestrationIdShouldReturnSupervisorId()
        {
            //Arrange
            var supervisorId = "supervisorId";
            var projectId = "projectId";
            var instanceId = CreateProjectScanOrchestrationId(supervisorId, projectId);

            //Act
            var result = GetSupervisorId(instanceId);

            //Assert
            result.ShouldBe(supervisorId);
        }
        
        [Fact]
        public void ProjectScanScopeOrchestrationIdShouldReturnSupervisorId()
        {
            //Arrange
            var supervisorId = "supervisorId";
            var projectId = "projectId";
            var scope = "globalpermissions";
            var instanceId = CreateProjectScanScopeOrchestrationId(
                CreateProjectScanOrchestrationId(supervisorId, projectId),
                scope);

            //Act
            var result = GetSupervisorId(instanceId);

            //Assert
            result.ShouldBe(supervisorId);
        }
    }
}