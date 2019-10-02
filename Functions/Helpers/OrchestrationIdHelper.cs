using System;
using System.Linq;

namespace Functions.Helpers
{
    public static class OrchestrationIdHelper
    {
        public static string CreateProjectScanOrchestrationId(string supervisorId, string projectId)
            => $"{supervisorId}:{projectId}";

        public static string CreateProjectScanScopeOrchestrationId(string projectScanOrchestrationId, string scope)
            => $"{projectScanOrchestrationId}:{scope}";

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
    }
}