targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string
@metadata({azd: {
  type: 'location'
  usageName: 'OpenAI.GlobalStandard.gpt-4o-mini,50'
  }
})
param gpt4oMini20240718Location string


@description('Id of the user or app to assign application roles')
param principalId string

// Tags that should be applied to all resources.
// 
// Note that 'azd-service-name' tags should be applied separately to service host resources.
// Example usage:
//   tags: union(tags, { 'azd-service-name': <service name in azure.yaml> })
var tags = {
  'azd-env-name': environmentName
}

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    tags: tags
    principalId: principalId
    aiFoundryProjectConnectionString: aiModelsDeploy.outputs.aiFoundryProjectConnectionString
  }
}

module aiModelsDeploy 'ai-project.bicep' = {
  scope: rg
  name: 'ai-project'
  params: {
    gpt4oMini20240718Location:  gpt4oMini20240718Location    
    tags: tags
    principalId: principalId
    location: location
    envName: environmentName
  }
}
output AZURE_AI_PROJECT_CONNECTION_STRING string = aiModelsDeploy.outputs.aiFoundryProjectConnectionString
output AZURE_RESOURCE_AI_PROJECT_ID string = aiModelsDeploy.outputs.projectId
output AZURE_AI_AGENTS_SERVICE_HOSTNAME string = concat(
  substring(aiModelsDeploy.outputs.projectDiscoveryUrl, 0, lastIndexOf(aiModelsDeploy.outputs.projectDiscoveryUrl, '/')),
  '/agents/v1.0',
  aiModelsDeploy.outputs.projectId)
