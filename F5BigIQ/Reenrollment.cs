// Copyright 2024 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions.Interfaces;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.X509;
using System.Text;

namespace Keyfactor.Extensions.Orchestrator.F5BigIQ
{
    public class Reenrollment : F5JobBase, IReenrollmentJobExtension
    {
        //Job Entry Point
        public Reenrollment(IPAMSecretResolver resolver)
        {
            _resolver = resolver;
        }
        public JobResult ProcessJob(ReenrollmentJobConfiguration config, SubmitReenrollmentCSR submitReenrollment)
        {
            ILogger logger = LogHandler.GetClassLogger(this.GetType());
            logger.LogDebug($"Begin {config.Capability} for job id {config.JobId}...");
            logger.LogDebug($"Server: {config.CertificateStoreDetails.ClientMachine}");
            logger.LogDebug($"Store Path: {config.CertificateStoreDetails.StorePath}");
            logger.LogDebug($"Job Properties:");
            foreach (KeyValuePair<string, object> keyValue in config.JobProperties ?? new Dictionary<string, object>())
            {
                logger.LogDebug($"    {keyValue.Key}: {keyValue.Value}");
            }
           
            dynamic properties = JsonConvert.DeserializeObject(config.CertificateStoreDetails.Properties);

            SetPAMSecrets(config.ServerUsername, config.ServerPassword, logger);

            bool deployCertificateOnRenewal = properties.DeployCertificateOnRenewal == null || string.IsNullOrEmpty(properties.DeployCertificateOnRenewal.Value) ? false : bool.Parse(properties.DeployCertificateOnRenewal.Value);
            bool ignoreSSLWarning = properties.IgnoreSSLWarning == null || string.IsNullOrEmpty(properties.IgnoreSSLWarning.Value) ? false : bool.Parse(properties.IgnoreSSLWarning.Value);
            bool useTokenAuthentication = properties.UseTokenAuth == null || string.IsNullOrEmpty(properties.UseTokenAuth.Value) ? false : bool.Parse(properties.UseTokenAuth.Value);
            string loginProviderName = properties.LoginProviderName == null || string.IsNullOrEmpty(properties.LoginProviderName.Value) ? "tmos" : properties.LoginProviderName.Value;
            
            string keyType = !config.JobProperties.ContainsKey("keyType") || config.JobProperties["keyType"] == null || string.IsNullOrEmpty(config.JobProperties["keyType"].ToString()) ? string.Empty : config.JobProperties["keyType"].ToString();
            int? keySize = !config.JobProperties.ContainsKey("keySize") || config.JobProperties["keySize"] == null || string.IsNullOrEmpty(config.JobProperties["keySize"].ToString()) ? null : Convert.ToInt32(config.JobProperties["keySize"]);
            string subjectText = !config.JobProperties.ContainsKey("subjectText") || config.JobProperties["subjectText"] == null || config.JobProperties["subjectText"] == null || string.IsNullOrEmpty(config.JobProperties["subjectText"].ToString()) ? string.Empty : config.JobProperties["subjectText"].ToString();
            string sans = !config.JobProperties.ContainsKey("SANs") || config.JobProperties["SANs"] == null || string.IsNullOrEmpty(config.JobProperties["SANs"].ToString()) ? string.Empty : config.JobProperties["SANs"].ToString();
            if (!config.JobProperties.ContainsKey("Alias") || config.JobProperties["Alias"] == null || config.JobProperties["Alias"] == null || string.IsNullOrEmpty(config.JobProperties["Alias"].ToString()))
            {
                string errorMessage = "Error performing reenrollment.  Alias blank or does not exist.";
                logger.LogError(errorMessage);
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}: {errorMessage}"};
            }
            string alias = config.JobProperties["Alias"].ToString();
            bool overwrite = !config.JobProperties.ContainsKey("Overwrite") || config.JobProperties["Overwrite"] == null || string.IsNullOrEmpty(config.JobProperties["Overwrite"].ToString()) ? false : Convert.ToBoolean(config.JobProperties["Overwrite"]);

            try
            {
                F5BigIQClient f5Client = new F5BigIQClient(config.CertificateStoreDetails.ClientMachine, config.CertificateStoreDetails.StorePath, ServerUserName, ServerPassword, loginProviderName, useTokenAuthentication, ignoreSSLWarning);

                int totalKeys = f5Client.GetKeyByName(alias).TotalItems;
                if (!overwrite && totalKeys > 0)
                {
                    throw new Exception($"Alias {alias} already exists, but Overwrite is set to False.  Overwrite must be set to True if you wish to perform reenrollment on an existing certificate.");
                }

                string csr = f5Client.GenerateCSR(alias, totalKeys > 0, subjectText, keyType, keySize, sans);

                X509Certificate2 cert = submitReenrollment.Invoke(csr);
                if (cert == null)
                {
                    string errorMessage = "Error retrieving certificate for CSR: certificate not returned.";
                    logger.LogError(errorMessage);
                    return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}: {errorMessage}" };
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("-----BEGIN CERTIFICATE-----");
                sb.AppendLine(Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
                sb.AppendLine("-----END CERTIFICATE-----");

                try
                {
                    f5Client.AddReplaceBindCertificate(alias, sb.ToString(), string.Empty, overwrite, deployCertificateOnRenewal, F5BigIQClient.CERT_FILE_TYPE_TO_ADD.CERT);
                }
                catch (F5BigIQException ex)
                {
                    logger.LogError($"Exception for {config.Capability}: {F5BigIQException.FlattenExceptionMessages(ex, string.Empty)} for job id {config.JobId}");
                    return new JobResult() { Result = OrchestratorJobStatusJobResult.Warning, JobHistoryId = config.JobHistoryId, FailureMessage = F5BigIQException.FlattenExceptionMessages(ex, $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}.  Please see the Keyfactor Orchestrator log for more information.") };
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception for {config.Capability}: {F5BigIQException.FlattenExceptionMessages(ex, string.Empty)} for job id {config.JobId}");
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = F5BigIQException.FlattenExceptionMessages(ex, $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}:") };
            }

            return new JobResult() { Result = Keyfactor.Orchestrators.Common.Enums.OrchestratorJobStatusJobResult.Success, JobHistoryId = config.JobHistoryId };
        }
    }
}