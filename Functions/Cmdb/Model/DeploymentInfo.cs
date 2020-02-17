using System.Diagnostics.CodeAnalysis;

namespace Functions.Cmdb.Model
{
    [ExcludeFromCodeCoverage]
    public class DeploymentInfo
    {
        public string DeploymentMethod { get; set; }

        public string SupplementaryInformation { get; set; }
    }
}