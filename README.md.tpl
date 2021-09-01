# {{ name }}
## {{ integration_type | capitalize }}

{{ description }}

<!-- add integration specific information below -->
*** 
## **A10 vThunder Configuration**

**Overview**

The A10 vThunder Agent allows a user to inventory certificates and manage (add/remove/replace) certificates from the A10 vThunder platform.

**1) Create the new Certificate store Type for the New A10 vThunder AnyAgent**

In Keyfactor Command create a new Certificate Store Type similar to the one below:

![image.png](/Media/Images/CertStores.gif)


- **Name** – Required. The display name of the new Certificate Store Type
- **Short Name** – Required. MUST be "vThunder"

- **Needs Server, Blueprint Allowed** – checked as shown
- **Requires Store Password, Supports Entry Password** – unchecked as shown
- **Supports Custom Alias** – Forbidden. Not used.
- **Use PowerShell** – Unchecked
- **Store PathType** – Freeform (user will enter the the location of the store).
- **Private Keys** – Optional
- **PFX Password Style** – Default
- **Job Types** – Inventory, Add and Remove are the 3 job types implemented by this AnyAgent
   
    
**2) Register the A10 vThunder AnyAgent with Keyfactor**

Open the Keyfactor Windows Agent Configuration Wizard and perform the tasks as illustrated below:

![image.png](/Media/Images/ConfigWizard1.gif)

- Click **<Next>**

![image.png](/Media/Images/ConfigWizard2.gif)

If you have configured the agent service previously, you should be able to skip to just click **<Next>**. Otherwise, enter the service account Username and Password you wish to run the Keyfactor Windows Agent Service under, click **<Update Windows Service Account>** and click **<Next>**.

![image.png](/Media/Images/ConfigWizard3.gif)

If you have configured the agent service previously, you should be able to skip to just re-enter the password to the service account the agent service will run under, click **<Validate Keyfactor Connection>** and then **<Next>**.

![image.png](/Media/Images/ConfigWizard4.gif)

Select the agent you are adding capabilities for (in this case, vThunder, and also select the specific capabilities (Inventory and Management in this example). Click **<Next>**.

![image.png](/Media/Images/ConfigWizard5.gif)

For each AnyAgent implementation, check Load assemblies containing extension modules from other location , browse to the location of the compiled AnyAgent dlls, and click **<Validate Capabilities>**. Once all AnyAgents have been validated, click **<Apply Configuration>**.

![image.png](/Media/Images/ConfigWizard6.gif)

If the Keyfactor Agent Configuration Wizard configured everything correctly, you should see the dialog above.

**3) Create a Cert Store within the Keyfactor Portal**

Navigate to Certificate Locations => Certificate Stores within Keyfactor Command to add an A10 vThunder certificate store. Below are the values that should be entered.

![image.png](/Media/Images/CertStores.gif)

- **Category** – Required. The vThunder category name must be selected
- **Container** – Optional. Select a container if utilized.
- **Client Machine** – Required. The server name or IP Address of the A10 vThunder API plus port.  [Azure Test Machine](https://portal.azure.com/#@csspkioutlook.onmicrosoft.com/resource/subscriptions/b3114ff1-bb92-45b6-9bd6-e4a1eed8c91e/resourceGroups/kVThunderA10/providers/Microsoft.Compute/virtualMachines/kVThunderA10/overview) port is :1113 for ssl.
- **Store Path** – Required.  This will be one of the following based on what you are looking to add to the store.
1. **[DomainName]\cert** where [DomainName] is the name of the domain in A10 vThunder you are looking to manage and inventory.

2.  **cert** - This will use the default domain in A10 vThunder to manage and inventory **domain** certs

3. **[DomainName]\pubcert** - This will give you the ability to Inventory the Pub Cert Folder on the specified domain where [DomainName] is the name of the domain in A10 vThunder you are looking to inventory.  

4. **pubcert** - This will use the default domain in A10 vThunder to manage and inventory **public certs** certs

### App Config Settings
Keyfactor.AnyAgent.vThunder.dll.config (Deployed with all AnyAgent Binaries)
```
<appSettings>
    <!--Should be https, made configurable in case needed for dev or whatever-->
    <add key="Protocol" value="https" />
    <!--true for debugging/testing on Azure VM since the cert will be invalid at .eastus.cloudapp.azure.com should be false in Production with a valid cert-->
    <add key="AllowInvalidCerts" value="true" />
</appSettings>
```

There are 2 App Config Settings
1. Protocol should always be **https** in **Production** but you may need to switch to **http** for **Testing** only

2. AllowInvalidCerts should be set to **false** in **Production**.  It is set to **true** in **Dev/Test** since the VM we are testing with a vThunder VM that does not have a valid SSL Certificate on the Azure Platform.
