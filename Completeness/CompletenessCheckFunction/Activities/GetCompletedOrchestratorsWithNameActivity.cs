using System;
using System.Collections.Generic;
using System.Linq;
using DurableFunctionsAdministration.Client;
using DurableFunctionsAdministration.Client.Model;
using DurableFunctionsAdministration.Client.Request;
using DurableFunctionsAdministration.Client.Response;
using Microsoft.Azure.WebJobs;

namespace CompletenessCheckFunction.Activities
{
    public class GetCompletedOrchestratorsWithNameActivity
    {
        private readonly IDurableFunctionsAdministrationClient _client;
        
        public GetCompletedOrchestratorsWithNameActivity(IDurableFunctionsAdministrationClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }
        
        [FunctionName(nameof(GetCompletedOrchestratorsWithNameActivity))]
        public IList<OrchestrationInstance> Run([ActivityTrigger] string name)
        {
            var orchestrators = _client.Get(OrchestrationInstances.List())
                .Where(i => i.Name == name && i.RuntimeStatus == RunTimeStatusses.Completed)
                .ToList();
            
            return orchestrators;
        }
    }
}