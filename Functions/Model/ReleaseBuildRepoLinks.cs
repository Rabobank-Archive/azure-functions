using System.Collections.Generic;

namespace Functions.Model
{
    public class ReleaseBuildsReposLink
    {
        public string ReleasePipelineId { get; set; }
        public IList<string> BuildPipelineIds { get; set; }
        public IList<string> RepositoryIds { get; set; }

    }
}