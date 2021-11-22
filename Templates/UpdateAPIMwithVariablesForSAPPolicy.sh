export resourceGroup="<your Azure resource group>"
export APIMserviceName="<your Azure APIM domain name>"
export AzureSubscription="<your Azure subscription id>"
export AADTenantId="<your Azure AD tenant id>"
export APIMAADRegisteredAppClientId="<your Azure AD registered APIM client id>"
export APIMAADRegisteredAppClientSecret="<your Azure AD registered APIM client secret>"
export AADSAPResource="spn:<your Azure AD app registered SAP resource identifier (likely your SID)>"
export SAPOAuthClientID="<your SAP OAuth client id (check TC OAUTH2)>"
export SAPOAuthClientSecret="<your SAP Oauth client secret (check TC OAUTH2)>"
export SAPOAuthScope="<your SAP OAuth Scope (check TC OAUTH2)>"
export SAPOAuthServerAdressForTokenEndpoint="<ip or domain>:<SAP backend ssl port>"

az apim nv create --display-name 'AADTenantId' --named-value-id AADTenantId -g $resourceGroup --service-name $APIMserviceName --secret false --subscription $AzureSubscription --value $AADTenantId

az apim nv create --display-name 'APIMAADRegisteredAppClientId' --named-value-id APIMAADRegisteredAppClientId -g $resourceGroup --service-name $APIMserviceName --secret false --subscription $AzureSubscription --value $APIMAADRegisteredAppClientId

az apim nv create --display-name 'APIMAADRegisteredAppClientSecret' --named-value-id APIMAADRegisteredAppClientSecret -g $resourceGroup --service-name $APIMserviceName --secret true --subscription $AzureSubscription --value $APIMAADRegisteredAppClientSecret

az apim nv create --display-name 'AADSAPResource' --named-value-id AADSAPResource -g $resourceGroup --service-name $APIMserviceName --secret false --subscription $AzureSubscription --value $AADSAPResource

az apim nv create --display-name 'SAPOAuthClientID' --named-value-id SAPOAuthClientID -g $resourceGroup --service-name $APIMserviceName --secret false --subscription $AzureSubscription --value $SAPOAuthClientID

az apim nv create --display-name 'SAPOAuthClientSecret' --named-value-id SAPOAuthClientSecret -g $resourceGroup --service-name $APIMserviceName --secret true --subscription $AzureSubscription --value $SAPOAuthClientSecret

az apim nv create --display-name 'SAPOAuthScope' --named-value-id SAPOAuthScope -g $resourceGroup --service-name $APIMserviceName --secret false --subscription $AzureSubscription --value $SAPOAuthScope

az apim nv create --display-name 'SAPOAuthServerAdressForTokenEndpoint' --named-value-id SAPOAuthServerAdressForTokenEndpoint -g $resourceGroup --service-name $APIMserviceName --secret false --subscription $AzureSubscription --value $SAPOAuthServerAdressForTokenEndpoint