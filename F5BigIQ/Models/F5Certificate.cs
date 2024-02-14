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
    internal class F5Certificate
    {
        [JsonProperty("pageIndex")]
        internal int PageIndex { get; set; }
        [JsonProperty("totalPages")]
        internal int TotalPages { get; set; }
        [JsonProperty("totalItems")]
        internal int TotalCertificates { get; set; }
        [JsonProperty("items")]
        internal List<F5CertificateItem> CertificateItems { get; set; }
    }

    internal class F5CertificateItem
    {
        [JsonProperty("id")]
        internal string Id { get; set; }
        [JsonProperty("name")]
        internal string Alias { get; set; }
        [JsonProperty("fileReference")]
        internal F5CertificateFileReference FileReference { get; set; }
        [JsonProperty("selfLink")]
        internal string Link { get; set; }
    }

    internal class F5CertificateFileReference
    {
        [JsonProperty("link")]
        internal string Link { get; set; }
    }
}
