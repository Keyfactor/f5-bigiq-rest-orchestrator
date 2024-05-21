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
using Org.BouncyCastle.Asn1.Ocsp;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Org.BouncyCastle.Tls;
using Renci.SshNet.Security;

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
        private const string GENERATE_CSR_COMMAND = "GENERATE_CSR";
        private const string REPLACE_CSR_COMMAND = "GEN_REPLACE_CSR";
        private const string ALIAS_SUFFIX = ".crt";
        private const string ALIAS_KEY_SUFFIX = ".key";

        private enum RESULT_STATUS
        {
            STARTED,
            FINISHED,
            FAILED
        }

        internal enum CERT_FILE_TYPE_TO_ADD
        {
            CERT,
            PKCS12
        }

        ILogger logger;
        private string BaseUrl { get; set; }
        private string Partition { get; set; }
        private string UserId { get; set; }
        private string Password { get; set; }
        private RestClient Client { get; set; }

        internal F5BigIQClient(string baseUrl, string partition, string id, string pswd, string loginProviderName, bool useTokenAuth, bool ignoreSSLWarning)
        {
            logger = LogHandler.GetClassLogger<F5BigIQClient>();
            BaseUrl = baseUrl;
            Partition = partition;
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
                F5CertificateObject pageOfCerts = JsonConvert.DeserializeObject<F5CertificateObject>(json.ToString());
                if (pageOfCerts.Items.Count == 0)
                    break;

                certificates.AddRange(pageOfCerts.Items.Where(p => p.Partition.ToLower() == Partition.ToLower()));

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
        internal void AddReplaceBindCertificate(string alias, string cert, string privateKeyPassword, bool overwrite, bool deployCertificateOnRenewal, CERT_FILE_TYPE_TO_ADD fileType)
        {
            AddReplaceCertificate(alias, cert, privateKeyPassword, overwrite, fileType);

            try
            {
                if (overwrite && deployCertificateOnRenewal)
                {
                    List<string> profileNames = GetProfilesNamesByAlias(alias);
                    if (profileNames.Count > 0)
                    {
                        List<F5Deployment> f5Deployments = GetVirtualServerDeploymentsForVirtualServers(profileNames);
                        foreach (F5Deployment f5Deployment in f5Deployments)
                            ScheduleBigIPDeployment(f5Deployment);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new F5BigIQException($"Certificate {alias} added successfully, but error occurred during attempt to check for linked Big IP deployments or deploying the certificate.", ex);
            }
        }
        private void AddReplaceCertificate(string alias, string b64Certificate, string password, bool overwrite, CERT_FILE_TYPE_TO_ADD fileType)
        {
            logger.MethodEntry(LogLevel.Debug);
            F5CertificateObject f5Certificate = GetCertificateByName(alias);

            if (f5Certificate.TotalItems > 1)
                throw new F5BigIQException($"Two or more certificates already exist with the alias name of {alias}.");
            if (f5Certificate.TotalItems == 1 && !overwrite)
                throw new F5BigIQException($"Certificate with alias name {alias} already exists but Overwrite is set to FALSE.  Please re-schedule this job and select the Overwrite checkbox (set to TRUE) if you wish to replace this certificate.");

            F5CertificateObject f5Key = f5Certificate.TotalItems == 1 ? GetKeyByName(alias) : null;

            string uploadFileName = Guid.NewGuid().ToString() + (fileType == CERT_FILE_TYPE_TO_ADD.PKCS12 ? ".p12" : ".crt");
            byte[] certBytes = Convert.FromBase64String(b64Certificate);

            UploadCertificateFile(certBytes, uploadFileName);

            F5CertificateAddRequest addRequest = new F5CertificateAddRequest()
            {
                Alias = alias + ALIAS_SUFFIX,
                FileLocation = $@"{UPLOAD_FOLDER}/{uploadFileName}",
                Partition = this.Partition,
                Password = password,
                Command = f5Certificate.TotalItems == 1 ? $"REPL_{fileType.ToString()}" : $"ADD_{fileType.ToString()}",
                CertReference = (f5Certificate.TotalItems == 1 ? new CertificateReference() { Link = f5Certificate.Items[0].Link.Replace(LOCAL_URL_VALUE, BaseUrl) } : null),
                KeyReference = (f5Certificate.TotalItems == 1 ? new CertificateReference() { Link = f5Key.Items[0].Link.Replace(LOCAL_URL_VALUE, BaseUrl) } : null)
            };

            RestRequest request = new RestRequest(POST_ENDPOINT, Method.Post);
            request.AddParameter("application/json", JsonConvert.SerializeObject(addRequest), ParameterType.RequestBody);

            SubmitRequest(request);
        }

        internal void DeleteCertificate(string alias)
        {
            logger.MethodEntry(LogLevel.Debug);

            F5CertificateObject f5Certificate = GetCertificateByName(alias);
            if (f5Certificate.TotalItems > 1)
                throw new F5BigIQException($"Two or more certificates already exist with the alias name of {alias}.");
            if (f5Certificate.TotalItems == 0)
                throw new F5BigIQException($"Alias {alias} not found.  Delete unsuccessful.");

            F5CertificateObject f5Key = GetKeyByName(alias);
            if (f5Key.TotalItems > 1)
                throw new F5BigIQException($"Two or more certificate keys already exist with the alias name of {alias}.");
            if (f5Key.TotalItems == 0)
                throw new F5BigIQException($"Key for alias {alias} not found.  Delete unsuccessful.");

            RestRequest request = new RestRequest(GET_KEY_ENDPOINT + $@"/{f5Key.Items[0].Id}", Method.Delete);
            SubmitRequest(request);

            request = new RestRequest(GET_ENDPOINT + $@"/{f5Certificate.Items[0].Id}", Method.Delete);
            SubmitRequest(request);

            logger.MethodExit(LogLevel.Debug);
        }

        internal string GenerateCSR(string alias, bool replaceCSR, string subjectText, string keyType, int? keySize, string sans)
        {
            logger.MethodEntry(LogLevel.Debug);

            string[] subjectParams = subjectText.Split(',');
            Dictionary<string, string> subjectValues = new Dictionary<string, string>();
            foreach (string subjectParam in subjectParams)
            {
                string[] subjectPair = subjectParam.Split('=', 2, StringSplitOptions.TrimEntries);
                subjectValues.Add(subjectPair[0].ToUpper(), subjectPair[1]);
            }

            F5CertificateCSRRequest csrRequest = new F5CertificateCSRRequest()
            {
                Command = replaceCSR ? REPLACE_CSR_COMMAND : GENERATE_CSR_COMMAND,
                ItemName = alias + ".csr",
                ItemPartition = Partition,
                CommonName = subjectValues.ContainsKey("CN") ? subjectValues["CN"] : null,
                Organization = subjectValues.ContainsKey("O") ? subjectValues["O"] : null,
                OrganizationalUnit = subjectValues.ContainsKey("OU") ? subjectValues["OU"] : null,
                Country = subjectValues.ContainsKey("C") ? subjectValues["C"] : null,
                State = subjectValues.ContainsKey("ST") ? subjectValues["ST"] : null,
                Locality = subjectValues.ContainsKey("L") ? subjectValues["L"] : null,
                KeyType = keyType,
                KeySize = keySize.HasValue ? keySize.Value : null,
                SubjectAlternativeNames = sans
            };

            if (replaceCSR)
            {
                F5CertificateObject f5Key = GetKeyByName(alias);
                if (f5Key.TotalItems > 1)
                    throw new F5BigIQException($"Multiple keys currently exist for alias {alias}.  Renewal using ODKG (Reenrollment) not possible.");

                F5CertificateObject f5Key = GetCSRByName(alias);
                if (f5Key.TotalItems > 1)
                    throw new F5BigIQException($"Multiple CSRs currently exist for alias {alias}.  Renewal using ODKG (Reenrollment) not possible.");
                if (f5Key.TotalItems == 0)
                    throw new F5BigIQException($"No existing CSR found for alias {alias}, but key exists.  Renewal using ODKG (Reenrollment) not possible.");

                csrRequest.KeyReference = (f5Key.TotalItems == 1 ? new F5CSRReference() { Link = f5Key.Items[0].Link.Replace(LOCAL_URL_VALUE, BaseUrl) } : null);

            }

            RestRequest request = new RestRequest(POST_ENDPOINT, Method.Post);
            request.AddParameter("application/json", JsonConvert.SerializeObject(csrRequest), ParameterType.RequestBody);

            JObject json = SubmitRequest(request);
            string csrLink = JsonConvert.DeserializeObject<F5CSRReference>(json.ToString()).Link.Replace(LOCAL_URL_VALUE, BaseUrl);

            JObject json2 = SubmitRequest(new RestRequest(csrLink, Method.Get));
            F5CSRResult csrResult;
            int tryNumber = 1;
            do
            {
                csrResult = JsonConvert.DeserializeObject<F5CSRResult>(json2.ToString());
                if (csrResult.Status.ToUpper() == RESULT_STATUS.FAILED.ToString() || csrResult.Status == RESULT_STATUS.FINISHED.ToString())
                    break;

                if (tryNumber > 10)
                {
                    throw new F5BigIQException("CSR Generation request did not complete in a timely manner.");
                }
                json2 = SubmitRequest(new RestRequest(csrLink, Method.Get));
                tryNumber++;
            } while (tryNumber < 11);

            if (csrResult.Status.ToUpper() == RESULT_STATUS.FAILED.ToString())
                throw new F5BigIQException($"CSR Generation failed: {csrResult.ErrorMessage}.");

            logger.MethodExit(LogLevel.Debug);

            return csrResult.CSR;
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

        internal F5CertificateObject GetCertificateByName(string name)
        {
            logger.MethodEntry(LogLevel.Debug);

            string fileLocation = string.Empty;
            string aliasWithSuffix = name + ALIAS_SUFFIX;

            RestRequest request = new RestRequest($"{GET_ENDPOINT}?$filter=name+eq+'{aliasWithSuffix}'", Method.Get);
            JObject json = SubmitRequest(request);

            logger.MethodExit(LogLevel.Debug);

            return JsonConvert.DeserializeObject<F5CertificateObject>(json.ToString());
        }

        internal F5CertificateObject GetKeyByName(string name)
        {
            logger.MethodEntry(LogLevel.Debug);

            string fileLocation = string.Empty;
            string aliasWithSuffix = name + ALIAS_KEY_SUFFIX;

            RestRequest request = new RestRequest($"{GET_KEY_ENDPOINT}?$filter=name+eq+'{aliasWithSuffix}'", Method.Get);
            JObject json = SubmitRequest(request);

            logger.MethodExit(LogLevel.Debug);

            return JsonConvert.DeserializeObject<F5CertificateObject>(json.ToString());
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
