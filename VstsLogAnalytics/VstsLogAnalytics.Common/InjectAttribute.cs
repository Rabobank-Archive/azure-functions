using Microsoft.Azure.WebJobs.Description;
using System;

namespace VstsLogAnalytics.Common
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class InjectAttribute : Attribute
    {
    }
}