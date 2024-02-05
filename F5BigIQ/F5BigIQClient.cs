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
using System.Runtime.ConstrainedExecution;

namespace Keyfactor.Extensions.Orchestrator.F5BigIQ
{
    internal class F5BigIQClient
    {
        private const string GET_ENDPOINT = "/mgmt/cm/adc-core/working-config/sys/file/ssl-cert";
        private const string POST_ENDPOINT = "/mgmt/cm/adc-core/tasks/certificate-management";
        private const string UPLOAD_FOLDER = "/var/config/rest/downloads";

        private const string ADD_COMMAND = "ADD_PKCS12";
        private const string REPLACE_COMMAND = "REPLACE_PKCS12";

        ILogger logger;
        private string BaseUrl { get; set; }
        private string UserId { get; set; }
        private string Password { get; set; }
        private RestClient Client { get; set; }

        internal F5BigIQClient(string baseUrl, string id, string pswd, bool useTokenAuth, bool ignoreSSLWarning)
        {
            logger = LogHandler.GetClassLogger<F5BigIQClient>();
            BaseUrl = baseUrl;
            UserId = id;
            Password = pswd;
            Client = GetRestClient(baseUrl, id, pswd, useTokenAuth, ignoreSSLWarning); 
        }

        internal List<F5CertificateItem> GetCertificates()
        {
            logger.MethodEntry(LogLevel.Debug);

            int CERTIFICATES_PER_PAGE = 50;
            int currentPageIndex = 1;

            List<F5CertificateItem> certificates = new List<F5CertificateItem>();

            do
            {
                string RESOURCE = $"{GET_ENDPOINT}?top={CERTIFICATES_PER_PAGE.ToString()}&$skip={((currentPageIndex-1)*CERTIFICATES_PER_PAGE).ToString()}";
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

        internal X509Certificate2 GetCertificateByLink(string command)
        {
            logger.MethodEntry(LogLevel.Debug);

            string fileLocation = string.Empty;
            RestRequest request = new RestRequest(command.Replace("localhost", BaseUrl), Method.Get);

            JObject json = SubmitRequest(request);
            string certificateLocation = JsonConvert.DeserializeObject<F5CertificateLocation>(json.ToString()).CertificateLocation;
            string certs = DownloadCertificateFile(certificateLocation);
            
            CertificateCollectionConverter c = CertificateCollectionConverterFactory.FromPEM(certificateEntryAfterRemovalOfDelim);

            return c.ToX509Certificate2Collection();

            logger.MethodExit(LogLevel.Debug);

            return new X509Certificate2(DownloadCertificateFile(certificateLocation));
        }

        internal void AddReplaceCertificate(string storePath, string alias, string b64Certificate, string password, bool overwriteExisting)
        {
            logger.MethodEntry(LogLevel.Debug);

            F5Certificate f5Certificate = GetCertificateByName(alias);

            if (f5Certificate.TotalCertificates == 2)
                throw new F5BigIQException($"Two or more certificates already exist with the alias name of {alias}.");
            if (f5Certificate.TotalCertificates == 1 && !overwriteExisting)
                throw new F5BigIQException($"Certificate with alias name {alias} already exists but Overwrite is set to FALSE.  Please re-schedule this job and select the Overwrite checkbox (set to TRUE) if you with to replace this certificate.");

            string uploadFileName = Guid.NewGuid().ToString() + ".p12";
            byte[] certBytes = Convert.FromBase64String(b64Certificate);

            UploadCertificateFile(certBytes, uploadFileName);

            F5CertificateAddRequest addRequest = new F5CertificateAddRequest()
            {
                Alias = alias,
                FileLocation = uploadFileName,
                Partition = storePath,
                Password = password,
                Command = f5Certificate.TotalCertificates == 1 ? REPLACE_COMMAND : ADD_COMMAND,
                Reference = (f5Certificate.TotalCertificates == 1 ? new CertificateReference() { Link = f5Certificate.CertificateItems[0].Link.Replace("localhost", BaseUrl) } : null)
            };

            RestRequest request = new RestRequest(POST_ENDPOINT, Method.Post);
            request.AddParameter("application/json", JsonConvert.SerializeObject(addRequest), ParameterType.RequestBody);

            SubmitRequest(request);
        }

        private F5Certificate GetCertificateByName(string name)
        {
            logger.MethodEntry(LogLevel.Debug);

            string fileLocation = string.Empty;
            RestRequest request = new RestRequest($"{GET_ENDPOINT}?$filter=name+eq+'{name}'", Method.Get);
            JObject json = SubmitRequest(request);

            logger.MethodExit(LogLevel.Debug);

            return JsonConvert.DeserializeObject<F5Certificate>(json.ToString());
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

        private void UploadCertificateFile(byte[] certBytes, string fileName)
        {
            logger.MethodEntry(LogLevel.Debug);

            using (ScpClient client = new ScpClient(BaseUrl, UserId, Password))
            {
                try
                {
                    logger.LogDebug($"SCP connection attempt from {BaseUrl}");
                    client.Connect();

                    using (MemoryStream stream = new MemoryStream(certBytes))
                    {
                        client.Upload(stream, $"{UPLOAD_FOLDER}/{fileName}");
                    }
                }
                catch (Exception ex)
                {
                    string msg = F5BigIQException.FlattenExceptionMessages(ex, "SCP Upload Error: ");
                    logger.LogError(msg);
                    throw new F5BigIQException($"Error attempting SCP file transfer from {BaseUrl} .  Please contact your company's system administrator to verify connection and permission settings.", ex);
                }
                finally
                {
                    client.Disconnect();
                }
            }

            logger.MethodExit(LogLevel.Debug);
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

        private RestClient GetRestClient(string baseUrl, string id, string pswd, bool useTokenAuth, bool ignoreSSLWarning)
        {
            logger.MethodEntry(LogLevel.Debug);

            RestClientOptions options = new RestClientOptions(baseUrl);

            if (ignoreSSLWarning)
                options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

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
