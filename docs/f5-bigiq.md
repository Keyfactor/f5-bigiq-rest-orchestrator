## F5 Big IQ

The F5 Big IQ Certificate Store Type in Keyfactor Command is a configuration representing SSL certificates on F5 Big IQ devices. This Certificate Store Type enables various certificate management tasks, such as inventorying existing certificates, adding new or renewed certificates, and removing outdated ones. Additionally, it supports reenrollment, where a new key pair and CSR are generated directly on the F5 Big IQ device.

The Certificate Store Type represents a specific container or logical partition on the F5 Big IQ device where certificates are stored. During its setup, users define essential parameters, including server credentials for API authentication, the partition name on the F5 Big IQ device, and other customized settings. These configurations ensure seamless integration between Keyfactor Command and the F5 Big IQ device.

There are several caveats to consider. Firstly, the F5 Big IQ Certificate Store Type requires the configuration of SCP for file transfers, which involves specific folder paths on the F5 Big IQ device. Secondly, it does not support creating new binding relationships between F5 Big IQ and F5 Big IP devices or storing these relationships during inventory.

The F5 Big IQ Certificate Store Type utilizes the integration provided by Keyfactor Command and does not require a separate SDK. However, there are some areas where users might encounter confusion, such as multiple Alias and Overwrite fields when scheduling Reenrollment or Management jobs. Users must distinguish between fields used for different tasks, as outlined in more detailed configuration instructions.

Significant limitations include the prerequisite for administrative credentials and the requirement for SCP setup on the F5 Big IQ device. Understanding these nuances can help ensure smooth certificate management using the F5 Big IQ Universal Orchestrator extension.



### Supported Job Types

| Job Name | Supported |
| -------- | --------- |
| Inventory | ✅ |
| Management Add | ✅ |
| Management Remove | ✅ |
| Discovery |  |
| Create |  |
| Reenrollment |  |

## Requirements

### F5 Big IQ Prerequisites

When creating a Keyfactor Command Certificate Store, you will be asked to enter server credentials.  These credentials will serve two purposes:
1. They will be used to authenticate to the F5 Big IQ instance when accessing API endpoints.  Please make sure these credentials have Admin authority on F5 Big IQ.
2. When Inventorying and Adding/Replacing certificates it will be necessary for certificate files to be transferred to and from the F5 device. The F5 Big IQ Orchestrator Extension uses SCP (Secure Copy Protocol) to perform these functions. Please make sure your F5 Big IQ device is set up to allow SCP to transfer files *to* /var/config/rest/downloads (a reserved F5 Big IQ folder used for file transfers) and *from* /var/config/rest/fileobject (the certificate file location path) and all subfolders. Other configuration tasks may be necessary in your environment to enable this feature.



## Certificate Store Type Configuration

The recommended method for creating the `F5-BigIQ` Certificate Store Type is to use [kfutil](https://github.com/Keyfactor/kfutil). After installing, use the following command to create the `` Certificate Store Type:

```shell
kfutil store-types create F5-BigIQ
```

<details><summary>F5-BigIQ</summary>

Create a store type called `F5-BigIQ` with the attributes in the tables below:

### Basic Tab
| Attribute | Value | Description |
| --------- | ----- | ----- |
| Name | F5 Big IQ | Display name for the store type (may be customized) |
| Short Name | F5-BigIQ | Short display name for the store type |
| Capability | F5-BigIQ | Store type name orchestrator will register with. Check the box to allow entry of value |
| Supported Job Types (check the box for each) | Add, Discovery, Remove | Job types the extension supports |
| Supports Add | ✅ | Check the box. Indicates that the Store Type supports Management Add |
| Supports Remove | ✅ | Check the box. Indicates that the Store Type supports Management Remove |
| Supports Discovery |  |  Indicates that the Store Type supports Discovery |
| Supports Reenrollment |  |  Indicates that the Store Type supports Reenrollment |
| Supports Create |  |  Indicates that the Store Type supports store creation |
| Needs Server | ✅ | Determines if a target server name is required when creating store |
| Blueprint Allowed | ✅ | Determines if store type may be included in an Orchestrator blueprint |
| Uses PowerShell |  | Determines if underlying implementation is PowerShell |
| Requires Store Password |  | Determines if a store password is required when configuring an individual store. |
| Supports Entry Password |  | Determines if an individual entry within a store can have a password. |

The Basic tab should look like this:

![F5-BigIQ Basic Tab](../docsource/images/F5-BigIQ-basic-store-type-dialog.png)

### Advanced Tab
| Attribute | Value | Description |
| --------- | ----- | ----- |
| Supports Custom Alias | Required | Determines if an individual entry within a store can have a custom Alias. |
| Private Key Handling | Required | This determines if Keyfactor can send the private key associated with a certificate to the store. Required because IIS certificates without private keys would be invalid. |
| PFX Password Style | Default | 'Default' - PFX password is randomly generated, 'Custom' - PFX password may be specified when the enrollment job is created (Requires the Allow Custom Password application setting to be enabled.) |

The Advanced tab should look like this:

![F5-BigIQ Advanced Tab](../docsource/images/F5-BigIQ-advanced-store-type-dialog.png)

### Custom Fields Tab
Custom fields operate at the certificate store level and are used to control how the orchestrator connects to the remote target server containing the certificate store to be managed. The following custom fields should be added to the store type:

| Name | Display Name | Type | Default Value/Options | Required | Description |
| ---- | ------------ | ---- | --------------------- | -------- | ----------- |


The Custom Fields tab should look like this:

![F5-BigIQ Custom Fields Tab](../docsource/images/F5-BigIQ-custom-fields-store-type-dialog.png)



</details>

## Certificate Store Configuration

After creating the `F5-BigIQ` Certificate Store Type and installing the F5 BigIQ Universal Orchestrator extension, you can create new [Certificate Stores](https://software.keyfactor.com/Core-OnPrem/Current/Content/ReferenceGuide/Certificate%20Stores.htm?Highlight=certificate%20store) to manage certificates in the remote platform.

The following table describes the required and optional fields for the `F5-BigIQ` certificate store type.

| Attribute | Description | Attribute is PAM Eligible |
| --------- | ----------- | ------------------------- |
| Category | Select "F5 Big IQ" or the customized certificate store name from the previous step. | |
| Container | Optional container to associate certificate store with. | |
| Client Machine | For the Client Machine field, enter the full URL of the F5 Big IQ device portal, including the protocol (http or https). For example: https://bigiq.example.com. | |
| Store Path | For the Store Path field, enter the name of the partition on the F5 Big IQ device you wish to manage. Note that this value is case sensitive, for example, 'Common'. | |
| Orchestrator | Select an approved orchestrator capable of managing `F5-BigIQ` certificates. Specifically, one with the `F5-BigIQ` capability. | |

* **Using kfutil**

    ```shell
    # Generate a CSV template for the AzureApp certificate store
    kfutil stores import generate-template --store-type-name F5-BigIQ --outpath F5-BigIQ.csv

    # Open the CSV file and fill in the required fields for each certificate store.

    # Import the CSV file to create the certificate stores
    kfutil stores import csv --store-type-name F5-BigIQ --file F5-BigIQ.csv
    ```

* **Manually with the Command UI**: In Keyfactor Command, navigate to Certificate Stores from the Locations Menu. Click the Add button to create a new Certificate Store using the attributes in the table above.