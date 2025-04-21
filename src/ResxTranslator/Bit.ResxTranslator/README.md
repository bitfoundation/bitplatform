# Bit.ResxTranslator

**Bit.ResxTranslator** is a .NET global tool that automates the translation of `.resx` resource files into multiple languages using OpenAI or Azure OpenAI Large Language Models (LLMs).

It efficiently identifies missing translations in your target language files and generates them using the configured LLM, while preserving any existing manual translations.

## Key Features

*   **LLM-Powered Translation:** Uses OpenAI or Azure OpenAI for translating `.resx` entries.
*   **Preserves Existing Translations:** Only adds missing translations to target `.resx` files, leaving existing ones untouched.
*   **Automatic File Generation:** Creates target language `.resx` files (e.g., `AppStrings.fr.resx`) if they don't exist based on your default language file (e.g., `AppStrings.fr.resx`).
*   **Flexible Configuration:** Configure languages, `.resx` file paths (using glob patterns), and LLM provider details via `resx-translate.json`.
*   **.NET Configuration:** Supports API keys via JSON or standard environment variables (e.g., `OpenAI__ApiKey`, `AzureOpenAI__ApiKey`).
*   **Easy Installation:** Installs as a .NET global tool.

## Installation

```bash
dotnet tool install --global Bit.ResxTranslator
```

## Configuration

**Bit.ResxTranslator** requires a configuration file named `resx-translate.json` located in the directory where you execute the `resx-translate` command.

This JSON file defines the source and target languages, the location of your resource files, and the connection details for the LLM service (OpenAI or Azure OpenAI).

**Example `resx-translate.json`:**

```json
{
  // .NET culture's name, en, en-US for example.
  "DefaultLanguage": "en",

  // Required: An array of .NET culture names.
  "AdditionalLanguages": [ "nl", "fa", "sv" ], // e.g., Dutch, Persian, Swedish

  // Required: An array of glob patterns to find your *base* .resx files
  "ResxPaths": [
    "src/**/*.resx"
   ],

  // --- Provide EITHER OpenAI OR AzureOpenAI ---

  // Optional: Configuration for standard OpenAI API
  "OpenAI": {
    "Model": "gpt-4.1-mini",
    "Endpoint": "https://models.inference.ai.azure.com",
    // Required if using OpenAI: API Key.
    // Can be set here OR via Environment Variable "OpenAI__ApiKey"
    // Get one at https://github.com/settings/personal-access-tokens/new
    // More info at https://github.com/marketplace/models
    "ApiKey": null
  },

  // Optional: Configuration for Azure OpenAI Service
  "AzureOpenAI": {
    "Model": "gpt-4.1-mini",
    // Required if using Azure: Your Azure OpenAI resource endpoint URL
    // Can be set here OR via Environment Variable "AzureOpenAI__Endpoint"
    "Endpoint": "https://yourResourceName.openai.azure.com/openai/deployments/yourDeployment",
    // Required if using Azure: API Key.
    // Get one at https://portal.azure.com/
    // Can be set here OR via Environment Variable "AzureOpenAI__ApiKey"
    "ApiKey": null
  }
}
```

***Security Note:*** It is highly recommended to provide your ApiKey using environment variables (`OpenAI__ApiKey` or `AzureOpenAI__ApiKey`) instead of hardcoding it directly in the `resx-translate.json` file,
especially if this file is checked into version control. The tool uses standard .NET configuration practices, meaning environment variables will override values present in the JSON file.

## Usage

```bash
dotnet tool install --global Bit.ResxTranslator

resx-translate
```