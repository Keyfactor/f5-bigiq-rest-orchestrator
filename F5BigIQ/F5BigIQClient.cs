// Copyright 2024 Keyfactor
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

using Renci.SshNet;

using Keyfactor.Logging;
using Keyfactor.Extensions.Orchestrator.F5BigIQ.Models;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Keyfactor.Extensions.Orchestrator.F5BigIQ
{
    internal class F5BigIQClient
    {
        ILogger logger;
        private string BaseUrl { get; set; }
        private string UserId { get; set; }
        private string Password { get; set; }
        private RestClient Client { get; set; }

        F5BigIQClient(string baseUrl, string id, string pswd, bool useTokenAuth)
        {
            logger = LogHandler.GetClassLogger<F5BigIQClient>();
            BaseUrl = baseUrl;
            UserId = id;
            Password = pswd;
            Client = GetRestClient(baseUrl, id, pswd, useTokenAuth); 
        }

        internal List<F5CertificateItem> GetCertificates()
        {
            logger.MethodEntry(LogLevel.Debug);

            int CERTIFICATES_PER_PAGE = 50;
            int currentPageIndex = 1;

            List<F5CertificateItem> certificates = new List<F5CertificateItem>();

            do
            {
                string RESOURCE = $"/mgmt/cm/adc-core/working-config/sys/file/ssl-cert?top={CERTIFICATES_PER_PAGE.ToString()}&$skip={((currentPageIndex-1)*CERTIFICATES_PER_PAGE).ToString()}";
                RestRequest request = new RestRequest(RESOURCE, Method.Get);

                JObject json = SubmitRequest(request);
                F5Certificate pageOfCerts = JsonConvert.DeserializeObject<F5Certificate>(json.ToString());
                if (pageOfCerts.CertificateItems.Count == 0)
                    break;

                certificates.AddRange(pageOfCerts.CertificateItems);

                if (pageOfCerts.TotalPages == pageOfCerts.PageIndex)
                    break;

                currentPageIndex = pageOfCerts.PageIndex + 1;
            } while (1 == 1);

            logger.MethodExit(LogLevel.Debug);

            return certificates;
        }

        internal X509Certificate2 GetCertificate(string command)
        {
            logger.MethodEntry(LogLevel.Debug);

            string fileLocation = string.Empty;
            RestRequest request = new RestRequest(command.Replace("localhost", BaseUrl), Method.Get);

            JObject json = SubmitRequest(request);
            string certificateLocation = JsonConvert.DeserializeObject<F5CertificateLocation>(json.ToString()).CertificateLocation;

            return new X509Certificate2(DownloadCertificateFile(certificateLocation));
        }

        private byte[] DownloadCertificateFile(string location)
        {
            logger.MethodEntry(LogLevel.Debug);
            logger.LogDebug($"DownloadCertificateFile: {location}");

            byte[] rtnStore = new byte[] { };

            using (ScpClient client = new ScpClient(BaseUrl, UserId, Password))
            {
                try
                {
                    logger.LogDebug($"SCP connection attempt from {BaseUrl}");
                    client.Connect();

                    using (MemoryStream stream = new MemoryStream())
                    {
                        client.Download(location, stream);
                        rtnStore = stream.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    string msg = F5BigIQException.FlattenExceptionMessages(ex, "SCP Download Error: ");
                    logger.LogError(msg);
                    throw new F5BigIQException($"Error attempting SCP file transfer from {BaseUrl} .  Please contact your company's system administrator to verify connection and permission settings.", ex);
                }
                finally
                {
                    client.Disconnect();
                }
            }

            logger.MethodExit(LogLevel.Debug);

            return rtnStore;
        }

        private string GetAccessToken(string id, string pswd)
        {
            logger.MethodEntry(LogLevel.Debug);

            F5LoginRequest loginRequest = new F5LoginRequest() { UserId = id, Password = pswd, ProviderName = "tmos" };
            RestRequest request = new RestRequest($"/mgmt/shared/authn/login", Method.Post);
            request.AddBody(loginRequest);
            JObject json = SubmitRequest(request);

            logger.MethodExit(LogLevel.Debug);
            return JsonConvert.DeserializeObject<F5LoginResponse>(json.ToString()).Token.Token;
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
