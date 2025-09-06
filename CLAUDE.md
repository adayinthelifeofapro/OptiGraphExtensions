# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
This is an Optimizely CMS 12 AddOn package called OptiGraphExtensions that provides management of synonyms and pinned results within Optimizely Graph. The project consists of the main library and a sample CMS implementation.

## Development Commands

### Building the Project
```bash
# Build the main solution
dotnet build src/OptiGraphExtensions.sln

# Build the sample CMS
dotnet build Sample/SampleCms.sln

# Build in Release mode for packaging
dotnet build src/OptiGraphExtensions.sln -c Release
```

### Running Tests
```bash
# Run all tests
dotnet test src/OptiGraphExtensions.Tests/OptiGraphExtensions.Tests.csproj

# Run tests with coverage
dotnet test src/OptiGraphExtensions.Tests/OptiGraphExtensions.Tests.csproj --collect:"XPlat Code Coverage"
```

### Running the Sample CMS
```bash
# Navigate to sample and run
cd Sample/SampleCms
dotnet run
```

### Package Creation
The project is configured to automatically generate NuGet packages on build (`GeneratePackageOnBuild` is set to `True`).

## Architecture

### Project Structure
- **src/OptiGraphExtensions/**: Main AddOn library
- **src/OptiGraphExtensions.Tests/**: NUnit test project  
- **Sample/SampleCms/**: Example Optimizely CMS 12 implementation demonstrating the AddOn

### Key Components

#### Configuration & Setup
- `Features/Configuration/OptiGraphExtensionsServiceExtensions.cs`: Main service registration and configuration
- `Features/Configuration/OptiGraphExtensionsSetupOptions.cs`: Configuration options
- Entry point uses `AddOptiGraphExtensions()` and `UseOptiGraphExtensions()` extension methods

#### Data Layer
- `Entities/OptiGraphExtensionsDataContext.cs`: Entity Framework DbContext
- `Entities/IOptiGraphExtensionsDataContext.cs`: Data context interface
- `Entities/Synonyms.cs`: Synonym entity model
- Uses Entity Framework migrations for database management
- SQL Server as the database provider

#### Administration Interface
- `Administration/AdministrationController.cs`: MVC controller for admin pages
- Routes: `/optimizely-graphextensions/administration/`
  - `/about` - About page
  - `/synonyms` - Synonym management
  - `/pinned-results` - Pinned results management
- Protected by authorization policy requiring CmsAdmins, Administrator, or WebAdmins roles

#### UI Components
- Uses Razor views located in `Views/OptiGraphExtensions/Administration/`
- Blazor components for interactive UI (see `Features/Synonyms/MyBlazorCounterComponentBase.cs`)
- Layout: `Views/Shared/Layouts/_LayoutBlazorAdminPage.cshtml`

#### Module Integration
- `module.config`: Optimizely module configuration
- `Menus/OptiGraphExtensionsMenuProvider.cs`: CMS menu integration
- Configured as a protected module that loads from bin

### Dependencies
- .NET 8.0 target framework
- Optimizely CMS 12 (EPiServer.CMS.UI.Core 12.23.0)
- Entity Framework Core 8.0.19 with SQL Server provider
- NUnit for testing

### NuGet Configuration
The sample project uses a custom `nuget.config` that includes the Optimizely NuGet feed:
- nuget.org (standard)
- api.nuget.optimizely.com (Optimizely packages)

### Authorization
Default authorization policy requires membership in:
- CmsAdmins
- Administrator  
- WebAdmins

### Database Connection
Default connection string name: "EPiServerDB" (configurable via setup options)