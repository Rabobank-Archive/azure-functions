using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VstsLogAnalyticsFunction
{
    public class VstsHttpClient
    {
        private string _projectUrl;
        private string _pat;
        VssBasicCredential _cred;

        public VstsHttpClient(string projectUrl, string pat)
        {
            _projectUrl = projectUrl;
            _pat = pat;

            _cred = new VssBasicCredential(string.Empty, "");
        }

        public async Task<List<GitRepository>> GetRepositoriesForTeamProject(string project)
        {
            using (var connection = new VssConnection(new Uri(_projectUrl), _cred))
            {
                var git = connection.GetClient<GitHttpClient>();

                return await git.GetRepositoriesAsync(project);
            }
        }
    }
}
