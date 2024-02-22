// Copyright 2024 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Orchestrator.F5BigIQ.Models
{
    internal class F5Profile
    {
        [JsonProperty("pageIndex")]
        internal int PageIndex { get; set; }
        [JsonProperty("totalPages")]
        internal int TotalPages { get; set; }
        [JsonProperty("totalItems")]
        internal int TotalCertificates { get; set; }
        [JsonProperty("items")]
        internal List<F5ProfileItem> ProfileItems { get; set; }
    }

    internal class F5ProfileItem
    { 
        [JsonProperty("name")]
        string Name { get; set; }
        [JsonProperty("certKeyChain")]
        internal List<F5ProfileCertificateKeyChain> CertificateKeyChains { get; set; }
    }

    internal class F5ProfileCertificateKeyChain
    {
        [JsonProperty("certReference")]
        internal F5ProfileCertificateReference CertificateReference { get; set; }
    }

    internal class F5ProfileCertificateReference
    {
        [JsonProperty("name")]
        string Name { get; set; }
    }
}
