// Copyright 2024 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;

using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Common.Enums;

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Orchestrator.F5BigIQ
{
    public class Management : IManagementJobExtension
    {
        public string ExtensionName => "";

        //Job Entry Point
        public JobResult ProcessJob(ManagementJobConfiguration config)
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
            bool ignoreSSLWarning = properties.IgnoreSSLWarning == null || string.IsNullOrEmpty(properties.IgnoreSSLWarning.Value) ? false : bool.Parse(properties.IgnoreSSLWarning.Value);
            bool useTokenAuthentication = properties.UseTokenAuthentication == null || string.IsNullOrEmpty(properties.UseTokenAuthentication.Value) ? false : bool.Parse(properties.UseTokenAuthentication.Value);

            try
            {
                F5BigIQClient f5Client = new F5BigIQClient(config.CertificateStoreDetails.ClientMachine, config.ServerUsername, config.ServerPassword, useTokenAuthentication, ignoreSSLWarning);
                
                switch (config.OperationType)
                {
                    case CertStoreOperationType.Add:
                        f5Client.AddReplaceCertificate(config.CertificateStoreDetails.StorePath, config.JobCertificate.Alias,
                            config.JobCertificate.Contents, config.JobCertificate.PrivateKeyPassword, config.Overwrite);
                        break;
                    case CertStoreOperationType.Remove:
                        break;
                    default:
                        return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}: Unsupported operation: {config.OperationType.ToString()}" };
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception for {config.Capability}: {F5BigIQException.FlattenExceptionMessages(ex, string.Empty)} for job id {config.JobId}");
            }

            return new JobResult() { Result = Keyfactor.Orchestrators.Common.Enums.OrchestratorJobStatusJobResult.Success, JobHistoryId = config.JobHistoryId };
        }
    }
}