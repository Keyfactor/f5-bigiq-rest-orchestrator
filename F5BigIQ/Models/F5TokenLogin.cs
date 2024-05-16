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
        internal string UserId { get; set; }
        [JsonProperty("password")]
        internal string Password { get; set; }
        [JsonProperty("loginProviderName")]
        internal string LoginProviderName { get; set; }
    }

    internal class F5LoginResponse
    {
        [JsonProperty("token")]
        internal F5LoginToken Token { get; set; }
        [JsonProperty("refreshToken")]
        internal F5LoginToken RefreshToken { get; set; }
    }

    internal class F5LoginToken
    {
        [JsonProperty("token")]
        internal string Token { get; set; }
    }



    internal class F5TokenRefreshRequest
    {
        [JsonProperty("refreshToken")]
        internal F5TokenRefreshRequestToken RefreshToken { get; set; }
    }

    internal class F5TokenRefreshRequestToken
    {
        [JsonProperty("token")]
        internal string Token { get; set; }
    }

}
