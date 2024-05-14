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
    internal class F5CertificateCSRRequest
    {
        [JsonProperty("command")]
        internal string Command { get; set; }
        [JsonProperty("itemPartition")]
        internal string ItemPartition { get; set; }
        [JsonProperty("itemName")]
        internal string ItemName { get; set; }
        [JsonProperty("commonName")]
        internal string CommonName { get; set; }
        [JsonProperty("country")]
        internal string Country { get; set; }
        [JsonProperty("state")]
        internal string State { get; set; }
        [JsonProperty("locality")]
        internal string Locality { get; set; }
        [JsonProperty("organization")]
        internal string Organization { get; set; }
        [JsonProperty("division")]
        internal string OrganizationalUnit { get; set; }
        [JsonProperty("email")]
        internal string Email { get; set; }
        [JsonProperty("subjectAlternativeName")]
        internal string SubjectAlternativeNames { get; set; }
        [JsonProperty("keyType")]
        internal string KeyType { get; set; }
        [JsonProperty("keySize")]
        internal int? KeySize { get; set; }
    }

    internal class F5CSRReference
    {
        [JsonProperty("selfLink")]
        internal string Link { get; set; }
    }

    internal class F5CSRResult
    {
        [JsonProperty("status")]
        internal string Status { get; set; }
        [JsonProperty("errorMessage")]
        internal string ErrorMessage { get; set; }
        [JsonProperty("csrText")]
        internal string CSR { get; set; }
    }
}
