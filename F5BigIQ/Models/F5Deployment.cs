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
    internal class F5Deployment
    {
        [JsonProperty("skipVerifyConfig")]
        internal bool SkipVerifyConfig { get { return false; } }
        [JsonProperty("skipDistribution")]
        internal bool SkipDistribution { get { return false; } }
        [JsonProperty("snapshotReference")]
        internal bool? SnapshotReference { get { return null; } }
        [JsonProperty("name")]
        internal string Name { get; set; }
        [JsonProperty("objectsToDeployReferences")]
        internal List<F5FileReference> ObjectsToDeployReferences { get; set; }
        [JsonProperty("deploySpecifiedObjectsOnly")]
        internal bool DeploySpecifiedObjectsOnly { get { return false; } }
        [JsonProperty("deviceReferences")]
        internal List<F5FileReference> DeviceReferences { get; set; }
    }
}
