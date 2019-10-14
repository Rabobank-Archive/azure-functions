using Functions.Model;
using Microsoft.Azure.WebJobs;
using System;
using System.Linq;

namespace Functions.Helpers
{
    public static class OrchestrationHelper
    {
        public static string CreateProjectScanOrchestrationId(string supervisorId, string projectId) => 
            $"{supervisorId}:{projectId}";

        public static string CreateProjectScanScopeOrchestrationId(string projectScanOrchestrationId, string scope) => 
            $"{projectScanOrchestrationId}:{scope}";

        public static string GetSupervisorId(string instanceId)
        {
            if (instanceId == null)
                throw new ArgumentNullException(nameof(instanceId));

            return instanceId.Contains(":") ? instanceId.Split(':').First() : null;
        }
        
        public static string GetProjectId(string instanceId)
        {
            if (instanceId == null)
                throw new ArgumentNullException(nameof(instanceId));

            return instanceId.Contains(":") ? instanceId.Split(':')[1] : null;
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