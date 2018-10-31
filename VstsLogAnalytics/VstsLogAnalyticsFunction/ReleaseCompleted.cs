namespace VstsLogAnalyticsFunction
{
    public class Release
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Deployment
    {
        public int Id { get; set; }
        public Release Release { get; set; }
    }

    public class Resource
    {
        public SecurePipelineScan.VstsService.Response.Environment Environment { get; set; }
        public Project Project { get; set; }
        public Deployment Deployment { get; set; }
    }

    public class ReleaseCompleted
    {
        public string Id { get; set; }
        public string EventType { get; set; }
        public string PublisherId { get; set; }
        public Resource Resource { get; set; }
        public string ResourceVersion { get; set; }
    }
}