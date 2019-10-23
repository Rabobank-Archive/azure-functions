using System.Collections.Generic;

namespace Functions.Model
{
    public class ProductionItem
    {
        public string ItemId { get; set; }
        public IList<DeploymentMethod> DeploymentInfo { get; set; }
    }
}