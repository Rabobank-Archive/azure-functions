using Functions.Model;
using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Helpers
{
    public static class OrchestrationHelper
    {
        private const int ScopeOrchestratorComponentCount = 3; //scope orchestrator format: supervisorId:projectId:scope
        private const int SupervisorIndex = 0;
        private const int ProjectOrchestratorComponentCount = 2; //project orchestrator format: supervisorId:projectId
        private const int ProjectIdIndex = 1; 

        public static string CreateProjectScanOrchestrationId(string supervisorId, string projectId) => 
            $"{supervisorId}:{projectId}";

        public static string CreateProjectScanScopeOrchestrationId(string projectScanOrchestrationId, string scope) => 
            $"{projectScanOrchestrationId}:{scope}";

        public static string GetSuperVisorIdForScopeOrchestrator(string instanceId) =>
            GetOrchestratorId(instanceId, ScopeOrchestratorComponentCount, SupervisorIndex); 

        public static string GetSuperVisorIdForProjectOrchestrator(string instanceId) =>
            GetOrchestratorId(instanceId, ProjectOrchestratorComponentCount, SupervisorIndex); 

        public static string GetProjectIdForProjectOrchestrator(string instanceId) =>
            GetOrchestratorId(instanceId, ProjectOrchestratorComponentCount, ProjectIdIndex);

        private static string GetOrchestratorId(string instanceId, int componentCount, int indexComponent)
        {
            if (instanceId == null)
                throw new ArgumentNullException(nameof(instanceId));

            var split = instanceId.Split(':');
            return split.Length == componentCount
                ? split[indexComponent]
                : null;
        }

        public static Orchestrator ConvertToOrchestrator(DurableOrchestrationStatus orchestrator)
        {
            if (orchestrator == null)
                throw new ArgumentNullException(nameof(orchestrator));

            return new Orchestrator
            {
                Name = orchestrator.Name,
                InstanceId = orchestrator.InstanceId,
                CreatedTime = orchestrator.CreatedTime,
                RuntimeStatus = orchestrator.RuntimeStatus,
                CustomStatus = orchestrator.CustomStatus
            };
        }
    }
}