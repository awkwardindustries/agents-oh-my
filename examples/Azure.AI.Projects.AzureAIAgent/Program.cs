using System.Diagnostics;
using Azure.AI.Projects;
using Azure.Identity;
using dotenv.net;

// Setup environment and Azure.AI.Projects.AgentsClient

DotEnv.Fluent().WithProbeForEnv(probeLevelsToSearch: 6).WithOverwriteExistingVars().Load();

string aiProjectConnectionString = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Environment variable 'AZURE_AI_PROJECT_CONNECTION_STRING' is not set.");

AgentsClient agentsClient = new(aiProjectConnectionString, new DefaultAzureCredential());

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

Agent agent = await agentsClient.CreateAgentAsync(
    model: "gpt-4o-mini",
    name: "holidays-recipe-agent",
    description: "You are responsible for knowing what food holidays are on what days.",
    instructions: $"You are a helpful assistant. You answer questions about upcoming events based only on your calendar data. The current date is {DateTime.Now.ToString("MM/dd/yyyy")}. Do not make up events, and do not use other data besides what is available in your file search tool.",
    tools: [new FileSearchToolDefinition()],
    toolResources: new ToolResources() { FileSearch = fileSearchToolResource });

Console.WriteLine($"Created AI Agent Service Agent, agent ID: {agent.Id}");

// Create the Thread, and add a User Message

AgentThread thread = await agentsClient.CreateThreadAsync();

Console.WriteLine($"Created Thread, thread ID: {thread.Id}");

ThreadMessage message = await agentsClient.CreateMessageAsync(
    threadId: thread.Id,
    role: MessageRole.User,
    content: "What food holidays are coming up over the next week?");

// Run the Thread and View Results

ThreadRun run = await agentsClient.CreateRunAsync(thread, agent);

Console.WriteLine($"Created Run, run ID: {run.Id}");

do
{
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    run = await agentsClient.GetRunAsync(thread.Id, run.Id);
}
while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);

Console.WriteLine("Chat log...");
Console.WriteLine();

PageableList<ThreadMessage> messages = await agentsClient.GetMessagesAsync(
    thread.Id, order: ListSortOrder.Ascending);
foreach (var m in messages)
{
    PrettyPrint(m);
}

Console.WriteLine("End of chat.");

// Add File via Message Attachment (thread-level managed vector store)

var filePathBirthdays = "../files/birthdays.txt";
if (Debugger.IsAttached)
    filePath = "../../../../files/birthdays.txt";

AgentFile uploadedBirthdaysFile = await agentsClient.UploadFileAsync(
    filePath: filePathBirthdays,
    purpose: AgentFilePurpose.Agents);

Console.WriteLine($"Uploading second file, file ID: {uploadedBirthdaysFile.Id}");

MessageAttachment messageAttachment = new(uploadedBirthdaysFile.Id, [new FileSearchToolDefinition()]);
ThreadMessage messageWithAttachment = await agentsClient.CreateMessageAsync(
    threadId: thread.Id,
    role: MessageRole.User,
    content: "I need to prepare something special for a teammate's birthday. What food holidays are on the next two team members' birthdays?",
    attachments: [messageAttachment]);

// Run the Thread and View Results

ThreadRun messageAttachmentRun = await agentsClient.CreateRunAsync(thread, agent);

Console.WriteLine($"Created second run, run ID: {messageAttachmentRun.Id}");

do
{
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    messageAttachmentRun = await agentsClient.GetRunAsync(thread.Id, messageAttachmentRun.Id);
}
while (messageAttachmentRun.Status == RunStatus.Queued || messageAttachmentRun.Status == RunStatus.InProgress);

Console.WriteLine($"Run finished: {messageAttachmentRun.Status} {messageAttachmentRun?.LastError?.Message}");

Console.WriteLine("Chat log...");
Console.WriteLine();

PageableList<ThreadMessage> messageAttachmentRunMessages = await agentsClient.GetMessagesAsync(
    thread.Id, order: ListSortOrder.Ascending);
foreach (var m in messageAttachmentRunMessages)
{
    PrettyPrint(m);
}

Console.WriteLine("End of chat.");

// Cleanup files, vector stores, threads, agents

Console.WriteLine("Cleaning up...");

await agentsClient.DeleteVectorStoreAsync(vectorStore.Id);
await agentsClient.DeleteFileAsync(uploadedHolidaysFile.Id);
await agentsClient.DeleteFileAsync(uploadedBirthdaysFile.Id);
await agentsClient.DeleteThreadAsync(thread.Id);
await agentsClient.DeleteAgentAsync(agent.Id);

Console.WriteLine("Good bye!");

// Helper methods ------------------------------

static void PrettyPrint(ThreadMessage message)
{
    Console.ForegroundColor = message.Role.Equals(MessageRole.User) ? ConsoleColor.Yellow :
                              message.Role.Equals(MessageRole.Agent) ? ConsoleColor.Gray :
                              ConsoleColor.White;

    foreach (MessageContent messageContent in message.ContentItems)
    {
        if (messageContent is not MessageTextContent)
            break;
        
        MessageTextContent messageTextContent = (MessageTextContent)messageContent;
        Console.WriteLine($"{message.Role.ToString().ToUpper()} [{message.AssistantId}]: {messageTextContent.Text}");
        if (messageTextContent.Annotations.Any(i => i is MessageTextFileCitationAnnotation))
        {
            Console.WriteLine("\nAnnotations:");
            foreach (var annotation in messageTextContent.Annotations.Where(i => i is MessageTextFileCitationAnnotation).Cast<MessageTextFileCitationAnnotation>())
            {
                Console.WriteLine($"\t{annotation.Quote} source: {annotation.FileId}");
            }
        }
    }

    Console.WriteLine();
    Console.ResetColor();
}