using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Functions.Cmdb.Model
{
    [ExcludeFromCodeCoverage]
    public class GetCiResponse
    {
        [JsonProperty(PropertyName = "content")]
        public IEnumerable<CiContentItem> Content { get; set; }
    }
}
