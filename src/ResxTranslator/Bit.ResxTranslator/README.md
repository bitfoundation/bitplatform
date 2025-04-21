# Bit.ResxTranslator

**Bit.ResxTranslator** is a .NET global tool that automates the translation of `.resx` resource files into multiple languages using OpenAI or Azure OpenAI Large Language Models (LLMs).

It efficiently identifies missing translations in your target language files and generates them using the configured LLM, while preserving any existing manual translations.

## Key Features

*   **LLM-Powered Translation:** Uses OpenAI or Azure OpenAI for translating `.resx` entries.
*   **Preserves Existing Translations:** Only adds missing translations to target `.resx` files, leaving existing ones untouched.
*   **Automatic File Generation:** Creates target language `.resx` files (e.g., `AppStrings.fr.resx`) if they don't exist based on your default language file (e.g., `AppStrings.fr.resx`).
*   **Flexible Configuration:** Configure languages, `.resx` file paths (using glob patterns), and LLM provider details via `Bit.ResxTranslator.json`.
*   **.NET Configuration:** Supports API keys via JSON or standard environment variables (e.g., `OpenAI__ApiKey`, `AzureOpenAI__ApiKey`).
*   **Easy Installation:** Installs as a .NET global tool.

## Installation

```bash
dotnet tool install --global Bit.ResxTranslator
```

## Configuration

**Bit.ResxTranslator** requires a configuration file named `Bit.ResxTranslator.json` located in the directory where you execute the `bit-resx-translate` command.

This JSON file defines the source and target languages, the location of your resource files, and the connection details for the LLM service (OpenAI or Azure OpenAI).

**Example `Bit.RexTranslator.json`:**

```jsonc
{
  "DefaultLanguage": "en",
  "DefaultLanguage__Comment": "Required: .NET culture's name, en, en-US for example.",

  "AdditionalLanguages": [ "nl", "fa", "sv", "hi", "zh", "es", "fr", "ar", "de" ],
  "AdditionalLanguages__Comment": "Required: An array of .NET culture names.",

  "ResxPaths": [ "src/**/*.resx" ],
  "RextPaths__Comment": "Required: An array of glob patterns to find your *base* .resx files",

  "OpenAI": {
    "Model": "gpt-4.1-mini",
    "Endpoint": "https://models.inference.ai.azure.com",
    "Endpoint__Comment": "Required if using OpenAI: API Key. Can be set here OR via Environment Variable [OpenAI__ApiKey]",
    "ApiKey": null
  },

  "AzureOpenAI": {
    "Model": "gpt-4.1-mini",
    "Endpoint": "https://yourResourceName.openai.azure.com/openai/deployments/yourDeployment",
    "Endpoint__Comment": "Required if using Azure: API Key. Can be set here OR via Environment Variable [AzureOpenAI__ApiKey]",
    "ApiKey": null,
    "ApiKey__Comment": "Required if using Azure: API Key. Can be set here OR via Environment Variable [AzureOpenAI__ApiKey]"
  }
}
```

***Security Note:*** It is highly recommended to provide your ApiKey using environment variables (`OpenAI__ApiKey` or `AzureOpenAI__ApiKey`) instead of hardcoding it directly in the `Bit.RexsTranslator.json` file,
especially if this file is checked into version control. The tool uses standard .NET configuration practices, meaning environment variables will override values present in the JSON file.

## Usage

```bash
bit-resx-translate
```