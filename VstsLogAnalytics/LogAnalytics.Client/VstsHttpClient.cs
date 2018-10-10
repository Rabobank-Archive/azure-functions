using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VstsLogAnalytics.Client
{
    public class VstsHttpClient : IVstsHttpClient
    {
        private string _projectUrl;
        private string _pat;
        private VssBasicCredential _cred;

        public VstsHttpClient(string projectUrl, string pat)
        {
            _projectUrl = projectUrl;
            _pat = pat;

            _cred = new VssBasicCredential(string.Empty, pat);
        }

        public async Task<IEnumerable<GitRepository>> GetRepositoriesForTeamProject(string project)
        {
            using (var connection = new VssConnection(new Uri(_projectUrl), _cred))
            {
                var git = connection.GetClient<GitHttpClient>();

                return await git.GetRepositoriesAsync(project);
            }
        }

        public async Task<IEnumerable<PolicyConfiguration>> GetRepoPoliciesForTeamProject(string project)
        {
            using (var connection = new VssConnection(new Uri(_projectUrl), _cred))
            {
                var policyClient = connection.GetClient<PolicyHttpClient>();

                return await policyClient.GetPolicyConfigurationsAsync(project);
            }
        }
    }
}