# bit Boilerplate AI Chatbot Capabilities Using `Microsoft.Extensions.AI`

The **bit Boilerplate** project template has recently been enhanced to include built-in support for **AI chatbots**, leveraging the power of `Microsoft.Extensions.AI`. This feature allows developers to easily integrate advanced natural language processing and conversational interfaces into their applications, whether they're building customer service bots, internal tools, or AI-enhanced dashboards.

---

## 🧠 AI Chatbot Integration in bit Boilerplate

When creating a new project using the bit Boilerplate template, if you select **SignalR**, an AI chatbot module is automatically added to your solution. This chatbot uses the `Microsoft.Extensions.AI` library to provide seamless integration with various large language models (LLMs).

### Supported AI Providers

Currently, the following providers are supported:

- **Azure OpenAI**
- **OpenAI (e.g., ChatGPT)**
- **Google Gemini**
- **DeepSeek**
- **Other OpenAI-compatible endpoints**

---

## 🔧 Configuration Settings

All AI-related configurations are centralized in the appsettings.json file under two main sections: `AzureOpenAI` and `OpenAI`.

### Azure OpenAI Configuration

If you're using **Azure OpenAI**, place your settings under the `AzureOpenAI` section:

```json
"AzureOpenAI": {
  "ChatEndpoint": "https://yourResourceName.openai.azure.com/openai/deployments/yourDeployment",
  "ChatApiKey": "your_azure_openai_api_key"
}
```

>⚠️ **Important**: The endpoint must follow the exact format:
>
> ```
> https://{resourceName}.openai.azure.com/openai/deployments/{deploymentName}
> ```
>
> Replace `{resourceName}` and `{deploymentName}` accordingly.

### OpenAI-Compatible Providers (e.g., ChatGPT, Gemini, DeepSeek)

For non-Azure providers, use the `OpenAI` section:

```json
"OpenAI": {
  "ChatEndpoint": "https://models.inference.ai.azure.com",
  "ChatApiKey": "your_openai_or_provider_api_key"
}
```

> ✅ You can use a **free API key from GitHub Models** at [https://models.inference.ai.azure.com](https://models.inference.ai.azure.com).
>
> If you're not using GitHub's inference endpoint, make sure to replace `ChatEndpoint` with the correct base URL of your chosen provider.

---

## 🔍 Debugging and Logging Requests

To ensure everything works as expected, all AI-related HTTP requests are logged through:

- **ASP.NET Core Console**
- **Diagnostic Modal UI**
- Any configured logging infrastructure (e.g., Serilog, Application Insights)

These logs allow developers to inspect outgoing requests and verify that the correct endpoint is being used.

This visibility is especially crucial when debugging issues related to authentication or incorrect endpoint URLs.

---

## 💾 Embedding Support with PostgreSQL

If your application uses **PostgreSQL** as the database provider, the bit Boilerplate template includes an additional feature: **vector embeddings** powered by Entity Framework Core.

### Why Use Vector Embeddings?

Vector embeddings allow you to store and query high-dimensional data—such as semantic representations of text—in a relational database. This opens up powerful capabilities like:

- Semantic search over product descriptions
- Similarity matching between user queries and stored content
- Personalized recommendations based on embedding distances

### EF Core Integration

The vector functionality is fully integrated with **Entity Framework Core**, meaning:

- Migrations support adding `vector` columns to existing tables
- LINQ queries can be written against these vectors
- You can use standard operators like `Take`, `Skip`, and custom similarity functions

Here’s an example of querying products based on their embeddings:

```csharp
var results = await dbContext.Products
    .OrderBy(p => p.DescriptionEmbedding!.DistanceTo(queryEmbedding))
    .Take(10)
    .ToListAsync();
```

This would return the 10 most semantically similar products to a given query.

> 📌 Note: While this feature currently only works with **PostgreSQL**, it demonstrates a scalable pattern that could be extended to other databases with vector support.