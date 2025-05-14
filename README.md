# Azure AI Agent Service Experiments

This project quickly sets up an Azure AI Agent Service using the `azd up` command. After provisioning, it will also automatically create a local `.env` file under the *examples/* directory so that all examples will have access to the provisioned services.

The deployment includes:
- An Azure **AI Hub** with an **Azure AI Services** connected resource
- A **gpt-4o-mini** model deployment with 10,000 req/min rate limits available via connected resource
- An Azure **AI Project** with access to Hub connected resources

## Examples

- ### [REST API for AI Projects and Agents](./examples/ai-agent-service-create-and-run.http)
   - Prerequisites:
      - Leverages the [VS Code REST Client Extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)
   - References:
      - [REST: Azure AI Agent Service Documentation (with File Search)](https://learn.microsoft.com/en-us/azure/ai-services/agents/how-to/tools/file-search?tabs=rest&pivots=upload-files-code-examples)
      - [Reference documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/assistants-reference)
- ### [Python Azure SDK for Azure AI Projects and Agents](./examples/ai-agent-service-create-and-run.ipynb)
   - Prerequisites:
      - Leverages the [VS Code Polyglot Notebooks Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode)
   - References:
      - [Python: Azure AI Agent Service Documentation (with File Search)](https://learn.microsoft.com/en-us/azure/ai-services/agents/how-to/tools/file-search?tabs=python&pivots=upload-files-code-examples)
      - [Reference documentation](https://aka.ms/azsdk/azure-ai-projects/python/reference)
      - [Samples](https://github.com/Azure/azure-sdk-for-python/tree/main/sdk/ai/azure-ai-projects/samples/agents)
      - [Source code](https://aka.ms/azsdk/azure-ai-projects/python/code)
      - [PyPi package azure-ai-projects](https://aka.ms/azsdk/azure-ai-projects/python/package)
- ### [C# Azure SDK for Azure AI Projects and Agents](./examples/Azure.AI.Projects.AzureAIAgent/Program.cs)
   - Prerequisites:
      - [.NET 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
   - References:
      - [C#: Azure AI Agent Service Documentation (with File Search)](https://learn.microsoft.com/en-us/azure/ai-services/agents/how-to/tools/file-search?tabs=csharp&pivots=upload-files-code-examples)
      - [Reference documentation](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.projects-readme)
      - [Samples](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/ai/Azure.AI.Projects/tests/Samples)
      - [Source code](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/ai/Azure.AI.Projects)
      - [NuGet package Azure.AI.Projects]()
- ### [C# Semantic Kernel Agents](./examples/SemanticKernel.AzureAIAgent/Program.cs)
   - Prerequisites:
      - [.NET 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
   - References:
      - [C# Agents Examples with `AzureAIAgent`](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/GettingStartedWithAgents/AzureAIAgent)
      - [Documentation on `AzureAIAgent`](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/azure-ai-agent?pivots=programming-language-csharp)