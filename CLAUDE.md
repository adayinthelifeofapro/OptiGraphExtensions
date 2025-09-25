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
- `Entities/Synonyms.cs`: Synonym entity model
- `Entities/PinnedResult.cs`: Pinned result entity model with collection relationships
- `Entities/PinnedResultsCollection.cs`: Pinned results collection entity with Graph integration
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
- Blazor components for interactive UI:
  - `Features/Synonyms/SynonymManagementComponentBase.cs`: Synonym management component
  - `Features/PinnedResults/PinnedResultsManagementComponentBase.cs`: Pinned results management component
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
  - `ISynonymGraphSyncService` / `SynonymGraphSyncService`: Dedicated Graph synchronization with IHttpClientFactory
  - `ISynonymApiService` / `SynonymApiService`: Facade pattern coordinating CRUD and sync services
  - `ISynonymValidationService` / `SynonymValidationService`: Business validation logic
  - `SynonymRequestMapper`: Maps between SynonymModel and API request DTOs
- **Repositories**:
  - `ISynonymRepository` / `SynonymRepository`: Data access layer
  - `CachedSynonymRepository`: Decorator pattern adding caching layer with automatic invalidation
- **Components**:
  - `SynonymManagementComponentBase`: Refactored from 287 to 178 lines (38% reduction)

##### Pinned Results Feature (Clean Architecture)
- **Services** (decomposed following Single Responsibility Principle):
  - `IPinnedResultsCrudService` / `PinnedResultsCrudService`: Focused CRUD operations with HttpClient pooling
  - `IPinnedResultsCollectionCrudService` / `PinnedResultsCollectionCrudService`: Collection CRUD with delete support
  - `IPinnedResultsGraphSyncService` / `PinnedResultsGraphSyncService`: Graph sync with IHttpClientFactory
  - `IPinnedResultsApiService` / `PinnedResultsApiService`: Facade coordinating specialized services
  - `IPinnedResultsValidationService` / `PinnedResultsValidationService`: Business validation
  - `PinnedResultRequestMapper` / `PinnedResultsCollectionRequestMapper`: Request mapping services
- **Repositories**:
  - `IPinnedResultRepository` / `PinnedResultRepository`: Pinned results data access
  - `CachedPinnedResultRepository`: Caching decorator with automatic cache invalidation
  - `IPinnedResultsCollectionRepository` / `PinnedResultsCollectionRepository`: Collections data access
  - `CachedPinnedResultsCollectionRepository`: Collection caching with invalidation support
- **Components**:
  - `PinnedResultsManagementComponentBase`: Completely refactored from 604 to 359 lines (41% reduction)

##### Architecture Improvements Applied
- **SOLID Principles**: Services follow Single Responsibility, Interface Segregation, and Dependency Inversion
- **DRY (Don't Repeat Yourself)**: Eliminated duplicate validation, error handling, and mapping code
- **Clean Code**: Long methods extracted into focused helper methods with clear names
- **Separation of Concerns**: Clear boundaries between CRUD, validation, synchronization, and UI logic
- **Comprehensive Unit Testing**: 29+ unit tests covering all refactored services with 100% pass rate

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

#### Optimizely Graph Integration
- Synchronization functionality to keep local data in sync with Optimizely Graph
- Graph collection management with automatic sync capabilities
- API services for bi-directional data synchronization
- Connection pooling via IHttpClientFactory for efficient HTTP connections
- Support for collection deletion from both local database and Graph

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
- 35+ comprehensive unit tests with 100% pass rate covering:
  - Repository operations (CRUD, caching, invalidation)
  - Service layer (validation, mapping, error handling)
  - API controllers (HTTP operations, status codes)
  - Graph synchronization (connection pooling, retry logic)
- Eliminated 8+ instances of duplicate error handling code
- Added intelligent caching layer with automatic invalidation
- Implemented connection pooling for all HTTP operations via IHttpClientFactory

#### Service Registration
All new services are automatically registered via `OptiGraphExtensionsServiceExtensions.cs`:
- Common services (error handling, validation, request mapping, caching)
- Decomposed CRUD services with connection pooling
- Graph synchronization services with IHttpClientFactory
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