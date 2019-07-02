using System;

namespace Functions.LogItems
{
    internal class LogAnalyticsAgentStatus
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public bool Enabled { get; set; }
        public string Status { get; set; }
        public int StatusCode { get; set; }

        public string Pool { get; set; }
        public string Version { get; set; }
        public string AssignedTask { get; set; }
        public DateTime Date { get; internal set; }
    }
}