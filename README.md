# AzureSAPODataReader
A dotnet 5 web project to showcase integration of Azure AD with Azure API Management for SAP OData consumption leveraging Principal Propagation.

We used the `/sap/opu/odata/sap/epm_ref_apps_prod_man_srv` OData v2 service for this project hosted on our S4 Lab environment.

## Azure API Management Config
1. Download the metadata xml for your given service. In our case we called `https://[s4-url]:[backend port]]/sap/opu/odata/sap/epm_ref_apps_prod_man_srv/$metadata`.
2. Install odata to openapi [converter](https://github.com/oasis-tcs/odata-openapi).
3. Configure the converter [tools ](https://github.com/oasis-tcs/odata-openapi/tree/main/tools) including the build environment. We applied `npm install -g windows-build-tools`. Alternatively look for [VS2017 build tools](https://my.visualstudio.com/Downloads?q=Visual+Studio+2017).
4. Run `odata-openapi -p --basePath '/sap/opu/odata/sap/epm_ref_apps_prod_man_srv' --scheme https --host <your IP>:<your SSL port> .\epm_ref_apps_prod_man_srv.xml` to create the OpenAPI spec from the downloaded metadata.xml adding the base path of the service and apply option for json pretty print for visual verification.
5. Import OpenAPI spec into Azure API Management. Have a look [here](https://docs.microsoft.com/en-us/azure/api-management/import-api-from-oas) for more details.
6. Add GET Operation /$metadata when not generated by your files.
6. Add HEAD Operation / for efficient client token caching.
7. Test the $metadata api call to verify connectivity from Azure APIM to your SAP backend

## Authentication considerations
- This project leverages code based configuration with AAD leveraging "Microsoft.AspNetCore.Authentication" and "Microsoft.Identity.Web" library.
- In order to speed up and stream line the handling of the different tokens required for SAP Principal Propagation consider adding an additional token cache. The MSAL built-in one stores the Azure AD related ones on first login but has no knowledge of the subsequent calls to SAP. Moving this token aquisition call logic into APIM deprives you of the capability to caching the tokens due to the stateless nature of the setup.

## X-CSRF-Token handling
SAP OData services are protected by CSRF tokens usually.
- This project leverages code based configuration to inspect http calls for csrf tokens and inject as we go.
- Alternatively you could look into adding an APIM policy for "pre-flight" requests to handle the CSRF token for updates. Have a look at this [example](https://docs.microsoft.com/en-us/azure/api-management/policies/get-x-csrf-token-from-sap-gateway) for more details.