# AzureSAPODataReader

A dotnet 5 web project to showcase integration of Azure AD with Azure API Management for SAP OData consumption leveraging Principal Propagation. Have a look at Martin Raepple's [post series](https://blogs.sap.com/2021/04/13/principal-propagation-in-a-multi-cloud-solution-between-microsoft-azure-and-sap-business-technology-platform-btp-part-iv-sso-with-a-power-virtual-agent-chatbot-and-on-premises-data-gateway/) for more insights on SAP Principal Propagation.

Find my associated blog post on the SAP Community [here](https://blogs.sap.com/2021/08/12/.net-speaks-odata-too-how-to-implement-azure-app-service-with-sap-odata-gateway/).

As an example for this project we used the `/sap/opu/odata/sap/epm_ref_apps_prod_man_srv` OData v2 service hosted on our S4 Lab environment.

![Overview Architecture](/overview-architecture.png)

We assume you want to run the APIM instance centrally to expose SAP OData services to multiple interested parties. For a fully decoupled design we need to register each component individually with Azure AD. Our provided [APIM policy](https://github.com/Azure/api-management-policy-snippets/blob/master/examples/Request%20OAuth2%20access%20token%20from%20SAP%20using%20AAD%20JWT%20token.xml) takes care of the SAP Pricipal Propagation complexity. Consumer apps only need to provide the required scope and be allowed on AAD to authenticate against the middle tier app registration. We add the OData verb $format to force JSON output. Clients like Microsoft PowerPlatform work with JSON only. Consider dropping it from the policy in case you need XML as output format.

![App Registration Overview](/AAD-App-Registration-overview.png)

## Azure API Management Config

In case you are running your APIM instance in a private VNet you need to configure certain [network security group rules](https://docs.microsoft.com/azure/api-management/api-management-using-with-vnet?tabs=stv2#network-configuration) on your subnet, so it can reach the required Azure PaaS services.

Once APIM is provisioned and setup you can continue with the SAP OData specific parts:

1. Download the metadata xml for your given service. In our case we called `https://[s4-url]:[backend port]]/sap/opu/odata/sap/epm_ref_apps_prod_man_srv/$metadata`.
2. Either use our [web-based converter](https://aka.ms/ODataOpenAPI) or install the odata to openapi [converter](https://github.com/oasis-tcs/odata-openapi) and configure your [build environment](https://github.com/oasis-tcs/odata-openapi/tree/main/tools). Note that our web based converter applies post processing to enable PowerPlatform custom connector scenarios.
3. With the local converter you need to run `odata-openapi -p --basePath '/sap/opu/odata/sap/epm_ref_apps_prod_man_srv' --scheme https --host <your IP>:<your SSL port> .\epm_ref_apps_prod_man_srv.xml` to create the OpenAPI spec from the downloaded metadata.xml adding the base path of the service and apply option for json pretty print for visual verification.
4. Import OpenAPI spec into Azure API Management. Have a look [here](https://docs.microsoft.com/azure/api-management/import-api-from-oas) for more details.
5. Add GET Operation `/$metadata` when not generated by your files.
6. Add HEAD Operation `/` for efficient client token caching.
7. Add GET Operation `/` including the inbound policy `<rewrite-uri template="/" copy-unmatched-params="true" />`. That ensures the OData collection service on SAP is treated correctly without redirects.
8. Test the $metadata api call to verify connectivity from Azure APIM to your SAP backend

In case your SAP backend uses a self-signed certificate you may need to consider disabling the verification of the trust chain for SSL. To do so maintain an entry under APIs -> Backends -> Add -> Custom URL -> Uncheck Validate certificate chain and name. For productive usage it would be recommended to use proper certificates for end-to-end SSL verification.

SKIP steps 9-10 if you want to rely on client-side caching or implement SAP Principal Propagation at a later stage. We highly recommend using the APIM policy over the client-side approach though. See section `Authentication considerations` for more details.

9. Add policy [SAPPrincipalPropagationAndCachingPolicy.cshtml](Templates/SAPPrincipalPropagationAndCachingPolicy.cshtml) to your API base (All Operations -> Inbound Processing -> </>) on APIM and save.
10. Maintain your tenant specific params in [UpdateAPIMwithVariablesForSAPPolicy.sh](Templates/UpdateAPIMwithVariablesForSAPPolicy.sh) locally. The SAP policy relies on them for configuration of the SAP Principal Propagation.
11. Open Azure Cloud Shell from the top menu in your Azure Portal and upload the bash script [UpdateAPIMwithVariablesForSAPPolicy.sh](Templates/UpdateAPIMwithVariablesForSAPPolicy.sh). If you know your way around Azure, of course you can use other means to execute the bash commands.
12. Switch the Azure Cloud Shell environment to bash and run `bash UpdateAPIMwithVariablesForSAPPolicy.sh`

Find more policy snippets and expression cheatsheets on our [Azure Examples Repos](http://aka.ms/apimpolicyexamples).

## .NET Frontend Project setup

For you convenience I left the appsettings as templates on the Templates folder. Just move them to your root as you see fit and start configuring. Depending on your caching choice you will need to drop parts of the client-side config.

In addition to that there is a Postman collection with the relevant calls to check your setup. You need to configure the variables for that particular collection to start testing. Please note that the initial AAD login relies on the fragment concept explained by Martin Raepple in his [blog series](https://blogs.sap.com/2020/07/17/principal-propagation-in-a-multi-cloud-solution-between-microsoft-azure-and-sap-cloud-platform-scp/) (step 48) on the wider topic. This is necessary to be able to test from Postman only. The dotnet project does this natively from MSAL.

Find your initial APIM subscription key under APIs -> Subscriptions -> Built-in all-access subscription -> ... -> Show Primary Key

## Authentication considerations

### Client-side vs. APIM caching for SAP Principal Propagation

You can either do this with client-side caching from the [.NET code](Controllers/HomeController.cs) or leverage our [APIM policy](Templates/SAPPrincipalPropagationAndCachingPolicy.cshtml). We recommend the latter, because it solves SAP Principal Propagation for all clients and lifts the burden for each client to implement the multi-OAuth sequence of calls for the OAuth2SAMLBearerAssertion flow. 

- This project leverages code based configuration with Azure Active Directory leveraging both the "Microsoft.AspNetCore.Authentication" and "Microsoft.Identity.Web" libraries.
- In order to speed up and streamline the handling of the different tokens required for SAP Principal Propagation we implemented a token cache. The MSAL built-in one stores the Azure AD related ones on first login but has no knowledge of the subsequent calls to SAP. For that the custom token cache comes into play. Moving this token acquisition call logic into APIM deprives you of the capability of caching the tokens due to the stateless nature of the setup.

### Avoiding login bursts ("monday morning blues")

People have routines and therefore tend to create clusters of logins at similar times. SAP's OAuth server can become a bottleneck during such periods. We recommend to adjust the default token lifetimes on the SAP OAuth server and implement a random back off delay parameter. That parameter ensures that your cached user tokens don't expire all at the same time even though your users tend to login in waves (monday morning for instance). Our provided [APIM policy](https://github.com/Azure/api-management-policy-snippets/blob/master/examples/Request%20OAuth2%20access%20token%20from%20SAP%20using%20AAD%20JWT%20token.xml) supports that approach. See below an example to illustrate the process:

![token lifetime illustration](/apim-backoff-delay.png)

Of course on the very first day of your implementation when no tokens are cached yet, you are still in trouble ;-) we would recommend to rely on an [APIM throttling policy](https://docs.microsoft.com/azure/api-management/api-management-sample-flexible-throttling) in such cases. Likely you will need to experiment a bit with the parameters to find your individual optimal fit.

### X-CSRF-Token handling

SAP OData services are protected by CSRF tokens usually.

- By default this project leverages a SAP specific [APIM policy](Templates/SAPPrincipalPropagationAndCachingPolicy.cshtml) to inspect http calls for csrf tokens. Inject as we go so to say.
- In case you are looking to use the client-side caching option, have a look at pre-flight logic implemented in [SAPTokenCacheContent.cs](SAPTokenCacheContent.cs) @ `getODataClientSettingsAsync`

For further reading on csrf-token handling for SAP with APIM policy, have a look at this [example](https://docs.microsoft.com/azure/api-management/policies/get-x-csrf-token-from-sap-gateway).

### Troubleshooting hints

- Leverage [Postman collection](Templates/AAD_APIM_SAP_Principal_Propagation.postman_collection.json) to check each step and see meaningful error message outputs
- Configure [LogAnalytics/Application Insights for APIM](https://docs.microsoft.com/azure/api-management/api-management-howto-use-azure-monitor#resource-logs)
- Alternatively consider adding a SendRequest to your policy and forward relevant info to a RequestBin like [PipeDream](https://pipedream.com/) in case you are not familiar with Azure Log Analytics.
- Use SAP backend transaction __SEC_DIAG_TOOLS__ to trace Principal Propagation issues
- Use [Azure APIM policy debugger in VS Code](https://docs.microsoft.com/azure/api-management/api-management-debug-policies). Retrieve the Bearer token through Postman for instance and feed it into the debugger. That enables step by step debugging of each step in the policy

## Thoughts on OData result caching in APIM

One of the strengths of distributed APIM solutions is the capability to cache seldomly changing result sets and serve them from APIM directly instead of the backend. Regarding SAP Principal Propagation this is problematic, because user authorizations are no longer evaluated on the cached results. You would need to add logic to the APIM layer to either request permissions from SAP before returning the cache or also cache the permissions for a limited time. This is aspect is not implemented in the provided app.
