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
using Keyfactor.Extensions.Orchestrator.F5BigIQ.Models;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Keyfactor.Orchestrators.Extensions.Interfaces;

namespace Keyfactor.Extensions.Orchestrator.F5BigIQ
{
    public class Inventory : F5JobBase, IInventoryJobExtension
    {
        public Inventory(IPAMSecretResolver resolver)
        {
            _resolver = resolver;
        }

        public JobResult ProcessJob(InventoryJobConfiguration config, SubmitInventoryUpdate submitInventory)
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
            bool ignoreSSLWarning = properties.IgnoreSSLWarning == null || string.IsNullOrEmpty(properties.IgnoreSSLWarning.Value) ? false : bool.Parse(properties.IgnoreSSLWarning.Value);
            bool useTokenAuthentication = properties.UseTokenAuth == null || string.IsNullOrEmpty(properties.UseTokenAuth.Value) ? false : bool.Parse(properties.UseTokenAuth.Value);
            string loginProviderName = properties.LoginProviderName == null || string.IsNullOrEmpty(properties.LoginProviderName.Value) ? "tmos" : properties.LoginProviderName.Value;

            List<CurrentInventoryItem> inventoryItems = new List<CurrentInventoryItem>();

            try
            {
                F5BigIQClient f5Client = new F5BigIQClient(config.CertificateStoreDetails.ClientMachine, ServerUserName, ServerPassword, loginProviderName, useTokenAuthentication, ignoreSSLWarning);
                List<F5CertificateItem> certItems =  f5Client.GetCertificates();
                foreach (F5CertificateItem certItem in certItems)
                {
                    logger.LogDebug($"Retrieving Alias {certItem.Alias}, item {certItems.IndexOf(certItem).ToString()} of {certItems.Count.ToString()}");
                    if (certItem.FileReference == null)
                        continue;
                    X509Certificate2Collection certChain = f5Client.GetCertificateByLink(certItem.FileReference.Link);
                    List<string> certContents = new List<string>();
                    bool useChainLevel = certChain.Count > 1;
                    foreach (X509Certificate2 certificate in certChain)
                    {
                        certContents.Add(Convert.ToBase64String(certificate.Export(X509ContentType.Cert)));
                    }
                    inventoryItems.Add(new CurrentInventoryItem()
                    {
                        Alias = certItem.Alias,
                        Certificates = certContents.ToArray(),
                        ItemStatus = Orchestrators.Common.Enums.OrchestratorInventoryItemStatus.Unknown,
                        UseChainLevel = useChainLevel,
                        PrivateKeyEntry = true
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception for {config.Capability}: {F5BigIQException.FlattenExceptionMessages(ex, string.Empty)} for job id {config.JobId}");
                return new JobResult() { Result = Keyfactor.Orchestrators.Common.Enums.OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = F5BigIQException.FlattenExceptionMessages(ex, $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}:") };
            }

            try
            {
                submitInventory.Invoke(inventoryItems);
                return new JobResult() { Result = Keyfactor.Orchestrators.Common.Enums.OrchestratorJobStatusJobResult.Success, JobHistoryId = config.JobHistoryId };
            }
            catch (Exception ex)
            {
                string errorMessage = F5BigIQException.FlattenExceptionMessages(ex, string.Empty);
                logger.LogError($"Exception returning certificates for {config.Capability}: {errorMessage} for job id {config.JobId}");
                return new JobResult() { Result = Keyfactor.Orchestrators.Common.Enums.OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = F5BigIQException.FlattenExceptionMessages(ex, $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}:") };
            }
        }
    }
}