using Functions.Model;
using SecurePipelineScan.VstsService.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace Functions.Helpers
{
    public static class LinkCisToItemHelper
    {
        public static IList<ProductionItem> LinkCisToBuildPipelines(
            IList<ReleaseDefinition> releasePipelines, ItemOrchestratorRequest request)
        {
            if (releasePipelines == null)
                throw new ArgumentNullException(nameof(releasePipelines));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return null;
        }
    }
}
