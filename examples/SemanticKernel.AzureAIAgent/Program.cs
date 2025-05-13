using System.Diagnostics;
using Azure.AI.Projects;
using Azure.Identity;
using dotenv.net;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;

// Setup environment and Azure.AI.Projects.AgentsClient

DotEnv.Fluent().WithProbeForEnv(probeLevelsToSearch: 6).WithOverwriteExistingVars().Load();

string aiProjectConnectionString = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Environment variable 'AZURE_AI_PROJECT_CONNECTION_STRING' is not set.");

AIProjectClient client = AzureAIAgent.CreateAzureAIClient(aiProjectConnectionString, new AzureCliCredential());
AgentsClient agentsClient = client.GetAgentsClient();

// Prep the File and Vector Store (agent-level vector store)

var filePath = "../files/food-holidays.txt";
if (Debugger.IsAttached)
    filePath = "../../../../files/food-holidays.txt";

AgentFile uploadedHolidaysFile = await agentsClient.UploadFileAsync(
    filePath: filePath,
    purpose: AgentFilePurpose.Agents);

Console.WriteLine($"Uploaded file, file ID: {uploadedHolidaysFile.Id}");

VectorStore vectorStore = await agentsClient.CreateVectorStoreAsync(
    fileIds: [uploadedHolidaysFile.Id],
    name: "holiday-vector-store");

Console.WriteLine($"Created vector store, vector store ID: {vectorStore.Id}");

// This takes some time to digest. 
Thread.Sleep(5000);

// Create the Azure.AI.Projects.Agent with FileSearch tool access

FileSearchToolResource fileSearchToolResource = new();
fileSearchToolResource.VectorStoreIds.Add(vectorStore.Id);

Console.WriteLine($"Created file search tool resource with vector stores: {string.Join(',', fileSearchToolResource.VectorStoreIds)}");

Azure.AI.Projects.Agent aiAgentDefinition = await agentsClient.CreateAgentAsync(
    model: "gpt-4o-mini",
    name: "holidays-recipe-agent",
    description: "You are responsible for knowing what food holidays are on what days.",
    instructions: $"You are a helpful assistant. You answer questions about upcoming events based only on your calendar data. The current date is {DateTime.Now.ToString("MM/dd/yyyy")}. Do not make up events, and do not use other data besides what is available in your file search tool.",
    tools: [new FileSearchToolDefinition()],
    toolResources: new ToolResources() { FileSearch = fileSearchToolResource });

Console.WriteLine($"Created AI Agent Service Agent, agent ID: {aiAgentDefinition.Id}");

// Create a Semantic Kernel AzureAIAgent based on the agent definition

AzureAIAgent agent = new(aiAgentDefinition, agentsClient);

Console.WriteLine($"Created SK AzureAIAgent, agent ID: {agent.Id}");

// Create an SK Thread and add a User Message

Console.WriteLine("Starting chat...");
Console.WriteLine();

Microsoft.SemanticKernel.Agents.AgentThread agentThread = new AzureAIAgentThread(agent.Client);
ChatMessageContent message = new(AuthorRole.User, "What food holidays are coming up over the next week?");
PrettyPrint(message);

try
{
    await foreach (ChatMessageContent response in agent.InvokeAsync(message, agentThread))
    {
        PrettyPrint(response);
    }
}
catch
{
    Console.WriteLine("D'oh! Rate limits hit. Please wait a minute.");
}

Console.WriteLine("End of chat.");

// TODO: Is this possible?
// Add File via Message Attachment (thread-level managed vector store)
// Run the thread and view results

// Cleanup the thread and agent

Console.WriteLine("Cleaning up...");

await agentsClient.DeleteVectorStoreAsync(vectorStore.Id);
await agentsClient.DeleteFileAsync(uploadedHolidaysFile.Id);
await agentThread.DeleteAsync();
await agentsClient.DeleteAgentAsync(agent.Id);

Console.WriteLine("Good bye!");

static void PrettyPrint(ChatMessageContent chatMessageContent)
{
    Console.ForegroundColor = chatMessageContent.Role.Equals(AuthorRole.User) ? ConsoleColor.Yellow :
                                chatMessageContent.Role.Equals(AuthorRole.Assistant) ? ConsoleColor.Gray :
                                ConsoleColor.White;

    Console.WriteLine($"{chatMessageContent.Role.Label.ToUpper()} [{chatMessageContent.AuthorName}]: {chatMessageContent.Content}");
    if (chatMessageContent.Items.Any(i => i is AnnotationContent))
    {
        Console.WriteLine("\nAnnotations:");
        foreach (AnnotationContent annotation in chatMessageContent.Items.Where(i => i is AnnotationContent).Cast<AnnotationContent>())
        {
            Console.WriteLine($"\t{annotation.Quote} source: {annotation.FileId}");
        }
    }

    Console.WriteLine();
    Console.ResetColor();
}