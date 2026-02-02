# OptiGraphExtensions

An Optimizely CMS 13 AddOn that provides comprehensive management of synonyms, pinned results, webhooks, saved queries, request logs, and custom data sources within Optimizely Graph. This package enables content editors and administrators to enhance search experiences through intelligent synonym mapping, result pinning capabilities, webhook event management, GraphQL query building, API monitoring, and external data integration.

> ⚠️ **Pre-release Notice**: This version targets Optimizely CMS 13 (pre-release) and .NET 10. For CMS 12 support, please use an earlier version of this package.

[![NuGet Version](https://img.shields.io/nuget/v/OptiGraphExtensions.svg)](https://www.nuget.org/packages/OptiGraphExtensions/)
[![.NET 10](https://img.shields.io/badge/.NET-10-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Optimizely CMS 13](https://img.shields.io/badge/Optimizely%20CMS-13%20(pre--release)-blue.svg)](https://docs.developers.optimizely.com/content-management-system/)

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

### 🔔 Webhook Management
- Create, edit, and delete webhooks registered with Optimizely Graph
- **Topic Selection**: Subscribe to specific events (doc.created, doc.updated, doc.deleted, bulk.*, etc.)
- **Wildcard Support**: Use patterns like `*.*` for all events or `doc.*` for all document events
- **Filter Configuration**: Add filters to narrow webhook triggers (e.g., status equals Published)
- **Status Control**: Enable/disable webhooks without deleting them
- **HTTP Method Selection**: Configure webhook HTTP method (POST, GET, PUT, PATCH, DELETE)
- **Help Tooltips**: Detailed descriptions for topics and filter examples
- Real-time synchronization with Optimizely Graph API
- **Status Filter**: Filter webhook list by active/disabled status

### 📊 Query Library
- Build, save, and execute GraphQL queries directly against Optimizely Graph
- **Visual Query Builder**: Intuitive interface for building queries without writing GraphQL
  - Content type selection from your Graph schema
  - Field picker with checkbox interface
  - Filter configuration with multiple operators (equals, contains, starts with, greater than, etc.)
  - Sort options and language filtering
- **Raw GraphQL Mode**: Full-featured editor for advanced users
  - Direct GraphQL query editing
  - Query variables support in JSON format
  - Syntax validation before execution
- **Query Management**: Save and organize frequently-used queries
  - Name and describe queries for easy reference
  - Edit and refine saved queries over time
  - Run saved queries with a single click
- **CSV Export**: Export query results for further analysis
  - Export current preview results instantly
  - Full pagination support for large datasets
  - Progress indicator for long-running exports
- **Mode Switching**: Seamlessly switch between visual and raw modes with automatic query conversion

### 📋 Request Logs
- Monitor and analyze API requests between your CMS and Optimizely Graph
- **Comprehensive Log Viewing**: Detailed information for every Graph API request
  - Request details: HTTP method, path, host, timestamps
  - Response data: Status codes, success/failure indicators, duration
  - Operation context: GraphQL operation names and user information
  - Full payloads: Complete request and response bodies for debugging
- **Server-side Filtering**: Query the Graph API with specific parameters
  - Filter by request ID for tracing specific operations
  - Filter by host or path
  - Filter by success/failure status
- **Client-side Filtering**: Additional filtering after data retrieval
  - Date range selection
  - HTTP method filter (GET, POST, PUT, DELETE, etc.)
  - Free-text search across paths, operations, messages, and users
- **Export Capabilities**: Share or analyze logs offline
  - CSV export for spreadsheet analysis
  - JSON export for programmatic processing
  - Exports respect current filter selections
- **Detail View**: Click any log entry for complete information

### 📦 Custom Data Management
- Create and manage custom data sources in Optimizely Graph
- **Schema Management**: Define custom content types and properties
  - Visual schema builder with intuitive interface
  - Support for multiple content types per source
  - Property type definitions (String, Int, Float, Boolean, Date, DateTime, arrays)
  - Searchable property configuration
  - Raw JSON editor for advanced configurations
- **Data Synchronization**: Sync external data to Optimizely Graph
  - NdJSON format for efficient bulk data operations
  - Language routing support for multilingual data
  - Visual data entry with property validation
  - Raw NdJSON editor for bulk operations
- **Source Management**:
  - Create sources with 1-4 character IDs
  - Edit existing schemas (partial or full sync)
  - Delete sources with confirmation
  - View all custom data sources in your Graph instance
- **External Data Import**: Import data from REST APIs
  - Connect to any REST API endpoint
  - Authentication support: None, API Key, Basic Auth, Bearer Token
  - Field mapping from external JSON to custom data properties
  - JSON path support for navigating nested response structures
  - Schema inference from API responses
  - Preview imports before execution
  - Test connection functionality
- **Scheduled Imports**: Automate recurring data imports
  - Schedule frequencies: Hourly, Daily, Weekly, Monthly
  - Configurable time of day and day of week/month
  - Integrates with Optimizely CMS Scheduled Jobs
  - Automatic retry with exponential backoff (1, 5, 15, 30 minutes)
  - Email notifications on import failures
  - Recovery notifications when failed imports succeed
- **Execution History**: Track and audit import runs
  - Success/failure status with detailed counts
  - Items received, imported, skipped, and failed
  - Duration and error message tracking
  - Retry attempt tracking
  - Scheduled vs manual execution tracking

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

Install the NuGet package in your Optimizely CMS 13 project:

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

Add your Graph instance configuration to appsettings.json, this information can be found within PaaSPortal for a PaaS instance of Optimizely CMS 13 and within the dashboard of a SaaS instance of Optimizely CMS 13

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
│   │   ├── PinnedResults/          # Pinned results management feature
│   │   │   ├── Services/           # CRUD, sync, validation services
│   │   │   ├── Repositories/       # Data access with caching
│   │   │   └── Models/             # Request/response models
│   │   ├── Webhooks/               # Webhook management feature
│   │   │   ├── Services/           # CRUD and validation services
│   │   │   └── Models/             # Request/response models
│   │   ├── QueryLibrary/           # Query library feature
│   │   │   ├── Services/           # Query execution, CSV export, schema discovery
│   │   │   ├── Repositories/       # Saved query data access with caching
│   │   │   └── Models/             # Query models and execution results
│   │   ├── RequestLogs/            # Request logs feature
│   │   │   ├── Services/           # Log retrieval and export services
│   │   │   └── Models/             # Log models and filter options
│   │   └── CustomData/             # Custom data management feature
│   │       ├── Services/           # Schema, data sync, import, scheduling services
│   │       ├── Repositories/       # Import configuration and history data access
│   │       ├── ScheduledJobs/      # Optimizely CMS scheduled job for imports
│   │       └── Models/             # Schema, data item, and import models
│   ├── Menus/                      # CMS menu integration
│   └── Views/                      # Razor views and layouts
├── OptiGraphExtensions.Tests/       # NUnit test project
└── Sample/
    └── SampleCms/                   # Example implementation
```

## Development

### Prerequisites
- .NET 10 SDK
- SQL Server or SQL Server Express
- Optimizely CMS 13 (pre-release) development environment

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
- `tbl_OptiGraphExtensions_ImportConfigurations`: Stores external data import configurations
  - Columns: Id, Name, Description, TargetSourceId, TargetContentType, ApiUrl, HttpMethod, AuthType, AuthKeyOrUsername, AuthValueOrPassword, FieldMappingsJson, IdFieldMapping, LanguageRouting, JsonPath, CustomHeadersJson, IsActive, LastImportAt, LastImportCount, ScheduleFrequency, ScheduleIntervalValue, ScheduleTimeOfDay, ScheduleDayOfWeek, ScheduleDayOfMonth, NextScheduledRunAt, MaxRetries, ConsecutiveFailures, NextRetryAt, LastImportSuccess, LastImportError, NotificationEmail, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
- `tbl_OptiGraphExtensions_ImportExecutionHistory`: Stores import execution history
  - Columns: Id, ImportConfigurationId, ExecutedAt, Success, ItemsReceived, ItemsImported, ItemsSkipped, ItemsFailed, DurationTicks, ErrorMessage, Warnings, WasRetry, RetryAttempt, WasScheduled

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

### Webhook Management

Webhooks allow you to receive notifications when content changes occur in Optimizely Graph. The AddOn provides full webhook management capabilities:

| Feature | Description |
|---------|-------------|
| Topics | Subscribe to specific events: `doc.created`, `doc.updated`, `doc.deleted`, `bulk.*`, `*.*` |
| Filters | Narrow triggers using field/operator/value filters (e.g., `status eq Published`) |
| HTTP Methods | Configure webhook method: POST, GET, PUT, PATCH, DELETE |
| Status | Enable/disable webhooks without deletion |

**Note:** Due to a limitation in the Optimizely Graph PUT endpoint (which doesn't reliably update topics/filters), the AddOn implements updates by deleting and recreating webhooks. This means webhook IDs will change after editing.

### Query Library

The Query Library allows you to build and execute GraphQL queries directly against Optimizely Graph:

| Feature | Description |
|---------|-------------|
| Visual Builder | Build queries using dropdowns and checkboxes without writing GraphQL |
| Raw Mode | Write GraphQL queries directly with variable support |
| Schema Discovery | Automatically discovers content types and fields from your Graph schema |
| Saved Queries | Save, name, and organize frequently-used queries |
| CSV Export | Export query results with full pagination support |

**Supported Filter Operators:**
- `eq` (equals), `neq` (not equals)
- `contains`, `startsWith`
- `gt` (greater than), `lt` (less than)
- `gte` (greater or equal), `lte` (less or equal)

### Request Logs

The Request Logs feature provides visibility into API communications with Optimizely Graph:

| Feature | Description |
|---------|-------------|
| Log Retrieval | Fetches request/response logs from the Graph API |
| Filtering | Server-side and client-side filtering options |
| Export | Export to CSV or JSON formats |
| Detail View | Full request/response payloads for debugging |

**API Query Parameters:**
| Parameter | Description |
|-----------|-------------|
| `requestId` | Filter by specific request ID |
| `host` | Filter by host |
| `path` | Filter by request path |
| `success` | Filter by success status (true/false) |
| `page` | Pagination page number |

### Custom Data Management

Custom Data allows you to define schemas and sync external data to Optimizely Graph for unified search experiences:

| Feature | Description |
|---------|-------------|
| Schema Builder | Visual interface for defining content types and properties |
| Data Sync | NdJSON-based synchronization with language routing |
| Source Management | Create, edit, and delete custom data sources |
| External Import | Import data from REST APIs with field mapping |
| Scheduled Imports | Automate imports with configurable schedules |
| Execution History | Track import runs with detailed metrics |

**Schema API Endpoints:**
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/content/v3/sources` | List all data sources |
| GET | `/api/content/v3/types?id={sourceId}` | Get schema for a source |
| PUT | `/api/content/v3/types?id={sourceId}` | Create/replace schema (full sync) |
| POST | `/api/content/v3/types?id={sourceId}` | Update schema (partial sync) |
| DELETE | `/api/content/v3/sources?id={sourceId}` | Delete a source and its data |

**Data Sync API:**
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/content/v2/data?id={sourceId}` | Sync data items using NdJSON format |

**NdJSON Format:**
```json
{"index":{"_id":"unique-id","language_routing":"en"}}
{"PropertyName":"value","AnotherProperty":"value","_type":"ContentTypeName"}
```

**Supported Property Types:**
- `String`, `Int`, `Float`, `Boolean`
- `Date`, `DateTime`
- `StringArray`, `IntArray`, `FloatArray`

**External Data Import:**

Import configurations support the following authentication methods:

| Auth Type | Configuration |
|-----------|---------------|
| None | No authentication required |
| API Key | Header name + API key value |
| Basic | Username + Password |
| Bearer | Bearer token |

**Scheduled Import Options:**

| Frequency | Configuration |
|-----------|---------------|
| Hourly | Run every N hours (configurable interval) |
| Daily | Run at specific time each day |
| Weekly | Run on specific day of week at specific time |
| Monthly | Run on specific day of month at specific time |

**Retry Behavior:**
- Automatic retry with exponential backoff
- Retry delays: 1 minute, 5 minutes, 15 minutes, 30 minutes (max)
- Configurable maximum retry attempts
- Email notifications after max retries reached

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
- Query library services (execution, export, schema discovery)
- Request log services and export functionality
- Request mapping and DTOs

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add or update tests as needed
5. Ensure all tests pass
6. Submit a pull request

## Dependencies

- .NET 10
- Optimizely CMS 13 pre-release (EPiServer.CMS.UI.Core 13.x)
- Entity Framework Core 10.x with SQL Server provider
- Microsoft.Extensions.Caching.Memory for caching
- Microsoft.Extensions.Http for connection pooling
- NUnit 3.14.0 for testing
- Moq 4.20.72 for mocking in tests
- System.ComponentModel.Annotations for validation

## License

This project is licensed under the MIT License. See the LICENSE file for details.

## Support

For issues, questions, or contributions, please visit the project repository or contact the development team.

If you've found this learning centre helpful, consider buying me a coffee. It helps keep me caffeinated and creating more content!

<a href="https://buymeacoffee.com/adayinthelife" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" height="50"></a>