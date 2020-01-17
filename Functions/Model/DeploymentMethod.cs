using Microsoft.Azure.Cosmos.Table;

namespace Functions.Model
{
    public class DeploymentMethod : TableEntity
    {
        public DeploymentMethod(string rowKey, string partitionKey)
        {
            this.RowKey = rowKey;
            this.PartitionKey = partitionKey;
        }

        public DeploymentMethod() { }

        public string CiIdentifier { get; set; }
        public string CiName { get; set; }
        public bool IsSoxApplication { get; set; }
        public string Organization { get; set; }
        public string ProjectId { get; set; }
        public string PipelineId { get; set; }
        public string StageId { get; set; }
    }
}