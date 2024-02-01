﻿// Copyright 2024 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.
using System.Collections.Generic;
using System.Text;
using System;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using RestSharp;
using RestSharp.Authenticators;

using Keyfactor.Logging;
using Keyfactor.Extensions.Orchestrator.F5BigIQ.Models;

namespace Keyfactor.Extensions.Orchestrator.F5BigIQ
{
    internal class F5BigIQClient
    {
        ILogger logger;
        RestClient Client { get; set; }

        F5BigIQClient(string baseUrl, string id, string pswd, bool useTokenAuth)
        {
            logger = LogHandler.GetClassLogger<F5BigIQClient>();
            Client = GetRestClient(baseUrl, id, pswd, useTokenAuth); 
        }

        internal List<F5Certificate> GetCertificates()
        {
            logger.MethodEntry(LogLevel.Debug);

            List<F5Certificate> sites = new List<F5Certificate>();

            int totalPages = 0;

            do
            {
                string RESOURCE = $"/mgmt/cm/adc-core/working-config/sys/file/ssl-cert?top=50";
                RestRequest request = new RestRequest(RESOURCE, Method.Get);

                JObject json = SubmitRequest(request);
                List<F5Certificate> pageOfSites = JsonConvert.DeserializeObject<List<F5Certificate>>(json.ToString());
                if (pageOfSites.Count == 0)
                    break;
                else
                    page++;

                sites.AddRange(pageOfSites);
            } while (1 == 1);


            logger.MethodExit(LogLevel.Debug);

            return sites;
        }

        private string GetAccessToken(string id, string pswd)
        {
            logger.MethodEntry(LogLevel.Debug);
            logger.MethodExit(LogLevel.Debug);

            return "";
        }

        private JObject SubmitRequest(RestRequest request)
        {
            logger.MethodEntry(LogLevel.Debug);
            logger.LogTrace($"Request Resource: {request.Resource}");
            logger.LogTrace($"Request Method: {request.Method.ToString()}");

            if (request.Method != Method.Get)
            {
                StringBuilder body = new StringBuilder("Request Body: ");
                foreach (Parameter parameter in request.Parameters)
                {
                    body.Append($"{parameter.Name}={parameter.Value}");
                }
                logger.LogTrace(body.ToString());
            }

            RestResponse response;

            try
            {
                response = Client.ExecuteAsync(request).Result;
            }
            catch (Exception ex)
            {
                string exceptionMessage = F5BigIQException.FlattenExceptionMessages(ex, $"Error processing {request.Resource}");
                logger.LogError(exceptionMessage);
                throw;
            }

            if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                string errorMessage = response.Content + " " + response.ErrorMessage;
                string exceptionMessage = $"Error processing {request.Resource}: {errorMessage}";

                logger.LogError(exceptionMessage);
                logger.MethodExit(LogLevel.Debug);
                throw new F5BigIQException(exceptionMessage);
            }

            JObject json = JObject.Parse(response.Content);

            logger.LogTrace($"API Result: {response.Content}");
            logger.MethodExit(LogLevel.Debug);

            return json;
        }

        private RestClient GetRestClient(string baseUrl, string id, string pswd, bool useTokenAuth)
        {
            logger.MethodEntry(LogLevel.Debug);

            RestClientOptions options = new RestClientOptions(baseUrl);
            if (!useTokenAuth)
                options.Authenticator = new HttpBasicAuthenticator(id, pswd);

            RestClient client = new RestClient(options);
            if (useTokenAuth)
                client.AddDefaultHeader("X-F5-Auth-Token", GetAccessToken(id, pswd));

            logger.MethodExit(LogLevel.Debug);
            return client;
        }
    }
}
