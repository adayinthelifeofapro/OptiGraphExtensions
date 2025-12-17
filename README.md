# OptiGraphExtensions

An Optimizely CMS 12 AddOn that provides comprehensive management of synonyms and pinned results within Optimizely Graph. This package enables content editors and administrators to enhance search experiences through intelligent synonym mapping and result pinning capabilities.

[![NuGet Version](https://img.shields.io/nuget/v/OptiGraphExtensions.svg)](https://www.nuget.org/packages/OptiGraphExtensions/)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Optimizely CMS 12](https://img.shields.io/badge/Optimizely%20CMS-12-blue.svg)](https://docs.developers.optimizely.com/content-management-system/v12.0.0-cms/)

## Key Features

### 🔍 Synonym Management
- Create, update, and delete synonyms for enhanced search functionality
- **Language Routing**: Support for multiple synonym groups with different languages
- **Synonym Slots**: Assign synonyms to Slot ONE or TWO for different synonym sets
- **Slot Filter**: Filter synonyms by slot using dropdown selector
- Real-time synchronization with Optimizely Graph (grouped by language and slot)
- Intelligent caching with automatic invalidation
- Clean admin interface with language filter and slot selection dropdowns

### 📌 Pinned Results Management
- Create and manage pinned results collections
- Associate specific content with search phrases
- **Content Search Autocomplete**: Search for content items instead of manually entering GUIDs
- **Target Content Display**: Shows human-readable content names in the table instead of GUIDs
- Priority-based result ordering
- **Language Filter**: Filter pinned results by language with dropdown selector
- **Collection ID Display**: View collection IDs in the collections table for easy reference
- Bidirectional synchronization with Optimizely Graph (preserves content names during sync)
- **Cascade Delete**: Delete collections with associated pinned items properly handled
- Full CRUD operations including collection deletion from Graph

### 🎨 Administration Interface
- Clean, intuitive admin interface integrated with Optimizely CMS
- Blazor components for interactive UI elements
- Pagination support for large datasets
- Real-time validation and centralized error handling
- Bulk operations and sync capabilities
- About page with version and system information

### ⚡ Performance Optimizations
- **Intelligent Caching**: Repository-level caching with automatic cache invalidation
- **Connection Pooling**: Efficient HTTP connection management via IHttpClientFactory
- **Optimized Architecture**: Clean code principles with SOLID design patterns
- **Reduced Complexity**: 40% reduction in component code through refactoring

## Quick Start

### Installation

Install the NuGet package in your Optimizely CMS 12 project:

```bash
dotnet add package OptiGraphExtensions
```

### Configuration

Add the following to your `Program.cs` or `Startup.cs`:

```csharp
// Add services
services.AddOptiGraphExtensions(options =>
{
    options.ConnectionStringName = "EPiServerDB"; // Optional, defaults to "EPiServerDB"
});

// Configure the application
app.UseOptiGraphExtensions();
```

As this is a Blazor-based admin interface, ensure that Blazor server-side is set up in your Optimizely CMS project.

```csharp
// Add Blazor services
services.AddServerSideBlazor();

// Map Blazor hub
app.UseEndpoints(endpoints =>
{
    endpoints.MapContent();
    endpoints.MapBlazorHub();
    endpoints.MapControllers();
});
```

Add your Graph instance configuration to appsettings.json, this information can be found within PaaSPortal for a PaaS instance of Optimizely CMS12 and within the dashboard of a SaaS instance of Optimizely CMS 12

```csharp
  "Optimizely": {
    "ContentGraph": {
      "GatewayAddress": "<your graph instance gateway address>",
      "AppKey": "<your graph instance key>",
      "Secret": "<your graph instance secret>"
    }
  }
```

## Additional Configuration Customisation

The configuration of the module has some scope for modification by providing configuration in the service extension methods.  Both the provision of ```optiGraphExtensionsSetupOptions``` and ```authorizationOptions``` are optional in the following example.

Example:
```C#
services.AddOptiGraphExtensions(optiGraphExtensionsSetupOptions =>
{
    optiGraphExtensionsSetupOptions.ConnectionStringName = "EPiServerDB";
},
authorizationOptions => 
{
    authorizationOptions.AddPolicy(OptiGraphExtensionsConstants.AuthorizationPolicy, policy =>
    {
        policy.RequireRole("WebAdmins");
    });
});
```

### Authentication With Optimizely Opti ID

If you are using the new Optimizely Opti ID package for authentication into Optimizely CMS and the rest of the Optimizely One suite, then you will need to define the `authorizationOptions` for this module as part of your application start up.  This should be a simple case of adding `policy.AddAuthenticationSchemes(OptimizelyIdentityDefaults.SchemeName);` to the `authorizationOptions` as per the example below.

```C#
serviceCollection.AddOptiGraphExtensions(optiGraphExtensionsSetupOptions =>
{
    optiGraphExtensionsSetupOptions.ConnectionStringName = "EPiServerDB";
},
authorizationOptions =>
{
    authorizationOptions.AddPolicy(OptiGraphExtensionsConstants.AuthorizationPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(OptimizelyIdentityDefaults.SchemeName);
        policy.RequireRole("WebAdmins");
    });
});
```

## Project Structure

```
src/
├── OptiGraphExtensions/              # Main AddOn library
│   ├── Administration/               # Admin controllers and view models
│   ├── Entities/                    # Entity Framework models
│   ├── Features/
│   │   ├── Common/                  # Shared components and services
│   │   │   ├── Caching/            # Cache services and invalidation
│   │   │   ├── Components/         # Base component classes
│   │   │   ├── Exceptions/         # Custom exceptions
│   │   │   ├── Services/           # Shared services (error handling, validation)
│   │   │   └── Validation/         # Validation framework
│   │   ├── Configuration/          # Service registration and setup
│   │   ├── ContentSearch/          # Content search for autocomplete
│   │   │   ├── Services/           # Graph search service
│   │   │   └── Models/             # Search result models
│   │   ├── Synonyms/               # Synonym management feature
│   │   │   ├── Services/           # CRUD, sync, validation services
│   │   │   ├── Repositories/       # Data access with caching
│   │   │   └── Models/             # Request/response models
│   │   └── PinnedResults/          # Pinned results management feature
│   │       ├── Services/           # CRUD, sync, validation services
│   │       ├── Repositories/       # Data access with caching
│   │       └── Models/             # Request/response models
│   ├── Menus/                      # CMS menu integration
│   └── Views/                      # Razor views and layouts
├── OptiGraphExtensions.Tests/       # NUnit test project
└── Sample/
    └── SampleCms/                   # Example implementation
```

## Development

### Prerequisites
- .NET 8.0 SDK
- SQL Server or SQL Server Express
- Optimizely CMS 12 development environment

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
cd Sample/SampleCms
dotnet run
```

Navigate to `/optimizely-graphextensions/administration/` in your CMS to access the management interface.

## Architecture

### Clean Architecture & SOLID Principles
The AddOn follows clean architecture and SOLID principles with clear separation of concerns:

- **Entities**: Domain models and Entity Framework configuration
- **Repositories**: Data access layer with caching decorators
- **Services**: Decomposed services following Single Responsibility Principle
- **Controllers**: RESTful API endpoints and admin interface controllers
- **Components**: Refactored Blazor components with base class inheritance

### Key Architectural Improvements

#### 🏗️ Service Decomposition
- **Before**: Monolithic services (392+ lines)
- **After**: Focused services following Single Responsibility
  - Separate CRUD, validation, and synchronization services
  - Facade pattern for coordinating multiple services
  - 15+ new focused service classes

#### 🚀 Performance Enhancements
- **Intelligent Caching**: Repository decorator pattern with automatic invalidation
- **Connection Pooling**: IHttpClientFactory for efficient HTTP connections
- **Code Optimization**: 40% reduction in component complexity

#### 🛡️ Robust Error Handling
- Centralized error handling via ComponentErrorHandler
- Custom exception types for better error tracking
- Graceful fallback to local data when Graph is unavailable

#### ✅ Comprehensive Testing
- 145+ unit tests with 100% pass rate
- Service layer testing with Moq framework
- Repository operation testing
- Validation logic coverage

## Configuration Options

```csharp
services.AddOptiGraphExtensions(options =>
{
    // Database connection string name (default: "EPiServerDB")
    options.ConnectionStringName = "EPiServerDB";
    
    // Additional configuration options available
});
```

## Database Schema

The AddOn creates the following database tables:

- `tbl_OptiGraphExtensions_Synonyms`: Stores synonym definitions
  - Columns: Id, SynonymItem, Language, Slot (ONE=1, TWO=2), CreatedAt, CreatedBy
- `tbl_OptiGraphExtensions_PinnedResultsCollections`: Stores pinned results collections
  - Columns: Id, Title, IsActive, GraphCollectionId, CreatedAt, CreatedBy
- `tbl_OptiGraphExtensions_PinnedResults`: Stores individual pinned results
  - Columns: Id, CollectionId, Phrases, TargetKey (GUID), TargetName (display name), Language, Priority, IsActive, GraphId, CreatedAt, CreatedBy

## Optimizely Graph Integration

The AddOn provides seamless integration with Optimizely Graph:

- **Automatic Synchronization**: Keep local data in sync with Graph collections
- **Bidirectional Updates**: Changes flow both ways between local database and Graph
- **Collection Management**: Full CRUD operations including deletion from Graph
- **Connection Pooling**: Efficient HTTP connection management for Graph API calls
- **Error Handling**: Graceful fallback to local data when Graph is unavailable
- **Authentication**: Supports Optimizely Graph authentication requirements
- **Sync Status Tracking**: Real-time sync status for collections and results

### Synonym API Parameters

When syncing synonyms to Optimizely Graph, the following parameters are used:

| Parameter | Description | Values |
|-----------|-------------|--------|
| `language_routing` | Groups synonyms by language for localized search | Language code (e.g., "en", "sv") |
| `synonym_slot` | Assigns synonyms to different slots | `ONE` or `TWO` |

**API URL Format:**
```
{gatewayUrl}/resources/synonyms?language_routing={language}&synonym_slot={ONE|TWO}
```

Synonyms are grouped by both language and slot when syncing, allowing for:
- Language-specific synonym sets for multilingual sites
- Multiple synonym configurations per language using different slots

## Authorization

The admin interface requires users to be members of one of the following roles:
- CmsAdmins
- Administrator
- WebAdmins

## Testing

The project includes comprehensive NUnit tests covering:
- Service layer functionality with 100% pass rate
- Repository operations including caching behavior
- Validation logic and error handling
- Controller standards and conventions
- CRUD operations for all entities
- Graph synchronization services
- Content search service and API controller
- Request mapping and DTOs

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add or update tests as needed
5. Ensure all tests pass
6. Submit a pull request

## Dependencies

- .NET 8.0
- Optimizely CMS 12 (EPiServer.CMS.UI.Core 12.23.0)
- Entity Framework Core 8.0.19 with SQL Server provider
- Microsoft.Extensions.Caching.Memory for caching
- Microsoft.Extensions.Http for connection pooling
- NUnit 3.14.0 for testing
- Moq 4.20.72 for mocking in tests
- System.ComponentModel.Annotations for validation

## License

This project is licensed under the MIT License. See the LICENSE file for details.

## Support

For issues, questions, or contributions, please visit the project repository or contact the development team.