using Microsoft.WindowsAzure.Storage.Table;

namespace Functions.Model
{
    public class DeploymentMethodEntity : TableEntity
    {
        public DeploymentMethodEntity(string rowKey, string partitionKey)
        {
            this.RowKey = rowKey;
            this.PartitionKey = partitionKey;
        }

        public DeploymentMethodEntity() { }

        public string CiIdentifier { get; set; }
        public string Organization { get; set; }
        public string ProjectId { get; set; }
        public string PipelineId { get; set; }
        public string StageId { get; set; }
    }
}