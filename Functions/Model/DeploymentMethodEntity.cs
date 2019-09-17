using Microsoft.WindowsAzure.Storage.Table;

namespace Functions.Model
{
    public class DeploymentMethodEntity : TableEntity
    {
        public DeploymentMethodEntity(string rowKey, string projectId)
        {
            this.RowKey = rowKey;
            this.PartitionKey = projectId;
        }

        public DeploymentMethodEntity() { }

        public string CiIdentifier { get; set; }
        public string Organization { get; set; }
        public string PipelineId { get; set; }
        public string StageId { get; set; }
    }
}