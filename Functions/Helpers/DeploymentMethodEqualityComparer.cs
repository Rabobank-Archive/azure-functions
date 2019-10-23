using System;
using System.Collections.Generic;
using Functions.Model;

namespace Functions.Helpers
{
    public class DeploymentMethodEqualityComparer : IEqualityComparer<DeploymentMethod>
    {
        public bool Equals(DeploymentMethod x, DeploymentMethod y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }

            // just to satisfy SonarQube
            var result = x.CiIdentifier == y.CiIdentifier &&
                         x.Organization == y.Organization;
            result = result &&
                     x.ProjectId == y.ProjectId &&
                     x.PipelineId == y.PipelineId;
            return result && x.StageId == y.StageId;
        }

        public int GetHashCode(DeploymentMethod obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return GetHashCodeForString(obj.CiIdentifier) ^
                   GetHashCodeForString(obj.Organization) ^
                   GetHashCodeForString(obj.ProjectId) ^
                   GetHashCodeForString(obj.PipelineId) ^
                   GetHashCodeForString(obj.StageId);
        }

        private static int GetHashCodeForString(string obj)
        {
            return obj?.GetHashCode() ?? string.Empty.GetHashCode();
        }
    }
}