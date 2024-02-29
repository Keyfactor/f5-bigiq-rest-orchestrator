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
    internal class F5VirtualServer
    {
        [JsonProperty("pageIndex")]
        internal int PageIndex { get; set; }
        [JsonProperty("totalPages")]
        internal int TotalPages { get; set; }
        [JsonProperty("totalItems")]
        internal int TotalVirtualServers { get; set; }
        [JsonProperty("items")]
        internal List<F5VirtualServerItem> VirtualServerItems { get; set; }
    }

    internal class F5VirtualServerItem
    {
        [JsonProperty("name")]
        internal string Name { get; set; }
        [JsonProperty("selfLink")]
        internal string ItemLink { get; set; }
        [JsonProperty("deviceReference")]
        internal F5VirtualServerDeviceReference VirtualServerDeviceReference { get; set; }
        [JsonProperty("profilesCollectionReference")]
        internal F5VirtualServerProfilesCollectionReference VirtualServerProfilesCollectionReference { get; set; }
    }

    internal class F5VirtualServerDeviceReference
    { 
        [JsonProperty("link")]
        internal string ItemLink { get; set; }
    }

    internal class F5VirtualServerProfilesCollectionReference
    {
        [JsonProperty("link")]
        internal string ItemLink { get; set; }
    }
}
