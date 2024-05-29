## Versioning

The version number of a the F5 Big IQ Orchestrator Extension can be verified by right clicking on the F5BigIQ.dll file, selecting Properties, and then clicking on the Details tab.


## F5 Big IQ Prerequisites

When creating a Keyfactor Command Certificate Store, you will be asked to enter server credentials.  These credentials will serve two purposes:
1. They will be used to authenticate to the F5 Big IQ instance when accessing API endpoints.  Please make sure these credentials have Admin authority on F5 Big IQ.
2. When Inventorying and Adding/Replacing certificates it will be necessary for certificate files to be transferred to and from the F5 device. The F5 Big IQ Orchestrator Extension uses SCP (Secure Copy Protocol) to perform these functions. Please make sure your F5 Big IQ device is set up to allow SCP to transfer files *to* /var/config/rest/downloads (a reserved F5 Big IQ folder used for file transfers) and *from* /var/config/rest/fileobject (the certificate file location path) and all subfolders. Other configuration tasks may be necessary in your environment to enable this feature.


## F5 Big IQ Orchestrator Extension Installation

1. Stop the Keyfactor Universal Orchestrator Service.
2. In the Keyfactor Orchestrator installation folder (by convention usually C:\Program Files\Keyfactor\Keyfactor Orchestrator), find the "extensions" folder. Underneath that, create a new folder named F5BigIQ or another name of your choosing.
3. Download the latest version of the F5 BigIQ Orchestrator Extension from [GitHub](https://github.com/Keyfactor/f5-bigiq-rest-orchestrator).
4. Copy the contents of the download installation zip file into the folder created in step 2.
5. Start the Keyfactor Universal Orchestrator Service.


## F5 Big IQ Orchestrator Extension Configuration

### 1\. In Keyfactor Command, create a new certificate store type by navigating to Settings (the "gear" icon in the top right) => Certificate Store Types, and clicking ADD.  Then enter the following information:

<details>
<summary><b>Basic Tab</b></summary>
- **Name** – Required. The descriptive display name of the new Certificate Store Type.  Suggested => F5 Big IQ
- **Short Name** – Required. This value ***must be*** F5-BigIQ.
- **Custom Capability** - Leave unchecked
- **Supported Job Types** – Select Inventory, Add, and Remove.
- **General Settings** - Select Needs Server.  Select Blueprint Allowed if you plan to use blueprinting.  Leave Uses PowerShell unchecked.
- **Password Settings** - Leave both options unchecked

</details>

<details>
<summary><b>Advanced Tab</b></summary>
- **Store Path Type** - Select Freeform
- **Supports Custom Alias** - Required
- **Private Key Handling** - Required
- **PFX Password Style** - Default

</details>

<details>
<summary><b>Custom Fields Tab</b></summary>

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

- **Use Token Authentication Provider Name** - optional - If Use Token Authentication is selected, you may optionally add a value for the authentication provider F5 Big IQ will use to retrieve the auth token.  If you choose not to add this field or leave it blank on the certificate store (with no default value set), the default of "TMOS" will be used.
  - **Display Name**=Use Token Authentication Provider Name
  - **Type**=String
  - **Default Value**={client preference}
  - **Depends on**="UseTokenAuth"
  - **Required**=unchecked   

Please note, after saving the store type, going back into this screen will show three additional Custom Fields: Server Username, Server Password, and Use SSL.  These are added internally by Keyfactor Command and should not be modified.

</details>

<details>
<summary><b>Entry Parameters Tab</b></summary>

No Entry Parameters should be added.

</details>

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

- **Use Token Authentication Provider Name** - Optional - If Use Token Authentication is selected, you may optionally add a value for the authentication provider F5 Big IQ will use to retrieve the auth token.  If you choose leave this field blank, the default of "TMOS" will be used.  


- **Server Username/Password** - Required.  The credentials used to log into the F5 Big IQ device to perform API calls.  These values for server login can be either:
  
  - UserId/Password
  - PAM provider information used to look up the UserId/Password credentials

  Please make sure these credentials have Admin rights on the F5 Big IQ device and can perform SCP functions as described in the F5 Big IQ Prerequisites section above.

- **Use SSL** - N/A.  This value is not referenced in the F5 Big IQ Orchestrator Extension.  The value you enter for Client Machine, and specifically whether the protocol entered is http:// or https:// will determine whether a TLS (SSL) connection is utilized.

- **Inventory Schedule** – Set a schedule for running Inventory jobs or "none", if you choose not to schedule Inventory at this time.