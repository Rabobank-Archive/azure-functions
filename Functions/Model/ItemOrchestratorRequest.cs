using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;

namespace Functions.Model
{
    public class ItemOrchestratorRequest
    {
        public Project Project { get; set; }
        public IList<ProductionItem> ProductionItems { get; set; }
    }

    public class ProductionItem
    {
        public string ItemId { get; set; }
        public IList<string> CiIdentifiers { get; set; }
    }
}