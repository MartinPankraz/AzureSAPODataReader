# AzureSAPODataReader

A dotnet 5 web project to showcase integration of Azure AD with Azure API Management for SAP OData consumption leveraging Principal Propagation. Have a look at Martin Raepple's [post series](https://blogs.sap.com/2021/04/13/principal-propagation-in-a-multi-cloud-solution-between-microsoft-azure-and-sap-business-technology-platform-btp-part-iv-sso-with-a-power-virtual-agent-chatbot-and-on-premises-data-gateway/) and specifically [part 5](https://blogs.sap.com/2022/02/13/principal-propagation-in-a-multi-cloud-solution-between-microsoft-azure-and-sap-business-technology-platform-btp-part-v-production-readiness-with-unified-api-and-infrastructure-management/) for more insights on SAP Principal Propagation in this context.

Find my associated blog posts on the SAP Community [here](https://blogs.sap.com/2021/08/12/.net-speaks-odata-too-how-to-implement-azure-app-service-with-sap-odata-gateway/) and [here](https://blogs.sap.com/2022/03/17/open-your-sap-odata-apis-for-some-swagger-or-how-to-make-friends-with-the-other-kids-from-the-api-block/).

> **Important** - See [this official sample repos](https://github.com/Azure-Samples/app-service-javascript-sap-cloud-sdk-quickstart) to learn more about using the SAP Cloud SDK with Azure. Thanks to the Azure Developer CLI everything is up and running with a singel command😎.

See our webcast for more details on the topic [here](https://www.youtube.com/watch?v=VMAHSn_QgXQ&t=750s).
[![webcast teaser](/webcast-episode090.jpg)](https://www.youtube.com/watch?v=VMAHSn_QgXQ&t=750s)

As an example for this project we used the `/sap/opu/odata/sap/epm_ref_apps_prod_man_srv` OData v2 service hosted on our S4 Lab environment. We can also recommend to use `sap/opu/odata/iwbep/GWSAMPLE_BASIC` to play with Updates, Patches and Deletes.

🚀[Quick-Link](https://docs.microsoft.com/azure/api-management/sap-api) to official Microsoft docs for OData metadata import into Azure APIM.

![Overview Architecture](/overview-architecture.png)

We assume you want to run the APIM instance centrally to expose SAP OData services to multiple interested parties. For a fully decoupled design we need to register each component individually with Azure AD.

Our officially provided [APIM policy](https://github.com/Azure/api-management-policy-snippets/blob/master/examples/Request%20OAuth2%20access%20token%20from%20SAP%20using%20AAD%20JWT%20token.xml)🧾 takes care of the SAP Pricipal Propagation complexity.

Consumer apps only need to provide the required scope and be allowed on AAD to authenticate against the middle tier app registration. We add the OData verb $format to force JSON output. Clients like Microsoft PowerPlatform work with JSON only. Consider dropping it from the policy in case you need XML as output format.

![App Registration Overview](/AAD-App-Registration-overview.png)

## ⚙️Azure API Management Config

In case you are running your APIM instance in a private VNet you need to configure certain [network security group rules](https://docs.microsoft.com/azure/api-management/api-management-using-with-vnet?tabs=stv2#network-configuration) on your subnet, so it can reach the required Azure PaaS services.

Once APIM is provisioned and setup you can continue with the SAP OData specific parts:

1. Download the metadata xml for your given service. In our case we called `https://[s4-url]:[backend port]]/sap/opu/odata/sap/epm_ref_apps_prod_man_srv/$metadata`.
2. We recommend to use our [web-based converter](https://aka.ms/ODataOpenAPI) for the conversion from the metadata xml to OpenAPI definition to avoid local setup and specialized features for Microsoft Power Platform.
> Or install the odata to openapi [converter](https://github.com/oasis-tcs/odata-openapi) provided by Oasis and configure your [build environment](https://github.com/oasis-tcs/odata-openapi/tree/main/tools). Note that our web based converter applies post processing to enable PowerPlatform custom connector scenarios. Also I recommend validating your generated openapi spec with [swagger.io](https://editor.swagger.io/) in case you run into import issues.
> With the local converter you need to run `odata-openapi -p --basePath '/sap/opu/odata/sap/epm_ref_apps_prod_man_srv' --scheme https --host <your IP>:<your SSL port> .\epm_ref_apps_prod_man_srv.xml` to create the OpenAPI spec from the downloaded metadata.xml (OData v2) adding the base path of the service and apply option for json pretty print for visual verification. For OData v3/v4 run `odata-openapi3 -p .\metadata.xml` instead.
3. Import OpenAPI spec into Azure API Management. Have a look [here](https://docs.microsoft.com/azure/api-management/import-api-from-oas) for more details.
4. Add GET Operation `/$metadata` when not generated by your files.
5. Add HEAD Operation `/` for efficient client token caching.
6. Add GET Operation `/` including the inbound policy `<rewrite-uri template="/" copy-unmatched-params="true" />`. That ensures the OData collection service on SAP is treated correctly without redirects.
7. Test the $metadata api call to verify connectivity from Azure APIM to your SAP backend

In case your SAP backend uses a self-signed certificate you may need to consider disabling the verification of the trust chain for SSL. To do so maintain an entry under APIs -> Backends -> Add -> Custom URL -> Uncheck Validate certificate chain and name. For productive usage it would be recommended to use proper certificates for end-to-end SSL verification.

SKIP steps 8-11 if you want to rely on client-side caching or implement SAP Principal Propagation at a later stage. We highly recommend using the APIM policy over the client-side approach though. See section `Authentication considerations` for more details.

8. Add policy [SAPPrincipalPropagationAndCachingPolicy.cshtml](Templates/SAPPrincipalPropagationAndCachingPolicy.cshtml) to your API base (All Operations -> Inbound Processing -> `</>`) on APIM and save.
9. Maintain your tenant specific params in [UpdateAPIMwithVariablesForSAPPolicy.sh](Templates/UpdateAPIMwithVariablesForSAPPolicy.sh) locally. The SAP policy relies on them for configuration of the SAP Principal Propagation.
10. Open Azure Cloud Shell from the top menu in your Azure Portal and upload the bash script [UpdateAPIMwithVariablesForSAPPolicy.sh](Templates/UpdateAPIMwithVariablesForSAPPolicy.sh). If you know your way around Azure, of course you can use other means to execute the bash commands.
11. Switch the Azure Cloud Shell environment to bash and run `bash UpdateAPIMwithVariablesForSAPPolicy.sh`

Find more policy snippets and expression cheatsheets on our [Azure Examples Repos](http://aka.ms/apimpolicyexamples).

## 🛠️.NET Frontend Project setup

For you convenience I left the appsettings as templates on the Templates folder. Just move them to your root as you see fit and start configuring. Depending on your caching choice you will need to drop parts of the client-side config.

In addition to that there is a Postman collection with the relevant calls to check your setup. You need to configure the variables for that particular collection to start testing. Please note that the initial AAD login relies on the fragment concept explained by Martin Raepple in his [blog series](https://blogs.sap.com/2020/07/17/principal-propagation-in-a-multi-cloud-solution-between-microsoft-azure-and-sap-cloud-platform-scp/) (step 48) on the wider topic. This is necessary to be able to test from Postman only. The dotnet project does this natively from MSAL.

Find your initial APIM subscription key under APIs -> Subscriptions -> Built-in all-access subscription -> ... -> Show Primary Key

## 🔐 Authentication considerations

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

For further reading on csrf-token handling for SAP with APIM policy, have a look at this [template](Templates/SAPXCSRFTokenPolicy.cshtml) on our repos.

### Client logout and cache purge

If you are using APIM to deal with your tokens, you should consider implementing a logout endpoint that purges the tokens for an individual client from the cache. See the [Microsoft docs](https://docs.microsoft.com/azure/api-management/api-management-caching-policies#RemoveCacheByKey) for cache maintenance for more details.

## 🔬If-Match header and ETags

Be aware that some OData operations like PATCH require the [If-Match header](https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-odata/c3569037-0557-4769-8f75-a91ffcd7b05b) containing your [ETag](https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-odata/c4d715eb-10f6-47fa-9ccc-2ebf926558a6) value to ensure concurrent operations are executed in anticipated order. The provided APIM policy does not cover that by design. Your client needs to feed the ETag and If-Match header accordingly.

## 🩺Troubleshooting hints

- Verify generated OpenAPI spec with [swagger.io](https://editor.swagger.io/) if you have APIM import issues. For instance, we ran into issues with non-unique names for cross OData service references with the TripPinRESTTierService example service for v4 provided by [service.odata.org](https://services.odata.org/TripPinRESTierService/$metadata).
- Leverage [Postman collection](Templates/AAD_APIM_SAP_Principal_Propagation.postman_collection.json) to check each step and see meaningful error message outputs
- Configure [LogAnalytics/Application Insights for APIM](https://docs.microsoft.com/azure/api-management/api-management-howto-use-azure-monitor#resource-logs)
- Alternatively consider adding a SendRequest to your policy and forward relevant info to a RequestBin like [PipeDream](https://pipedream.com/) in case you are not familiar with Azure Log Analytics.
- Use SAP backend transaction __SEC_DIAG_TOOLS__ to trace Principal Propagation issues using the OAuth Client user name.
- Use [Azure APIM policy debugger in VS Code](https://docs.microsoft.com/azure/api-management/api-management-debug-policies). Retrieve the Bearer token through Postman for instance and feed it into the debugger. That enables step by step debugging of each step in the policy. Highly recommend this for sophisticated troubleshooting.
- Create a diagnostic endpoint to read your APIM cache to check on the tokens by user. Have a look at our [template](Templates/APIMScanCachePolicy.cshtml) to get started. It contains logic to clean your cache too in case you "messed" it up ;-). The foundation for it is the Authorization header and bearer token as well as a special header "reset-cache" to initiate delete. You cannot purge the whole cash at once. It has to happen on user by user basis.
- The SAP Refresh token is valid only once! If you use it from Postman but still have it cached in your APIM instance, your cache is broken. Consider overriding or make a "fresh" oauth call.

## 🗃️Thoughts on OData result caching in APIM

One of the strengths of distributed APIM solutions is the capability to cache seldomly changing result sets and serve them from APIM directly instead of the backend. Regarding SAP Principal Propagation this is problematic, because user authorizations are no longer evaluated on the cached results. You would need to add logic to the APIM layer to either request permissions from SAP before returning the cache or also cache the permissions for a limited time. This is aspect is not implemented in the provided app.

## 🧑🏾‍💻Operationalize the approach with APIOps

Working with the converter and APIM UI is nice but doesn't scale to hundreds or thousands of APIs. For that you need to step up the approach to incorporate pipeline tooling and automation. We would recommend to have a look at [this reference about APIOps](https://docs.microsoft.com/azure/architecture/example-scenario/devops/automated-api-deployments-apiops) and [this CI/CD approach with templates](https://docs.microsoft.com/azure/api-management/devops-api-development-templates) to get started. Similar like we leveraged the nodejs files provided by OASIS for our web-converter, you could inject that into your pipeline to convert to openAPI spec on the fly.

## 🙋🏾Contribute

Feel free to post an issue or pull request to our GitHub repo. Other than that, we love to hear from you via LinkedIn or Twitter.
