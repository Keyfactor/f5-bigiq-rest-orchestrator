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
    internal class F5CertificateAddRequest
    {
        [JsonProperty("command")]
        internal string Command { get; set; }
        [JsonProperty("pkcs12Passphrase")]
        internal string Password { get; set; }
        [JsonProperty("filePath")]
        internal string FileLocation { get; set; }
        [JsonProperty("itemName")]
        internal string Alias { get; set; }
        [JsonProperty("securityType")]
        internal string SecurityType { get { return "normal"; } }
        [JsonProperty("itemPartition")]
        internal string Partition { get; set; }
        [JsonProperty("certReference")]
        internal CertificateReference CertReference { get; set; }
        [JsonProperty("keyReference")]
        internal CertificateReference KeyReference { get; set; }
    }

    internal class CertificateReference
    {
        [JsonProperty("link")]
        internal string Link { get; set; }
    }
}
