# Keycrate SDK - C# Examples

Simple and full examples for the Keycrate license authentication SDK in C#.

| all examples : | [python](https://github.com/keycrate/keycrate-python-example) | [Node.js](https://github.com/keycrate/keycrate-nodejs-example) | [rust](https://github.com/keycrate/keycrate-rust-example) | [c#](https://github.com/keycrate/keycrate-cs-example) | [java](https://github.com/keycrate/keycrate-java-example) | [go](https://github.com/keycrate/keycrate-go-example) | [browser](https://github.com/keycrate/keycrate-browser-javascript-example) |
| -------------- | ------------------------------------------------------------- | -------------------------------------------------------------- | --------------------------------------------------------- | ----------------------------------------------------- | --------------------------------------------------------- | ----------------------------------------------------- | -------------------------------------------------------------------------- |

## Prerequisites

-   .NET SDK 6.0 or higher

## Setup

Install the Keycrate package:

```bash
dotnet add package Keycrate
```

Or restore all dependencies:

```bash
dotnet restore
```

## Running

```bash
dotnet run
```

## Examples

-   **Simple Example** - Basic authentication with license key or username/password, plus registration
-   **Full Example** - Includes HWID detection, detailed error handling, and a post-login menu

## Configuration

Update the app ID in `Program.cs`:

```csharp
var client = new KeycrateClient(
    host: "https://api.keycrate.dev",
    appId: "YOUR_APP_ID"
);
```

## Dependencies

-   **Keycrate** - Keycrate SDK
