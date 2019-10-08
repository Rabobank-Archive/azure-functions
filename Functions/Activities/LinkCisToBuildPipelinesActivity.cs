using System;
using System.Collections.Generic;
using System.Linq;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class LinkCisToBuildPipelinesActivity
    {
        [FunctionName(nameof(LinkCisToBuildPipelinesActivity))]
        public IList<ProductionItem> Run(
            [ActivityTrigger] (ReleaseDefinition, IList<string>, string) data)
        {
            if (data.Item1 == null || data.Item2 == null || data.Item3 == null)
                throw new ArgumentNullException(nameof(data));
            var (releasePipeline, ciIdentifiers, projectId) = data;

            return releasePipeline.Artifacts
                .Where(a => a.Type == "Build" && a.DefinitionReference?.Project?.Id == projectId)
                .Select(a => new ProductionItem
                {
                    ItemId = a.DefinitionReference.Definition.Id,
                    CiIdentifiers = ciIdentifiers
                })
                .ToList();
        }
    }
}