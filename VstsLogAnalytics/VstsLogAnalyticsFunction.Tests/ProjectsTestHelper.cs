using System.Collections.Generic;
using SecurePipelineScan.VstsService.Response;
using Response = SecurePipelineScan.VstsService.Response;


namespace VstsLogAnalyticsFunction.Tests
{
    public class ProjectsTestHelper
    {
        public static Project CreateProjectWithParameters(string name, string id, string description, string url )
        {
            var project = new Project
            {
                Name = name,
                Id = id, 
                Description = description,
                Url = url
            };

            return project;
        }
        
        public static Project CreateDummyProject()
        {
            var project = new Project
            {
                Name = "Dummy project",
                Id = "1234", 
                Description = "Describe project",
                Url = "www.url.com"
            };

            return project;
        }

        public static Multiple<Project> CreateMultipleProjectsResponse()
        {

            var projects = new Multiple<Project>(CreateDummyProject(), CreateDummyProject());
            return projects;
        }
    }
}