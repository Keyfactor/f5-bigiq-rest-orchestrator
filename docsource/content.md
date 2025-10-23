## Overview

The F5 Big IQ Orchestrator Extension supports the following use cases:

- Inventories an existing F5 Big IQ device to import SSL certificates into Keyfactor Command for management
- Add an existing or newly enrolled certificate and private key to an existing F5 Big IQ device not already on that device.
- Remove a certificate and private key from an existing F5 Big IQ device.
- Add an existing or newly enrolled certificate and private key to an existing F5 Big IQ device already on that device.  Optionally (based on the DeployCertificateOnRenewal setting on the certificate store), the newly renewed/replaced certificate will be deployed to any linked F5 Big IP device.
- On Device Key Generation (ODKG) of a new or existing certificate on the F5 Big IQ device.  In this use case, the key pair and CSR will be created on the F5 Big IQ device, Keyfactor Command will enroll the certificate, and the certificate will then be installed on the device.  If the DeployCertificateOnRenewal option is set, the certificate will be deployed to any linked F5 Big IP devices.

Use cases NOT supported by the F5 Big IQ Orchestrator Extension:

- Creating new binding relationships between F5 Big IQ and any linked F5 Big IP devices.
- Storing binding relationships in Keyfactor Command during Inventory.

NOTE: Beginning with v2.0 of the F5 Big IQ Orchestrator Extension, there is a minimum requirement of Keyfactor Command 25.3 and Keyfactor Universal Orchestrator 25.3 for ODKG use cases.  Pairing v2.0 or greater with earlier versions of Command and the Universal Orchestrator may lead to errors or unpredictable results using the ODKG functionality.


## Requirements

When creating a Keyfactor Command Certificate Store, you will be asked to enter server credentials.  These credentials will serve two purposes:
1. They will be used to authenticate to the F5 Big IQ instance when accessing API endpoints.  Please make sure these credentials have Admin authority on F5 Big IQ.
2. When Inventorying and Adding/Replacing certificates it will be necessary for certificate files to be transferred to and from the F5 device. The F5 Big IQ Orchestrator Extension uses SCP (Secure Copy Protocol) to perform these functions. Please make sure your F5 Big IQ device is set up to allow SCP to transfer files *to* /var/config/rest/downloads (a reserved F5 Big IQ folder used for file transfers) and *from* /var/config/rest/fileobject (the certificate file location path) and all subfolders. Other configuration tasks may be necessary in your environment to enable this feature.


