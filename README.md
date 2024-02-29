
# F5 BigIQ

The F5 Big IQ Orchestrator allows for the remote management of F5 Big IQ certificate stores.  Inventory and Management functions are supported.

#### Integration status: Prototype - Demonstration quality. Not for use in customer environments.

## About the Keyfactor Universal Orchestrator Extension

This repository contains a Universal Orchestrator Extension which is a plugin to the Keyfactor Universal Orchestrator. Within the Keyfactor Platform, Orchestrators are used to manage “certificate stores” &mdash; collections of certificates and roots of trust that are found within and used by various applications.

The Universal Orchestrator is part of the Keyfactor software distribution and is available via the Keyfactor customer portal. For general instructions on installing Extensions, see the “Keyfactor Command Orchestrator Installation and Configuration Guide” section of the Keyfactor documentation. For configuration details of this specific Extension see below in this readme.

The Universal Orchestrator is the successor to the Windows Orchestrator. This Orchestrator Extension plugin only works with the Universal Orchestrator and does not work with the Windows Orchestrator.

## Support for F5 BigIQ

F5 BigIQ is supported by Keyfactor for Keyfactor customers. If you have a support issue, please open a support ticket via the Keyfactor Support Portal at https://support.keyfactor.com

###### To report a problem or suggest a new feature, use the **[Issues](../../issues)** tab. If you want to contribute actual bug fixes or proposed enhancements, use the **[Pull requests](../../pulls)** tab.

---


---



## Keyfactor Version Supported

The minimum version of the Keyfactor Universal Orchestrator Framework needed to run this version of the extension is 10.4
## Platform Specific Notes

The Keyfactor Universal Orchestrator may be installed on either Windows or Linux based platforms. The certificate operations supported by a capability may vary based what platform the capability is installed on. The table below indicates what capabilities are supported based on which platform the encompassing Universal Orchestrator is running.
| Operation | Win | Linux |
|-----|-----|------|
|Supports Management Add|&check; |&check; |
|Supports Management Remove|&check; |&check; |
|Supports Create Store|  |  |
|Supports Discovery|  |  |
|Supports Renrollment|  |  |
|Supports Inventory|&check; |&check; |


## PAM Integration

This orchestrator extension has the ability to connect to a variety of supported PAM providers to allow for the retrieval of various client hosted secrets right from the orchestrator server itself.  This eliminates the need to set up the PAM integration on Keyfactor Command which may be in an environment that the client does not want to have access to their PAM provider.

The secrets that this orchestrator extension supports for use with a PAM Provider are:

|Name|Description|
|----|-----------|
|ServerUsername|The user id that will be used to authenticate to the F5 Biq API endpoints|
|ServerPassword|The password that will be used to authenticate to the F5 Biq API endpoints|
  

It is not necessary to use a PAM Provider for all of the secrets available above. If a PAM Provider should not be used, simply enter in the actual value to be used, as normal.

If a PAM Provider will be used for one of the fields above, start by referencing the [Keyfactor Integration Catalog](https://keyfactor.github.io/integrations-catalog/content/pam). The GitHub repo for the PAM Provider to be used contains important information such as the format of the `json` needed. What follows is an example but does not reflect the `json` values for all PAM Providers as they have different "instance" and "initialization" parameter names and values.

<details><summary>General PAM Provider Configuration</summary>
<p>



### Example PAM Provider Setup

To use a PAM Provider to resolve a field, in this example the __Server Password__ will be resolved by the `Hashicorp-Vault` provider, first install the PAM Provider extension from the [Keyfactor Integration Catalog](https://keyfactor.github.io/integrations-catalog/content/pam) on the Universal Orchestrator.

Next, complete configuration of the PAM Provider on the UO by editing the `manifest.json` of the __PAM Provider__ (e.g. located at extensions/Hashicorp-Vault/manifest.json). The "initialization" parameters need to be entered here:

~~~ json
  "Keyfactor:PAMProviders:Hashicorp-Vault:InitializationInfo": {
    "Host": "http://127.0.0.1:8200",
    "Path": "v1/secret/data",
    "Token": "xxxxxx"
  }
~~~

After these values are entered, the Orchestrator needs to be restarted to pick up the configuration. Now the PAM Provider can be used on other Orchestrator Extensions.

### Use the PAM Provider
With the PAM Provider configured as an extenion on the UO, a `json` object can be passed instead of an actual value to resolve the field with a PAM Provider. Consult the [Keyfactor Integration Catalog](https://keyfactor.github.io/integrations-catalog/content/pam) for the specific format of the `json` object.

To have the __Server Password__ field resolved by the `Hashicorp-Vault` provider, the corresponding `json` object from the `Hashicorp-Vault` extension needs to be copied and filed in with the correct information:

~~~ json
{"Secret":"my-kv-secret","Key":"myServerPassword"}
~~~

This text would be entered in as the value for the __Server Password__, instead of entering in the actual password. The Orchestrator will attempt to use the PAM Provider to retrieve the __Server Password__. If PAM should not be used, just directly enter in the value for the field.
</p>
</details> 




---


<span style="color:red">**Please note that this integration will work with the Universal Orchestrator version 10.1 or greater**</span>

## Use Cases

The F5 Big IQ Orchestrator Extension supports the following capabilities for SSL certificates:

- Inventory
- Management (Add and Remove)


## Versioning

The version number of a the F5 Big IQ Orchestrator Extension can be verified by right clicking on the F5BigIQ.dll file, selecting Properties, and then clicking on the Details tab.


## F5 Big IQ Prerequisites

When creating a Keyfactor Command Certificate Store, you will be asked to enter server credentials.  These credentials will serve two purposes:
1. They will be used to authenticate to the F5 Big IQ instance when accessing API endpoints.  Please make sure these credentials have Admin authority on F5 Big IQ.
2. When Inventorying and Adding/Replacing certificates it will be necessary for certificate files to be transferred to and from the F5 device.  The F5 Big IQ Orchestrator Extension uses SCP (Secure Copy Protocol) to perform these functions.  Please make sure your F5 Big IQ device is set up to allow SCP to transfer files *to* /var/config/rest/downloads (a reserved F5 Big IQ folder used for file transfers) and *from* /var/config/rest/fileobject (the certificate file location path) and all subfolders.  You may need go into the /etc/ssh/sshd_config file on your F5 Big IQ device and set PasswordAuthentication from “No” to “Yes” for SCP to work.  Other configuration tasks may be necessary in your environment to enable this feature.


## F5 Big IQ Orchestrator Extension Installation

1. Stop the Keyfactor Universal Orchestrator Service.
2. In the Keyfactor Orchestrator installation folder (by convention usually C:\Program Files\Keyfactor\Keyfactor Orchestrator), find the "extensions" folder. Underneath that, create a new folder named F5BigIQ or another name of your choosing.
3. Download the latest version of the F5 BigIQ Orchestrator Extension from [GitHub](https://github.com/Keyfactor/f5-bigiq-rest-orchestrator).
4. Copy the contents of the download installation zip file into the folder created in step 2.
5. Start the Keyfactor Universal Orchestrator Service.


## F5 Big IQ Orchestrator Extension Configuration

### 1\. In Keyfactor Command, create a new certificate store type by navigating to Settings (the "gear" icon in the top right) => Certificate Store Types, and clicking ADD.  Then enter the following information:

**Basic Tab**
- **Name** – Required. The descriptive display name of the new Certificate Store Type.  Suggested => F5 Big IQ
- **Short Name** – Required. This value ***must be*** F5-BigIQ.
- **Custom Capability** - Leave unchecked
- **Supported Job Types** – Select Inventory, Add, and Remove.
- **General Settings** - Select Needs Server.  Select Blueprint Allowed if you plan to use blueprinting.  Leave Uses PowerShell unchecked.
- **Password Settings** - Leave both options unchecked

**Advanced Tab**
- **Store Path Type** - Select Freeform
- **Supports Custom Alias** - Required
- **Private Key Handling** - Required
- **PFX Password Style** - Default

**Custom Fields Tab**

- **Deploy Certificate to Linked Big IP on Renewal** - optional - This setting determines you wish to deploy renewed certificates (Management-Add jobs with Overwrite selected) to all linked Big IP devices.  Linked devices are determined by looking at all of the client-ssl profiles that reference the renewed certificate that have an associated virtual server linked to a Big IP device.  An "immediate" deployment is then scheduled within F5 Big IQ for each linked Big IP device. 
  - **Name**=DeployCertificateOnRenewal
  - **Display Name**=Deploy Certificate to Linked Big IP on Renewal
  - **Type**=Bool
  - **Default Value**={client preference}
  - **Depends on**=unchecked
  - **Required**=unchecked

- **Ignore SSL Warning** - optional - If you use a self signed certificate for the F5 Big IQ portal, you will need add this Custom Field and set the value to True on the managed certificate store.
  - **Name**=IgnoreSSLWarning
  - **Display Name**=Ignore SSL Warning
  - **Type**=Bool
  - **Default Value**={client preference}
  - **Depends on**=unchecked
  - **Required**=unchecked

- **Use Token Authentication** - optional - If you prefer to use F5 Big IQ's Token Authentication to authenticate F5 Big IQ API calls that the integration uses, you will need to add this Custom Field and set the value to True on the managed certificate store.  If this exists and is set to True for the store, the store userid/password credentials you set for the certificate store will be used once to receive a token.  This token is then used for all remaining API calls for the duration of the job.  If this option does not exist or is set to False, the userid/password credentials you set on the certificate store will be used for each API call.
  - **Name**=UseTokenAuth
  - **Display Name**=Use Token Authentication
  - **Type**=Bool
  - **Default Value**={client preference}
  - **Depends on**=unchecked
  - **Required**=unchecked

  Please note, after saving the store type, going back into this screen will show three additional Custom Fields: Server Username, Server Password, and Use SSL.  These are added internally by Keyfactor Command and should not be modified.

**Entry Parameters Tab**

No Entry Parameters should be added.


### 2\. Create an F5 Big IQ Certificate Store

Navigate to Certificate Locations =\> Certificate Stores within Keyfactor Command to add the store. Below are the values that should be entered:

- **Category** – Required.  Select the Name you entered when creating the Certificate Store Type.  Suggested value was F5 Big IQ.

- **Container** – Optional.  Select a container if utilized.

- **Client Machine & Credentials** – Required.  The full URL of the F5 Big IQ device portal.  
  
- **Store Path** – Required.  Enter the name of the partition on the F5 Big IQ device you wish to manage.  This value is case sensitive, so if the partition name is "Common", it must be entered as "Common" and not "common".

- **Orchestrator** – Required.  Select the orchestrator you wish to use to manage this store

- **Deploy Certificate to Linked Big IP on Renewal** - Optional.  Set this to True if you wish to deploy renewed certificates (Management-Add jobs with Overwrite selected) to all linked Big IP devices.  Linked devices are determined by looking at all of the client-ssl profiles that reference the renewed certificate that have an associated virtual server linked to a Big IP device.  An "immediate" deployment is then scheduled within F5 Big IQ for each linked Big IP device. 

- **Ignore SSL Warning** - Optional.  Set this to True if you wish to ignore SSL warnings from F5 that occur during API calls when the site does not have a trusted certificate with the proper SAN bound to it.  If you chose not to add this Custom Field when creating the Certificate Store Type, the default value of False will be assumed.  If this value is False (or missing) SSL warnings will cause errors during orchestrator extension jobs.

- **Use Token Authentication** - Optional.  Set this to True if you wish to use F5 Big IQ's token authentiation instead of basic authentication for all API requests.  If you chose not to add this optional Custom Field when creating the Certificate Store Type, the default value of False will be assumed and basic authentication will be used for all API requests for all jobs.  Setting this value to True will enable an initial basic authenticated request to acquire an authentication token, which will then be used for all subsequent API requests.

- **Server Username/Password** - Required.  The credentials used to log into the F5 Big IQ device to perform API calls.  These values for server login can be either:
  
  - UserId/Password
  - PAM provider information used to look up the UserId/Password credentials

  Please make sure these credentials have Admin rights on the F5 Big IQ device and can perform SCP functions as described in the F5 Big IQ Prerequisites section above.

- **Use SSL** - N/A.  This value is not referenced in the F5 Big IQ Orchestrator Extension.  The value you enter for Client Machine, and specifically whether the protocol entered is http:// or https:// will determine whether a TLS (SSL) connection is utilized.

- **Inventory Schedule** – Set a schedule for running Inventory jobs or "none", if you choose not to schedule Inventory at this time.

