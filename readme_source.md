<span style="color:red">**Please note that this integration will work with the Universal Orchestrator version 10.1 or greater**</span>

## Use Cases

The F5 Big IQ Orchestrator supports the following capabilities for SSL certificates:

- Inventory
- Management (Add and Remove)


## Versioning

The version number of a the F5 Big IQ Orchestrator can be verified by right clicking on the F5BigIQ.dll file, selecting Properties, and then clicking on the Details tab.


## F5 Orchestrator Configuration

1. In Keyfactor Command, create a new certificate store type by navigating to Settings (the "gear" icon in the top right) => Certificate Store Types, and clicking ADD.  Then enter the following information:

**Basic Tab**
- **Name** – Required. The descriptive display name of the new Certificate Store Type.
- **Short Name** – Required. This value ***must be*** F5-BigIQ.
- **Custom Capability** - Leave unchecked
- **Supported Job Types** – Select Inventory Add, and Remove.
- **General Settings** - Select Needs Server.  Select Blueprint Allowed if you plan to use blueprinting.  Leave Uses PowerShell unchecked.
- **Password Settings** - Leave both options unchecked

**Advanced Tab**
- **Store Path Type** - Select Freeform
- **Supports Custom Alias** - Required
- **Private Key Handling** - Required
- **PFX Password Style** - Default

**Custom Fields Tab**

**Ignore SSL Warning** - optional - If you use a self signed certificate for the F5 Big IQ portal, you will need add this Custom Field and set the value to True on the managed certificate store.  Name=IgnoreSSLWarning, Display Name=Ignore SSL Warning, Type=Bool, Default Value={client preference}, Depends on=unchecked, Required=unchecked

**Use Token Authentication** - optional - If you prefer to use F5's Token Authentication to authenticate F5 API calls that the integration uses, you will need to add this Custom Field and set the value to True on the managed certificate store.  If this exists and is set to True for the store, the store userid/password credentials you set for the certificate store will be used once to receive a token.  This token is then used for all remaining API calls for the duration of the job.  If this option does not exist or is set to False, the userid/password credentials you set on the certificate store will be used for each API call.  Name=UseTokenAuth, Display Name=Use Token Authentication, Type=Bool, Default Value={client preference}, Depends on=unchecked, Required=unchecked

Please note, after saving the store type, going back into this screen will show three Custom Fields, Server Username, Server Password, and Use SSL.  These are added internally by Keyfactor Command and do/should not be modified.

