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
            string[] ids = instanceId.Split(':');
                        
            if (ids.Length < 3)
            {
                return null;
            }
            
            return ids[0];
        }
    }
}