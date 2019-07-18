namespace Functions.Helpers
{
    public static class OrchestrationIdHelper
    {
        public static string CreateProjectScanOrchestrationId(string supervisorId, string projectId)
            => $"{supervisorId}:{projectId}";
    }
}