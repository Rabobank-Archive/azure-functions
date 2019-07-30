namespace Functions.Helpers
{
    public static class OrchestrationIdHelper
    {
        public static string CreateProjectScanOrchestrationId(string supervisorId, string projectId)
            => $"{supervisorId}:{projectId}";

        public static string CreateProjectScanScopeOrchestrationId(string projectScanOrchestrationId, string scope)
            => $"{projectScanOrchestrationId}:{scope}";
    }
}