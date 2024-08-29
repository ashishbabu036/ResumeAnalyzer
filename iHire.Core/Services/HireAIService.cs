using IHire.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using OpenAI.Assistants;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace IHire.Core
{
    public class FileUploadedInfo {
        private string documentId;
        public string DocumentId { get => documentId; set => documentId=value; }
    }

    public class Talk2DocsResponse
    {
        public Chat[] QueryMessages;
        public Chat[] AnswerMessages;
    }

    public class Chat
    {
        private string role;
        private string content;

        public string Role { get => role; set => role = value; }
        public string Content { get => content; set => content=value; }
    }


    public class HireAIService : IHireAIService
    {
        public async Task<string> ExtractCandidateInfo(string fileName, string queries)
        {
            string aiResponse = string.Empty;
            string path = Directory.GetCurrentDirectory() + "\\FileDownloaded\\" + fileName;

           
            if (fileName.EndsWith(".pdf"))
            {
                queries = queries + " using  OCR (Optical Character Recognition)";
            }
            else
            {

            }

            AzureOpenAIClient azureClient = new(
           new Uri("https://learnopenai-ashish036.openai.azure.com/"),
           new Azure.AzureKeyCredential("e710a8e5a085494eb7c155dfab483f96"));

#pragma warning disable OPENAI001 
            var client = azureClient.GetAssistantClient();
#pragma warning restore OPENAI001

            var fileUploadResponse = await azureClient.GetFileClient().UploadFileAsync(System.IO.File.Open(path, FileMode.Open),
            fileName, OpenAI.Files.FileUploadPurpose.Assistants);

            bool isPdfFile = fileName.EndsWith("pdf") ? true : false;
            // For Azure OpenAI service the model name is the "deployment" name
            var assistantCreationOptions = new AssistantCreationOptions
            {
                Name = "File question answerer",
                Instructions = isPdfFile ? "The file with the id " + fileUploadResponse.Value.Id + "has original filename of " + fileName + ". This is the Candidate's Resume for role of Software Engineer. " +
                ". Answer questions about the Candidate from the provided file." : " The content contains the candidate's resume for the role of software engineer." +
                " Answer questions about the Candidate from the provided content.",
                Tools = { new CodeInterpreterToolDefinition() }
            };

            // model name here is the name of your AOAI deployment
            var assistant = await client.CreateAssistantAsync("gpt-4o", assistantCreationOptions);

            Console.WriteLine($"Uploaded file {fileUploadResponse.Value.Filename}");

            var thread = await client.CreateThreadAsync();

            {
                var messageCreationOptions = new MessageCreationOptions();
                messageCreationOptions.Attachments.Add(new MessageCreationAttachment(fileUploadResponse.Value.Id, new List<ToolDefinition>() { ToolDefinition.CreateCodeInterpreter() }));

                await client.CreateMessageAsync(thread, new List<MessageContent>() { MessageContent.FromText(queries) }, messageCreationOptions);

                await foreach (StreamingUpdate streamingUpdate
                        in client.CreateRunStreamingAsync(thread, assistant, new RunCreationOptions()))
                {
                    if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunCreated)
                    {
                        Console.WriteLine($"--- Run started! ---");
                    }

                    else if (streamingUpdate is MessageContentUpdate contentUpdate)
                    {
                        if (contentUpdate?.TextAnnotation?.InputFileId == fileUploadResponse.Value.Id)
                        {
                            Console.Write(" (From: " + fileUploadResponse.Value.Filename + ")");
                        }
                        else
                        {
                            Console.Write(contentUpdate?.Text);
                            aiResponse += contentUpdate?.Text;
                            Console.WriteLine("Run Finished " + Environment.NewLine + aiResponse);
                        }
                    }
                }

            }
            return aiResponse;

        }

        public async Task<string> FetchContentFromResume(string documentId)
        {
            string content = string.Empty;
            Talk2DocsResponse talk2DocsResponse = null;
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", "api-key {value}");

                Chat history = new Chat();
                history.Role = "user";
                history.Content = $"Extract Technical Skill Sets in Comma Separated from document with document id :{documentId}";
                var requestBody = new StringContent(JsonConvert.SerializeObject(history));
                using (var response = await httpClient.PostAsync("<Talk2docs URL>", requestBody))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    if (apiResponse != null)
                    {
                        talk2DocsResponse = JsonConvert.DeserializeObject<Talk2DocsResponse>(apiResponse);
                    }
                }
            }

            return talk2DocsResponse.AnswerMessages[0].Content;
        }

        public async Task<FileUploadedInfo> UploadFile(Stream file)

        { 
            FileUploadedInfo fileUploadedInfo = new FileUploadedInfo();
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", "api-key {value}");


                byte[] data;
                using (var br = new BinaryReader(file))
                    data = br.ReadBytes((int)file.Length);

                ByteArrayContent bytes = new ByteArrayContent(data);


                MultipartFormDataContent multiContent = new MultipartFormDataContent();

                multiContent.Add(bytes, "file", "TestResume");

                using (var response = await httpClient.PostAsync("<Talk2docs URL>", multiContent))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    if (apiResponse != null)
                    {
                        fileUploadedInfo = JsonConvert.DeserializeObject<FileUploadedInfo>(apiResponse);
                    }
                }
            }
            return fileUploadedInfo;
        }
    }
}
