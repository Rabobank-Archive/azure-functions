using System;
using System.Collections.Generic;
using System.Linq;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class LinkCisToRepositoriesActivity
    {
        [FunctionName(nameof(LinkCisToRepositoriesActivity))]
        public ProductionItem Run(
            [ActivityTrigger] (BuildDefinition, IList<string>, string) data)
        {
            if (data.Item1 == null || data.Item2 == null || data.Item3 == null)
                throw new ArgumentNullException(nameof(data));
            var (buildPipeline, ciIdentifiers, projectId) = data;

            //TODO Repo inbouwen
            return new ProductionItem
            {
                ItemId = "",
                CiIdentifiers = ciIdentifiers
            };
        }
    }
}