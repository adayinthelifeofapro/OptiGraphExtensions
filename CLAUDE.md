# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
This is an Optimizely CMS 12 AddOn package called OptiGraphExtensions that provides management of synonyms, pinned results, webhooks, query library, request logs, and custom data sources within Optimizely Graph. The project consists of the main library and a sample CMS implementation.

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

# Run tests in watch mode for development
dotnet watch test --project src/OptiGraphExtensions.Tests/OptiGraphExtensions.Tests.csproj
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
- `Entities/Synonyms.cs`: Synonym entity model with language and slot support
- `Entities/SynonymSlot.cs`: Enum defining synonym slots (ONE, TWO) for Optimizely Graph API
- `Entities/PinnedResult.cs`: Pinned result entity model with collection relationships
- `Entities/PinnedResultsCollection.cs`: Pinned results collection entity with Graph integration
- `Entities/ImportConfiguration.cs`: External data import configuration with scheduling
- `Entities/ImportExecutionHistory.cs`: Import execution history tracking
- `Entities/ScheduleFrequency.cs`: Enum for schedule frequencies (None, Hourly, Daily, Weekly, Monthly)
- Uses Entity Framework migrations for database management
- SQL Server as the database provider

#### Administration Interface
- `Administration/AdministrationController.cs`: MVC controller for admin pages
- Routes: `/optimizely-graphextensions/administration/`
  - `/about` - About page
  - `/synonyms` - Synonym management
  - `/pinned-results` - Pinned results management
  - `/webhooks` - Webhook management
  - `/query-library` - Query library management
  - `/request-logs` - Request logs viewer
  - `/custom-data` - Custom data source management
- Protected by authorization policy requiring CmsAdmins, Administrator, or WebAdmins roles

#### UI Components
- Uses Razor views located in `Views/OptiGraphExtensions/Administration/`
- Blazor components for interactive UI:
  - `Features/Synonyms/SynonymManagementComponentBase.cs`: Synonym management component
  - `Features/PinnedResults/PinnedResultsManagementComponentBase.cs`: Pinned results management component
  - `Features/Webhooks/WebhookManagementComponentBase.cs`: Webhook management component
  - `Features/QueryLibrary/QueryLibraryManagementComponentBase.cs`: Query library component
  - `Features/RequestLogs/RequestLogsManagementComponentBase.cs`: Request logs component
  - `Features/CustomData/CustomDataManagementComponentBase.cs`: Custom data management component
- Layout: `Views/Shared/Layouts/_LayoutBlazorAdminPage.cshtml`

#### Module Integration
- `module.config`: Optimizely module configuration
- `Menus/OptiGraphExtensionsMenuProvider.cs`: CMS menu integration
- Configured as a protected module that loads from bin

#### Services & Repositories

##### Common Services (Clean Code Refactored)
- `Features/Common/Services/`: Shared services following SOLID principles
  - `IComponentErrorHandler` / `ComponentErrorHandler`: Centralized error handling for components
  - `IGraphConfigurationValidator` / `GraphConfigurationValidator`: Shared Graph configuration validation
  - `IRequestMapper<TModel, TCreateRequest, TUpdateRequest>`: Generic request mapping interface
- `Features/Common/Components/ManagementComponentBase<TEntity, TModel>`: Base class for management components
- `Features/Common/Validation/`: Centralized validation framework with data annotations support
- `Features/Common/Exceptions/ComponentException`: Custom exception type for component operations
- `Features/Common/Caching/`: Intelligent caching infrastructure
  - `ICacheService` / `MemoryCacheService`: In-memory caching with configurable expiration
  - `ICacheInvalidationService` / `CacheInvalidationService`: Cache invalidation management
  - `CacheKeyBuilder`: Consistent cache key generation

##### Synonyms Feature (SOLID Principles Applied)
- **Services** (decomposed from large monolithic services):
  - `ISynonymCrudService` / `SynonymCrudService`: Focused CRUD operations with HttpClient connection pooling
  - `ISynonymGraphSyncService` / `SynonymGraphSyncService`: Dedicated Graph synchronization with IHttpClientFactory, supports language routing and synonym slots
  - `ISynonymApiService` / `SynonymApiService`: Facade pattern coordinating CRUD and sync services
  - `ISynonymValidationService` / `SynonymValidationService`: Business validation logic
  - `SynonymRequestMapper`: Maps between SynonymModel and API request DTOs (includes slot mapping)
- **Repositories**:
  - `ISynonymRepository` / `SynonymRepository`: Data access layer
  - `CachedSynonymRepository`: Decorator pattern adding caching layer with automatic invalidation
- **Components**:
  - `SynonymManagementComponentBase`: Includes language filter, slot selection, and pagination support
- **Synonym Slot Support**:
  - Synonyms can be assigned to Slot ONE or Slot TWO (maps to Optimizely Graph's `synonym_slot` API parameter)
  - UI dropdown for selecting slot when creating/editing synonyms
  - Slot filter dropdown to view synonyms by specific slot
  - Graph synchronization groups synonyms by both language and slot

##### Content Search Feature (Autocomplete for Pinned Results)
- **Services**:
  - `IContentSearchService` / `ContentSearchService`: Searches Optimizely Graph for content items
  - Uses GraphQL fulltext search with `_fulltext: { match: ... }` query
  - Supports content type filtering and language-specific searches
- **API Controller**:
  - `ContentSearchApiController`: REST API for content search
  - `GET /api/optimizely-graphextensions/content-search?q=&contentType=&language=&limit=`
  - `GET /api/optimizely-graphextensions/content-search/content-types`
- **Models**:
  - `ContentSearchResult`: Search result with GuidValue, Name, Url, ContentType, Language
  - `ContentSearchGraphModels.cs`: GraphQL request/response models
- **UI Integration**:
  - Autocomplete input replaces manual GUID entry in pinned results
  - 300ms debounce timer for search input
  - Dropdown displays Name + URL + ContentType badge
  - Content type filter dropdown for narrowing results
  - Maximum 10 results returned per search

##### Pinned Results Feature (Clean Architecture)
- **Services** (decomposed following Single Responsibility Principle):
  - `IPinnedResultsCrudService` / `PinnedResultsCrudService`: Focused CRUD operations with HttpClient pooling
  - `IPinnedResultsCollectionCrudService` / `PinnedResultsCollectionCrudService`: Collection CRUD with delete support
  - `IPinnedResultsGraphSyncService` / `PinnedResultsGraphSyncService`: Graph sync with IHttpClientFactory, preserves TargetName during sync
  - `IPinnedResultsApiService` / `PinnedResultsApiService`: Facade coordinating specialized services
  - `IPinnedResultsValidationService` / `PinnedResultsValidationService`: Business validation
  - `PinnedResultRequestMapper` / `PinnedResultsCollectionRequestMapper`: Request mapping services
- **Repositories**:
  - `IPinnedResultRepository` / `PinnedResultRepository`: Pinned results data access
  - `CachedPinnedResultRepository`: Caching decorator with automatic cache invalidation
  - `IPinnedResultsCollectionRepository` / `PinnedResultsCollectionRepository`: Collections data access
  - `CachedPinnedResultsCollectionRepository`: Collection caching with invalidation support
- **Components**:
  - `PinnedResultsManagementComponentBase`: Includes content search autocomplete, language filter dropdown, collection ID display, and pagination support
- **Pinned Results Features**:
  - **Content Search Autocomplete**: Search for content items instead of manually entering GUIDs
  - **TargetName Display**: Shows human-readable content names in the table instead of GUIDs
  - Language-specific pinning support with filter dropdown
  - Collection ID displayed in collections table for easy reference
  - Cascade delete for collections with associated pinned items
  - Bidirectional sync with Optimizely Graph collections (preserves TargetName values)

##### Webhooks Feature (Optimizely Graph API Integration)
- **Services**:
  - `IWebhookService` / `WebhookService`: Full CRUD operations against Optimizely Graph Webhook API
  - `IWebhookValidationService` / `WebhookValidationService`: URL and HTTP method validation
- **Models**:
  - `WebhookModel`: Main webhook model with URL, method, topics, filters, and disabled status
  - `WebhookFilter`: Filter model with field, operator, and value
  - `CreateWebhookRequest` / `UpdateWebhookRequest`: Request DTOs for webhook operations
  - `WebhookResponse` / `WebhookRequest`: API response models for JSON deserialization
- **Components**:
  - `WebhookManagementComponentBase`: Includes topic selection, filter configuration, status filtering, and help tooltips
  - `WebhookManagementComponent.razor`: Blazor UI with create/edit forms, webhook table, and pagination
- **Webhook Features**:
  - **Topic Selection**: Subscribe to specific events (doc.created, doc.updated, doc.deleted, bulk.*, etc.)
  - **Wildcard Support**: Use `*.*` for all events, `doc.*` for all document events, etc.
  - **Filter Configuration**: Add field/operator/value filters (e.g., Status eq Published)
  - **Help Tooltips**: Detailed topic descriptions and filter examples
  - **Status Filter**: Filter webhook list by active/disabled status
  - **HTTP Method Selection**: POST, GET, PUT, PATCH, DELETE
- **API Limitation Workaround**:
  - The Optimizely Graph PUT endpoint doesn't reliably update topics/filters
  - Updates are implemented via delete-and-recreate pattern
  - Webhook IDs change after editing (new webhook created)

##### Custom Data Feature (External Data Integration)
- **Services**:
  - `ICustomDataSchemaService` / `CustomDataSchemaService`: Schema CRUD operations against Optimizely Graph
    - Create, read, update, delete custom data source schemas
    - Full sync (PUT) replaces schema and data
    - Partial sync (POST) preserves existing data
  - `ICustomDataService` / `CustomDataService`: Data synchronization and retrieval
    - NdJSON-based data sync to Graph
    - GraphQL queries to retrieve synced data
  - `ICustomDataValidationService` / `CustomDataValidationService`: Schema and data validation
    - Source ID validation (1-4 lowercase alphanumeric characters)
    - Content type and property validation
    - Data item validation against schema
  - `INdJsonBuilderService` / `NdJsonBuilderService`: NdJSON format handling
    - Build NdJSON payloads for data sync
    - Parse NdJSON for import operations
  - `ISchemaParserService` / `SchemaParserService`: JSON schema conversion
    - Convert between API format and internal models
    - Display JSON generation for UI
  - `IExternalDataImportService` / `ExternalDataImportService`: External API data import
    - Test connection to external APIs
    - Fetch and preview external data
    - Map external JSON fields to custom data properties
    - Execute full import with sync to Graph
  - `IApiSchemaInferenceService` / `ApiSchemaInferenceService`: Schema inference from APIs
    - Infer content type schemas from external API responses
    - Support for JSON path navigation in nested responses
    - Sample data extraction for preview
  - `IScheduledImportService` / `ScheduledImportService`: Scheduled import management
    - Calculate next scheduled run times
    - Exponential backoff retry logic (1, 5, 15, 30 min)
    - Record execution history
    - Update configuration after execution
  - `IImportNotificationService` / `ImportNotificationService`: Import notifications
    - Email notifications on import failures
    - Recovery notifications when previously failing imports succeed
- **Repositories**:
  - `IImportConfigurationRepository` / `ImportConfigurationRepository`: Import configuration data access
  - `IImportExecutionHistoryRepository` / `ImportExecutionHistoryRepository`: Execution history data access
- **Models**:
  - `CustomDataSourceModel`: Source with content types, properties, languages
  - `CustomDataItemModel`: Data item with properties and language routing
  - `ContentTypeSchemaModel`: Content type definition with properties
  - `PropertyTypeModel`: Property definition (name, type, searchable, index)
  - `CreateSchemaRequest` / `UpdateSchemaRequest`: Schema operation DTOs
  - `SyncDataRequest`: Data sync request with items and job ID
  - `GraphSchemaResponse`: API response model for schema retrieval
  - `ImportConfigurationModel`: Import configuration with API settings and field mappings
  - `ImportResult`: Import execution result with counts and errors
  - `FieldMapping`: Maps external field paths to custom data properties
- **Entities**:
  - `ImportConfiguration`: Saved import configuration entity
    - API URL, HTTP method, authentication settings
    - Field mappings and JSON path configuration
    - Scheduling settings (frequency, time, day)
    - Retry configuration and failure tracking
    - Notification email settings
  - `ImportExecutionHistory`: Execution history records
    - Success/failure status
    - Items received, imported, skipped, failed counts
    - Duration, error messages, warnings
    - Retry attempt tracking
  - `ScheduleFrequency`: Enum (None, Hourly, Daily, Weekly, Monthly)
- **Scheduled Job**:
  - `ExternalDataImportScheduledJob`: Optimizely CMS scheduled job
    - Processes all due import configurations
    - Integrates with Optimizely CMS scheduled jobs system
    - Supports stopping mid-execution
    - Logs execution details for monitoring
- **Components**:
  - `CustomDataManagementComponentBase`: Main component with schema, data, and import management
  - Dual-mode UI: Visual builder and Raw JSON/NdJSON editor
  - Import configuration UI with field mapping
- **Custom Data Features**:
  - **Schema Management**: Create/edit content types with properties
  - **Property Types**: String, Int, Float, Boolean, Date, DateTime, arrays
  - **Language Support**: Multi-language data with language routing
  - **Visual Builder**: Intuitive interface for schema and data entry
  - **Raw Mode**: JSON/NdJSON editors for advanced users
  - **External Data Import**: Import data from REST APIs
    - Authentication: None, API Key, Basic Auth, Bearer Token
    - Field mapping with JSON path support
    - Schema inference from API responses
    - Preview imports before execution
  - **Scheduled Imports**: Automate data imports
    - Schedule frequencies: Hourly, Daily, Weekly, Monthly
    - Configurable time of day and day of week/month
    - Automatic retry with exponential backoff
    - Email notifications on failures
  - **Execution History**: Track import runs
    - Success/failure status with item counts
    - Duration and error details
    - Retry attempt tracking
- **API Endpoints Used**:
  - `GET /api/content/v3/sources`: List all data sources
  - `GET /api/content/v3/types?id={sourceId}`: Get schema
  - `PUT /api/content/v3/types?id={sourceId}`: Full schema sync
  - `POST /api/content/v3/types?id={sourceId}`: Partial schema sync
  - `DELETE /api/content/v3/sources?id={sourceId}`: Delete source
  - `POST /api/content/v2/data?id={sourceId}`: Sync data (NdJSON)
  - GraphQL endpoint for querying synced data

##### Architecture Improvements Applied
- **SOLID Principles**: Services follow Single Responsibility, Interface Segregation, and Dependency Inversion
- **DRY (Don't Repeat Yourself)**: Eliminated duplicate validation, error handling, and mapping code
- **Clean Code**: Long methods extracted into focused helper methods with clear names
- **Separation of Concerns**: Clear boundaries between CRUD, validation, synchronization, and UI logic
- **Comprehensive Unit Testing**: 145+ unit tests covering all refactored services with 100% pass rate

#### API Controllers
- `Features/Synonyms/SynonymsApiController.cs`: RESTful API for synonym management
  - Endpoints: GET, POST, PUT, DELETE operations for synonyms
  - Integrated with caching and validation services
- `Features/PinnedResults/PinnedResultsApiController.cs`: RESTful API for pinned results management
  - Full CRUD operations for individual pinned results
  - Collection association management
- `Features/PinnedResults/PinnedResultsCollectionsApiController.cs`: RESTful API for collections management
  - Collection CRUD operations with Graph synchronization
  - Cascade delete support for collection and associated results
- `Features/ContentSearch/ContentSearchApiController.cs`: RESTful API for content search
  - `GET /api/optimizely-graphextensions/content-search`: Search content by text with optional filters
  - `GET /api/optimizely-graphextensions/content-search/content-types`: Get available content types

#### Optimizely Graph Integration
- Synchronization functionality to keep local data in sync with Optimizely Graph
- Graph collection management with automatic sync capabilities
- API services for bi-directional data synchronization
- Connection pooling via IHttpClientFactory for efficient HTTP connections
- Support for collection deletion from both local database and Graph
- **Synonym API Parameters**:
  - `language_routing`: Groups synonyms by language for localized search experiences
  - `synonym_slot`: Assigns synonyms to Slot ONE or TWO for different synonym sets
  - API URL format: `{gatewayUrl}/resources/synonyms?language_routing={language}&synonym_slot={ONE|TWO}`
- **Webhook API Integration**:
  - Full CRUD operations against Optimizely Graph Webhook API
  - API URL format: `{gatewayUrl}/api/webhooks` (GET, POST) and `{gatewayUrl}/api/webhooks/{id}` (DELETE)
  - Basic authentication with Base64 encoded AppKey:Secret
  - Topic subscription for content events (doc.*, bulk.*, etc.)
  - Filter support for conditional webhook triggering
- **Custom Data API Integration**:
  - Schema management via `/api/content/v3/types` and `/api/content/v3/sources`
  - Data sync via `/api/content/v2/data` using NdJSON format
  - GraphQL queries for retrieving synced data
  - Source-specific locale types (e.g., `test_Locales` for source ID "test")
  - Basic authentication with Base64 encoded AppKey:Secret
  - NdJSON format: action line + data line pairs

### Dependencies
- .NET 8.0 target framework
- Optimizely CMS 12 (EPiServer.CMS.UI.Core 12.23.0)
- Entity Framework Core 8.0.19 with SQL Server provider
- NUnit for testing with Moq framework for mocking
- System.ComponentModel.Annotations for validation attributes

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

## Recent Improvements (2025)

### Service Architecture Refactoring
The project has undergone comprehensive clean code refactoring following SOLID principles:

#### Before Refactoring Issues
- Large monolithic service classes (PinnedResultsApiService: 392 lines, SynonymApiService: 166 lines)
- Duplicate validation logic across multiple services
- Long component methods (604 lines in PinnedResultsManagementComponentBase)
- Violations of Single Responsibility Principle
- Repeated error handling patterns
- No caching strategy
- Individual HTTP client instances per request

#### After Refactoring Improvements
- **Service Decomposition**: Large services split into focused services following Single Responsibility Principle
- **Centralized Validation**: Common validation framework with data annotations
- **Error Handling**: Centralized ComponentErrorHandler eliminates duplicate try-catch blocks
- **Request Mapping**: Generic request mapper pattern reduces repetitive DTO creation code
- **Component Refactoring**: Base class inheritance and method extraction significantly reduces component complexity
- **Intelligent Caching**: Repository decorator pattern with automatic cache invalidation
- **Connection Pooling**: IHttpClientFactory for efficient HTTP connection management
- **Collection Management**: Full CRUD operations including Graph collection deletion

#### Key Metrics
- `SynonymManagementComponentBase`: Reduced from 287 to 178 lines (38% reduction)
- `PinnedResultsManagementComponentBase`: Reduced from 604 to 359 lines (41% reduction)
- Created 15+ new focused service classes following SOLID principles
- 145+ comprehensive unit tests with 100% pass rate covering:
  - Repository operations (CRUD, caching, invalidation)
  - Service layer (validation, mapping, error handling)
  - API controllers (HTTP operations, status codes)
  - Graph synchronization (connection pooling, retry logic)
  - Content search service and API controller
- Eliminated 8+ instances of duplicate error handling code
- Added intelligent caching layer with automatic invalidation
- Implemented connection pooling for all HTTP operations via IHttpClientFactory

#### Service Registration
All new services are automatically registered via `OptiGraphExtensionsServiceExtensions.cs`:
- Common services (error handling, validation, request mapping, caching)
- Decomposed CRUD services with connection pooling
- Graph synchronization services with IHttpClientFactory
- Content search service for autocomplete functionality
- Webhook services for Optimizely Graph webhook management
- Query library services for GraphQL query building and execution
- Request log services for API monitoring
- Custom data services for schema and data management
- External data import services (import, scheduling, notifications)
- Import configuration and execution history repositories
- Component base classes
- Cached repository decorators

This refactoring maintains all existing functionality while significantly improving maintainability, testability, performance, and adherence to clean code principles.

## Testing Strategy

### Unit Test Coverage
The project includes comprehensive unit tests using NUnit and Moq:

- **Repository Tests**: Mock DbContext operations, verify CRUD operations, test caching behavior
- **Service Tests**: Validate business logic, error handling, request mapping
- **API Controller Tests**: HTTP response codes, parameter validation, service integration
- **Caching Tests**: Cache hit/miss scenarios, invalidation triggers, expiration policies
- **Validation Tests**: Data annotation validation, business rule enforcement

### Running Specific Test Categories
```bash
# Run only repository tests
dotnet test --filter "FullyQualifiedName~Repository"

# Run only service tests
dotnet test --filter "FullyQualifiedName~Service"

# Run only API controller tests
dotnet test --filter "FullyQualifiedName~Controller"
```

## Performance Optimizations

### Connection Pooling
- All HTTP operations use IHttpClientFactory for efficient connection reuse
- Reduces socket exhaustion and improves response times
- Configured per-service with appropriate timeouts and retry policies

### Caching Strategy
- In-memory caching with configurable expiration (default: 5 minutes)
- Automatic cache invalidation on data mutations
- Cache key generation using consistent naming patterns
- Repository decorator pattern for transparent caching

### Database Optimization
- Efficient queries using Entity Framework Core
- Proper indexing on frequently queried columns
- Async/await patterns throughout for non-blocking I/O