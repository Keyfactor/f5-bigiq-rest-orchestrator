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
using Keyfactor.Extensions.Orchestrator.F5BigIQ.Models;

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Newtonsoft.Json;
using Keyfactor.Orchestrators.Extensions.Interfaces;

namespace Keyfactor.Extensions.Orchestrator.F5BigIQ
{
    public class Management : F5JobBase, IManagementJobExtension
    {
        //Job Entry Point
        public Management(IPAMSecretResolver resolver)
        {
            _resolver = resolver;
        }
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
            SetPAMSecrets(config.ServerUsername, config.ServerPassword, logger);
            bool ignoreSSLWarning = properties.IgnoreSSLWarning == null || string.IsNullOrEmpty(properties.IgnoreSSLWarning.Value) ? false : bool.Parse(properties.IgnoreSSLWarning.Value);
            bool useTokenAuthentication = properties.UseTokenAuth == null || string.IsNullOrEmpty(properties.UseTokenAuth.Value) ? false : bool.Parse(properties.UseTokenAuth.Value);

            try
            {
                F5BigIQClient f5Client = new F5BigIQClient(config.CertificateStoreDetails.ClientMachine, ServerUserName, ServerPassword, useTokenAuthentication, ignoreSSLWarning);
                
                switch (config.OperationType)
                {
                    case CertStoreOperationType.Add:
                        f5Client.AddReplaceCertificate(config.CertificateStoreDetails.StorePath, config.JobCertificate.Alias,
                            config.JobCertificate.Contents, config.JobCertificate.PrivateKeyPassword, config.Overwrite);

                        try
                        {
                            if (config.Overwrite)
                            {
                                List<string> profileNames = f5Client.GetProfilesNamesByAlias(config.JobCertificate.Alias);
                                if (profileNames.Count > 0)
                                {
                                    List<F5Deployment> f5Deployments = f5Client.GetVirtualServerDeploymentsForVirtualServers(profileNames);
                                    foreach (F5Deployment f5Deployment in f5Deployments)
                                        f5Client.ScheduleBigIPDeployment(f5Deployment);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            string msg = $"Certificate {config.JobCertificate.Alias} added successfully, but error occurred during attempt to check for linked Big IP deployments or deploying the certificate.";
                            logger.LogError($"Exception for {config.Capability}: {F5BigIQException.FlattenExceptionMessages(ex, msg)} for job id {config.JobId}");
                            return new JobResult() { Result = OrchestratorJobStatusJobResult.Warning, JobHistoryId = config.JobHistoryId, FailureMessage = F5BigIQException.FlattenExceptionMessages(ex, $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}: {msg}  Please see the Keyfactor Orchestrator log for more information.") };
                        }
                        break;
                    case CertStoreOperationType.Remove:
                        f5Client.DeleteCertificate(config.JobCertificate.Alias);
                        break;
                    default:
                        return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}: Unsupported operation: {config.OperationType.ToString()}" };
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