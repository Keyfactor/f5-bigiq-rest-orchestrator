## Overview

The F5 BigIQ Universal Orchestrator extension facilitates the management of SSL certificates on F5 Big IQ devices from Keyfactor Command. This orchestration includes the ability to inventory, add, and remove certificates from F5 Big IQ devices. Additionally, it supports the reenrollment of certificates, where a new key pair and certificate signing request (CSR) are generated on the F5 Big IQ device, and the resulting certificate is automatically enrolled and installed.

F5 Big IQ manages SSL certificates to secure communications and provide SSL termination for applications. Through this integration, Keyfactor Command can remotely manage these certificates, ensuring they are up-to-date and compliant.

Defined Certificate Stores of the Certificate Store Type represent specific configurations within Keyfactor Command. These stores can be viewed as specific containers or paths on the F5 Big IQ device where certificates reside. The store configuration includes details such as the server credentials, store path (e.g., partition on the F5 Big IQ device), and other custom settings that determine the interaction between Keyfactor Command and the F5 Big IQ device.

