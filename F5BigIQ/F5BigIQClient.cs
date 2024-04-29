// Copyright 2024 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using RestSharp;
using RestSharp.Authenticators;

using Renci.SshNet;

using Keyfactor.Logging;
using Keyfactor.PKI.X509;
using Keyfactor.Extensions.Orchestrator.F5BigIQ.Models;

namespace Keyfactor.Extensions.Orchestrator.F5BigIQ
{
    internal class F5BigIQClient
    {
        private const string LOCAL_URL_VALUE = @"https://localhost";
        private const string GET_ENDPOINT = "/mgmt/cm/adc-core/working-config/sys/file/ssl-cert";
        private const string GET_KEY_ENDPOINT = "/mgmt/cm/adc-core/working-config/sys/file/ssl-key";
        private const string GET_PROFILE_ENDPOINT = "/mgmt/cm/adc-core/working-config/ltm/profile/client-ssl";
        private const string GET_VIRTUAL_SERVER_ENDPOINT = "/mgmt/cm/adc-core/working-config/ltm/virtual";
        private const string POST_ENDPOINT = "/mgmt/cm/adc-core/tasks/certificate-management";
        private const string POST_DEPLOY_ENDPOINT = "/mgmt/cm/adc-core/tasks/deploy-configuration";
        private const string UPLOAD_FOLDER = "/var/config/rest/downloads";
        private const int ITEMS_PER_PAGE = 50;
        private const string ADD_COMMAND = "ADD_PKCS12";
        private const string REPLACE_COMMAND = "REPLACE_PKCS12";
        private const string GENERATE_CSR_COMMAND = "GENERATE_CSR";
        private const string ALIAS_SUFFIX = ".crt";
        private const string ALIAS_KEY_SUFFIX = ".key";

        ILogger logger;
        private string BaseUrl { get; set; }
        private string UserId { get; set; }
        private string Password { get; set; }
        private RestClient Client { get; set; }

        internal F5BigIQClient(string baseUrl, string id, string pswd, string loginProviderName, bool useTokenAuth, bool ignoreSSLWarning)
        {
            logger = LogHandler.GetClassLogger<F5BigIQClient>();
            BaseUrl = baseUrl;
            UserId = id;
            Password = pswd;
            Client = GetRestClient(baseUrl, id, pswd, ignoreSSLWarning, false);

            if (useTokenAuth)
            {
                string token = GetAccessToken(id, pswd, loginProviderName);
                Client = GetRestClient(baseUrl, id, pswd, ignoreSSLWarning, true);
                Client.AddDefaultHeader("X-F5-Auth-Token", token);
            }
        }

        internal List<F5CertificateItem> GetCertificates()
        {
            logger.MethodEntry(LogLevel.Debug);

            int currentPageIndex = 1;

            List<F5CertificateItem> certificates = new List<F5CertificateItem>();

            do
            {
                string RESOURCE = $"{GET_ENDPOINT}?$top={ITEMS_PER_PAGE.ToString()}&$skip={((currentPageIndex - 1) * ITEMS_PER_PAGE).ToString()}";
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
            certificates.ForEach(p => p.Alias = p.Alias.Replace(ALIAS_SUFFIX, string.Empty));

            logger.MethodExit(LogLevel.Debug);

            return certificates;
        }

        internal X509Certificate2Collection GetCertificateByLink(string command)
        {
            logger.MethodEntry(LogLevel.Debug);

            string fileLocation = string.Empty;
            RestRequest request = new RestRequest(command.Replace(LOCAL_URL_VALUE, BaseUrl), Method.Get);

            JObject json = SubmitRequest(request);
            string certificateLocation = JsonConvert.DeserializeObject<F5CertificateLocation>(json.ToString()).CertificateLocation;
            string certChain = System.Text.ASCIIEncoding.ASCII.GetString(DownloadCertificateFile(certificateLocation));

            CertificateCollectionConverter c = CertificateCollectionConverterFactory.FromPEM(certChain);

            logger.MethodExit(LogLevel.Debug);

            return c.ToX509Certificate2Collection();
        }

        internal void AddReplaceCertificate(string storePath, string alias, string b64Certificate, string password, bool overwriteExisting)
        {
            logger.MethodEntry(LogLevel.Debug);

            string aliasWithSuffix = alias + ALIAS_SUFFIX;
            F5Certificate f5Certificate = GetCertificateByName(aliasWithSuffix);

            if (f5Certificate.TotalCertificates > 1)
                throw new F5BigIQException($"Two or more certificates already exist with the alias name of {alias}.");
            if (f5Certificate.TotalCertificates == 1 && !overwriteExisting)
                throw new F5BigIQException($"Certificate with alias name {alias} already exists but Overwrite is set to FALSE.  Please re-schedule this job and select the Overwrite checkbox (set to TRUE) if you wish to replace this certificate.");

            F5Certificate f5Key = f5Certificate.TotalCertificates == 1 ? GetKeyByName(alias + ALIAS_KEY_SUFFIX) : null;

            string uploadFileName = Guid.NewGuid().ToString() + ".p12";
            byte[] certBytes = Convert.FromBase64String(b64Certificate);

            UploadCertificateFile(certBytes, uploadFileName);

            F5CertificateAddRequest addRequest = new F5CertificateAddRequest()
            {
                Alias = aliasWithSuffix,
                FileLocation = $@"{UPLOAD_FOLDER}/{uploadFileName}",
                Partition = storePath,
                Password = password,
                Command = f5Certificate.TotalCertificates == 1 ? REPLACE_COMMAND : ADD_COMMAND,
                CertReference = (f5Certificate.TotalCertificates == 1 ? new CertificateReference() { Link = f5Certificate.CertificateItems[0].Link.Replace(LOCAL_URL_VALUE, BaseUrl) } : null),
                KeyReference = (f5Certificate.TotalCertificates == 1 ? new CertificateReference() { Link = f5Key.CertificateItems[0].Link.Replace(LOCAL_URL_VALUE, BaseUrl) } : null)
            };

            RestRequest request = new RestRequest(POST_ENDPOINT, Method.Post);
            request.AddParameter("application/json", JsonConvert.SerializeObject(addRequest), ParameterType.RequestBody);

            SubmitRequest(request);
        }

        internal void DeleteCertificate(string alias)
        {
            logger.MethodEntry(LogLevel.Debug);

            F5Certificate f5Certificate = GetCertificateByName(alias + ALIAS_SUFFIX);
            if (f5Certificate.TotalCertificates > 1)
                throw new F5BigIQException($"Two or more certificates already exist with the alias name of {alias}.");
            if (f5Certificate.TotalCertificates == 0)
                throw new F5BigIQException($"Alias {alias} not found.  Delete unsuccessful.");

            F5Certificate f5Key = GetKeyByName(alias + ALIAS_KEY_SUFFIX);
            if (f5Key.TotalCertificates > 1)
                throw new F5BigIQException($"Two or more certificate keys already exist with the alias name of {alias}.");
            if (f5Key.TotalCertificates == 0)
                throw new F5BigIQException($"Key for alias {alias} not found.  Delete unsuccessful.");

            RestRequest request = new RestRequest(GET_KEY_ENDPOINT + $@"/{f5Key.CertificateItems[0].Id}", Method.Delete);
            SubmitRequest(request);

            request = new RestRequest(GET_ENDPOINT + $@"/{f5Certificate.CertificateItems[0].Id}", Method.Delete);
            SubmitRequest(request);

            logger.MethodExit(LogLevel.Debug);
        }

        internal void GenerateCSR(string storePath, string alias, string subjectText, string keyType, int keySize, string[] sans)
        {
            logger.MethodEntry(LogLevel.Debug);

            string aliasWithSuffix = alias + ALIAS_SUFFIX;
            F5Certificate f5Certificate = GetCertificateByName(aliasWithSuffix);

            if (f5Certificate.TotalCertificates > 1)
                throw new F5BigIQException($"Two or more certificates already exist with the alias name of {alias}.");
            if (f5Certificate.TotalCertificates == 1 && !overwriteExisting)
                throw new F5BigIQException($"Certificate with alias name {alias} already exists but Overwrite is set to FALSE.  Please re-schedule this job and select the Overwrite checkbox (set to TRUE) if you wish to replace this certificate.");

            F5Certificate f5Key = f5Certificate.TotalCertificates == 1 ? GetKeyByName(alias + ALIAS_KEY_SUFFIX) : null;

            string uploadFileName = Guid.NewGuid().ToString() + ".p12";
            byte[] certBytes = Convert.FromBase64String(b64Certificate);

            UploadCertificateFile(certBytes, uploadFileName);

            F5CertificateAddRequest addRequest = new F5CertificateAddRequest()
            {
                Alias = aliasWithSuffix,
                FileLocation = $@"{UPLOAD_FOLDER}/{uploadFileName}",
                Partition = storePath,
                Password = password,
                Command = f5Certificate.TotalCertificates == 1 ? REPLACE_COMMAND : ADD_COMMAND,
                CertReference = (f5Certificate.TotalCertificates == 1 ? new CertificateReference() { Link = f5Certificate.CertificateItems[0].Link.Replace(LOCAL_URL_VALUE, BaseUrl) } : null),
                KeyReference = (f5Certificate.TotalCertificates == 1 ? new CertificateReference() { Link = f5Key.CertificateItems[0].Link.Replace(LOCAL_URL_VALUE, BaseUrl) } : null)
            };

            RestRequest request = new RestRequest(POST_ENDPOINT, Method.Post);
            request.AddParameter("application/json", JsonConvert.SerializeObject(addRequest), ParameterType.RequestBody);

            SubmitRequest(request);
        }

        internal List<string> GetProfilesNamesByAlias(string alias)
        {
            logger.MethodEntry(LogLevel.Debug);
            List<string> profileNames = new List<string>();
            string aliasWithSuffix = alias + ALIAS_SUFFIX;

            int currentPageIndex = 1;

            do
            {
                string RESOURCE = $"{GET_PROFILE_ENDPOINT}?$top={ITEMS_PER_PAGE.ToString()}&$skip={((currentPageIndex - 1) * ITEMS_PER_PAGE).ToString()}";
                RestRequest request = new RestRequest(RESOURCE, Method.Get);

                JObject json = SubmitRequest(request);
                F5Profile pageOfProfiles = JsonConvert.DeserializeObject<F5Profile>(json.ToString());

                profileNames.AddRange(pageOfProfiles.ProfileItems.Where(o => o.CertificateKeyChains != null && o.CertificateKeyChains.Any(p => p.CertificateReference.Name == aliasWithSuffix)).Select(q => q.Name).ToList());

                if (pageOfProfiles.TotalPages == pageOfProfiles.PageIndex)
                    break;

                currentPageIndex = pageOfProfiles.PageIndex + 1;
            } while (1 == 1);

            logger.MethodExit(LogLevel.Debug);

            return profileNames;
        }

        internal List<F5Deployment> GetVirtualServerDeploymentsForVirtualServers(List<string> virtualServerNames)
        {
            logger.MethodEntry(LogLevel.Debug);
            List<F5Deployment> deployments = new List<F5Deployment>();

            int currentPageIndex = 1;

            do
            {
                string RESOURCE = $"{GET_VIRTUAL_SERVER_ENDPOINT}?$top={ITEMS_PER_PAGE.ToString()}&$skip={((currentPageIndex - 1) * ITEMS_PER_PAGE).ToString()}";
                RestRequest request = new RestRequest(RESOURCE, Method.Get);

                JObject json = SubmitRequest(request);
                F5VirtualServer pageOfVirtualServers = JsonConvert.DeserializeObject<F5VirtualServer>(json.ToString());

                foreach (F5VirtualServerItem virtualServerItem in pageOfVirtualServers.VirtualServerItems)
                {
                    RestRequest request2 = new RestRequest(virtualServerItem.VirtualServerProfilesCollectionReference.ItemLink.Replace(LOCAL_URL_VALUE, BaseUrl), Method.Get);
                    JObject json2 = SubmitRequest(request2);
                    F5VirtualServerProfile virtualServerProfiles = JsonConvert.DeserializeObject<F5VirtualServerProfile>(json2.ToString());

                    if (virtualServerProfiles.VirtualServerProfileItems.Any(p => p.VirtualServerProfileClientSSLReference != null && virtualServerNames.Contains(p.VirtualServerProfileClientSSLReference.Name)))
                    {
                        deployments.Add(new F5Deployment()
                        {
                            Name = virtualServerItem.Name + "-" +  Guid.NewGuid().ToString(),
                            DeviceReferences = new List<F5Reference>() { new F5Reference() { ItemLink = virtualServerItem.VirtualServerDeviceReference.ItemLink } },
                            ObjectsToDeployReferences = new List<F5Reference>() { new F5Reference() { ItemLink = virtualServerItem.ItemLink } }
                        });
                    }
                }

                if (pageOfVirtualServers.TotalPages == pageOfVirtualServers.PageIndex)
                    break;

                currentPageIndex = pageOfVirtualServers.PageIndex + 1;
            } while (1 == 1);

            logger.MethodExit(LogLevel.Debug);

            return deployments;
        }

        internal void ScheduleBigIPDeployment(F5Deployment deploymentRequest)
        {
            RestRequest request = new RestRequest(POST_DEPLOY_ENDPOINT, Method.Post);
            request.AddParameter("application/json", JsonConvert.SerializeObject(deploymentRequest), ParameterType.RequestBody);

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

        private F5Certificate GetKeyByName(string name)
        {
            logger.MethodEntry(LogLevel.Debug);

            string fileLocation = string.Empty;
            RestRequest request = new RestRequest($"{GET_KEY_ENDPOINT}?$filter=name+eq+'{name}'", Method.Get);
            JObject json = SubmitRequest(request);

            logger.MethodExit(LogLevel.Debug);

            return JsonConvert.DeserializeObject<F5Certificate>(json.ToString());
        }

        private byte[] DownloadCertificateFile(string location)
        {
            logger.MethodEntry(LogLevel.Debug);
            logger.LogDebug($"DownloadCertificateFile: {location}");

            byte[] rtnStore = new byte[] { };
            string serverLocation = BaseUrl.Replace("https://", String.Empty);

            ConnectionInfo connectionInfo = new ConnectionInfo(serverLocation, UserId, new PasswordAuthenticationMethod(UserId, Password));
            using (ScpClient client = new ScpClient(connectionInfo))
            {
                try
                {
                    logger.LogDebug($"SCP connection attempt from {serverLocation}");
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

            string serverLocation = BaseUrl.Replace("https://", String.Empty);

            ConnectionInfo connectionInfo = new ConnectionInfo(serverLocation, UserId, new List<AuthenticationMethod>() { new PasswordAuthenticationMethod(UserId, Password) }.ToArray());
            using (ScpClient client = new ScpClient(connectionInfo))
            {
                try
                {
                    logger.LogDebug($"SCP connection attempt from {serverLocation}");
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

        private string GetAccessToken(string id, string pswd, string loginProviderName)
        {
            logger.MethodEntry(LogLevel.Debug);

            F5LoginRequest loginRequest = new F5LoginRequest() { UserId = id, Password = pswd, LoginProviderName = loginProviderName}; //, ProviderName = "tmos" };
            RestRequest request = new RestRequest($"/mgmt/shared/authn/login", Method.Post);
            request.AddParameter("application/json", JsonConvert.SerializeObject(loginRequest), ParameterType.RequestBody);

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
                if (response.ErrorException != null)
                    throw response.ErrorException;
                else
                    throw new F5BigIQException(exceptionMessage);
            }

            JObject json = JObject.Parse(response.Content);

            logger.LogTrace($"API Result: {response.Content}");
            logger.MethodExit(LogLevel.Debug);

            return json;
        }

        private RestClient GetRestClient(string baseUrl, string id, string pswd, bool ignoreSSLWarning, bool useTokenAuth)
        {
            logger.MethodEntry(LogLevel.Debug);

            RestClientOptions options = new RestClientOptions(baseUrl);

            if (!useTokenAuth)
                options.Authenticator = new HttpBasicAuthenticator(id, pswd);
            if (ignoreSSLWarning)
                options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            RestClient client = new RestClient(options);

            logger.MethodExit(LogLevel.Debug);
            return client;
        }
    }
}
