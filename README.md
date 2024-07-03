<h1 align="center" style="border-bottom: none">
    F5 BigIQ Universal Orchestrator Extension
</h1>

<p align="center">
  <!-- Badges -->
<img src="https://img.shields.io/badge/integration_status-production-3D1973?style=flat-square" alt="Integration Status: production" />
<a href="https://github.com/Keyfactor/f5-bigiq-rest-orchestrator/releases"><img src="https://img.shields.io/github/v/release/Keyfactor/f5-bigiq-rest-orchestrator?style=flat-square" alt="Release" /></a>
<img src="https://img.shields.io/github/issues/Keyfactor/f5-bigiq-rest-orchestrator?style=flat-square" alt="Issues" />
<img src="https://img.shields.io/github/downloads/Keyfactor/f5-bigiq-rest-orchestrator/total?style=flat-square&label=downloads&color=28B905" alt="GitHub Downloads (all assets, all releases)" />
</p>

<p align="center">
  <!-- TOC -->
  <a href="#support">
    <b>Support</b>
  </a>
  ·
  <a href="#installation">
    <b>Installation</b>
  </a>
  ·
  <a href="#license">
    <b>License</b>
  </a>
  ·
  <a href="https://github.com/orgs/Keyfactor/repositories?q=orchestrator">
    <b>Related Integrations</b>
  </a>
</p>


## Overview

The F5 BigIQ Universal Orchestrator extension facilitates the management of SSL certificates on F5 Big IQ devices from Keyfactor Command. This orchestration includes the ability to inventory, add, and remove certificates from F5 Big IQ devices. Additionally, it supports the reenrollment of certificates, where a new key pair and certificate signing request (CSR) are generated on the F5 Big IQ device, and the resulting certificate is automatically enrolled and installed.

F5 Big IQ manages SSL certificates to secure communications and provide SSL termination for applications. Through this integration, Keyfactor Command can remotely manage these certificates, ensuring they are up-to-date and compliant.

Defined Certificate Stores of the Certificate Store Type represent specific configurations within Keyfactor Command. These stores can be viewed as specific containers or paths on the F5 Big IQ device where certificates reside. The store configuration includes details such as the server credentials, store path (e.g., partition on the F5 Big IQ device), and other custom settings that determine the interaction between Keyfactor Command and the F5 Big IQ device.

## Compatibility

This integration is compatible with Keyfactor Universal Orchestrator version 10.4 and later.

## Support
The F5 BigIQ Universal Orchestrator extension is supported by Keyfactor for Keyfactor customers. If you have a support issue, please open a support ticket with your Keyfactor representative. If you have a support issue, please open a support ticket via the Keyfactor Support Portal at https://support.keyfactor.com. 
 
> To report a problem or suggest a new feature, use the **[Issues](../../issues)** tab. If you want to contribute actual bug fixes or proposed enhancements, use the **[Pull requests](../../pulls)** tab.

## Installation
Before installing the F5 BigIQ Universal Orchestrator extension, it's recommended to install [kfutil](https://github.com/Keyfactor/kfutil). Kfutil is a command-line tool that simplifies the process of creating store types, installing extensions, and instantiating certificate stores in Keyfactor Command.


1. Follow the [requirements section](docs/f5-bigiq.md#requirements) to configure a Service Account and grant necessary API permissions.

    <details><summary>Requirements</summary>

    ### F5 Big IQ Prerequisites

    When creating a Keyfactor Command Certificate Store, you will be asked to enter server credentials.  These credentials will serve two purposes:
    1. They will be used to authenticate to the F5 Big IQ instance when accessing API endpoints.  Please make sure these credentials have Admin authority on F5 Big IQ.
    2. When Inventorying and Adding/Replacing certificates it will be necessary for certificate files to be transferred to and from the F5 device. The F5 Big IQ Orchestrator Extension uses SCP (Secure Copy Protocol) to perform these functions. Please make sure your F5 Big IQ device is set up to allow SCP to transfer files *to* /var/config/rest/downloads (a reserved F5 Big IQ folder used for file transfers) and *from* /var/config/rest/fileobject (the certificate file location path) and all subfolders. Other configuration tasks may be necessary in your environment to enable this feature.



    </details>

2. Create Certificate Store Types for the F5 BigIQ Orchestrator extension. 

    * **Using kfutil**:

        ```shell
        # F5 Big IQ
        kfutil store-types create F5-BigIQ
        ```

    * **Manually**:
        * [F5 Big IQ](docs/f5-bigiq.md#certificate-store-type-configuration)

3. Install the F5 BigIQ Universal Orchestrator extension.
    
    * **Using kfutil**: On the server that that hosts the Universal Orchestrator, run the following command:

        ```shell
        # Windows Server
        kfutil orchestrator extension -e f5-bigiq-rest-orchestrator@latest --out "C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions"

        # Linux
        kfutil orchestrator extension -e f5-bigiq-rest-orchestrator@latest --out "/opt/keyfactor/orchestrator/extensions"
        ```

    * **Manually**: Follow the [official Command documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/InstallingAgents/NetCoreOrchestrator/CustomExtensions.htm?Highlight=extensions) to install the latest [F5 BigIQ Universal Orchestrator extension](https://github.com/Keyfactor/f5-bigiq-rest-orchestrator/releases/latest).

4. Create new certificate stores in Keyfactor Command for the Sample Universal Orchestrator extension.

    * [F5 Big IQ](docs/f5-bigiq.md#certificate-store-configuration)



## License

Apache License 2.0, see [LICENSE](LICENSE).

## Related Integrations

See all [Keyfactor Universal Orchestrator extensions](https://github.com/orgs/Keyfactor/repositories?q=orchestrator).