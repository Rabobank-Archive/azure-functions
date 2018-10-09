using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VstsLogAnalytics.Client
{
    public interface IVstsHttpClient
    {
        Task<List<GitRepository>> GetRepositoriesForTeamProject(string project);

        Task<List<PolicyConfiguration>> GetRepoPoliciesForTeamProject(string project);
    }
}