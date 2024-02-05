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
    internal class F5LoginRequest
    {
        [JsonProperty("username")]
        public string UserId { get; set; }
        [JsonProperty("password")]
        public string Password { get; set; }
        [JsonProperty("loginProviderName")]
        public string ProviderName { get; set; }
    }

    internal class F5LoginResponse
    {
        [JsonProperty("token")]
        public F5LoginToken Token { get; set; }
    }

    internal class F5LoginToken
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }

}
