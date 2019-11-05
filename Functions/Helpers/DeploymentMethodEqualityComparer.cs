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
                throw new ArgumentNullException(nameof(x));

            if (y == null)
                throw new ArgumentNullException(nameof(y));

            return x.CiIdentifier == y.CiIdentifier &&
                   x.PipelineId == y.PipelineId &&
                   x.StageId == y.StageId;
        }

        public int GetHashCode(DeploymentMethod obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return GetHashCodeForString(obj.CiIdentifier) ^
                   GetHashCodeForString(obj.PipelineId) ^
                   GetHashCodeForString(obj.StageId);
        }

        private static int GetHashCodeForString(string obj)
        {
            return obj?.GetHashCode() ?? string.Empty.GetHashCode();
        }
    }
}