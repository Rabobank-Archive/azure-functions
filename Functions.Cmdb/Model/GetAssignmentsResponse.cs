using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Functions.Cmdb.Model
{
    [ExcludeFromCodeCoverage]
    public class GetAssignmentsResponse
    {
        public IEnumerable<AssignmentContentItem> Content { get; set; }
    }
}
