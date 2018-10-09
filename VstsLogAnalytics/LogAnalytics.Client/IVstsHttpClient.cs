using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VstsLogAnalytics.Client
{
    public interface IVstsHttpClient
    {
        Task<IEnumerable<GitRepository>> GetRepositoriesForTeamProject(string project);

        Task<IEnumerable<PolicyConfiguration>> GetRepoPoliciesForTeamProject(string project);
    }
}