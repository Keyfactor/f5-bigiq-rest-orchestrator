## Overview

The F5 Big IQ Certificate Store Type in Keyfactor Command is a configuration representing SSL certificates on F5 Big IQ devices. This Certificate Store Type enables various certificate management tasks, such as inventorying existing certificates, adding new or renewed certificates, and removing outdated ones. Additionally, it supports reenrollment, where a new key pair and CSR are generated directly on the F5 Big IQ device.

The Certificate Store Type represents a specific container or logical partition on the F5 Big IQ device where certificates are stored. During its setup, users define essential parameters, including server credentials for API authentication, the partition name on the F5 Big IQ device, and other customized settings. These configurations ensure seamless integration between Keyfactor Command and the F5 Big IQ device.

There are several caveats to consider. Firstly, the F5 Big IQ Certificate Store Type requires the configuration of SCP for file transfers, which involves specific folder paths on the F5 Big IQ device. Secondly, it does not support creating new binding relationships between F5 Big IQ and F5 Big IP devices or storing these relationships during inventory.

The F5 Big IQ Certificate Store Type utilizes the integration provided by Keyfactor Command and does not require a separate SDK. However, there are some areas where users might encounter confusion, such as multiple Alias and Overwrite fields when scheduling Reenrollment or Management jobs. Users must distinguish between fields used for different tasks, as outlined in more detailed configuration instructions.

Significant limitations include the prerequisite for administrative credentials and the requirement for SCP setup on the F5 Big IQ device. Understanding these nuances can help ensure smooth certificate management using the F5 Big IQ Universal Orchestrator extension.

## Requirements

### F5 Big IQ Prerequisites

When creating a Keyfactor Command Certificate Store, you will be asked to enter server credentials.  These credentials will serve two purposes:
1. They will be used to authenticate to the F5 Big IQ instance when accessing API endpoints.  Please make sure these credentials have Admin authority on F5 Big IQ.
2. When Inventorying and Adding/Replacing certificates it will be necessary for certificate files to be transferred to and from the F5 device. The F5 Big IQ Orchestrator Extension uses SCP (Secure Copy Protocol) to perform these functions. Please make sure your F5 Big IQ device is set up to allow SCP to transfer files *to* /var/config/rest/downloads (a reserved F5 Big IQ folder used for file transfers) and *from* /var/config/rest/fileobject (the certificate file location path) and all subfolders. Other configuration tasks may be necessary in your environment to enable this feature.

