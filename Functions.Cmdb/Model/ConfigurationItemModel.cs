using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Functions.Cmdb.Model
{
    [ExcludeFromCodeCoverage]
    public class ConfigurationItemModel
    {
        public string AssignmentGroup { get; set; }
        public string ConfigurationItem { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "rn.deployment.information")]
        public IEnumerable<DeploymentInfo> DeploymentInfo { get; set; }
    }
}
