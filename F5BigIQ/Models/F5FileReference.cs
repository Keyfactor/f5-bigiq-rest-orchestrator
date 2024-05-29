using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.Extensions.Orchestrator.F5BigIQ.Models
{
    internal class F5FileReference
    {
        [JsonProperty("link")]
        internal string Link { get; set; }
    }
}
