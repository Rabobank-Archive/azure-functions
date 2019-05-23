namespace Functions
{
    public class AgentInformation
    {
        public AgentInformation(string resourceGroup, int instanceId)
        {
            ResourceGroup = resourceGroup;
            InstanceId = instanceId;
        }

        public string ResourceGroup { get; set; }

        public int InstanceId { get; set; }
    }
}