# OptiGraphExtensions

An Optimizely CMS 12 AddOn that provides comprehensive management of synonyms and pinned results within Optimizely Graph. This package enables content editors and administrators to enhance search experiences through intelligent synonym mapping and result pinning capabilities.

## Features

### Synonym Management
- Create, update, and delete synonyms for enhanced search functionality
- Support for multiple synonym groups with different languages
- Integration with Optimizely Graph for real-time search improvements
- Clean admin interface for easy synonym management

### Pinned Results Management
- Create and manage pinned results collections
- Associate specific content with search phrases
- Priority-based result ordering
- Language-specific pinning support
- Bidirectional synchronization with Optimizely Graph

### Administration Interface
- Clean, intuitive admin interface integrated with Optimizely CMS
- Blazor components for interactive UI elements
- Pagination support for large datasets
- Real-time validation and error handling
- Bulk operations and sync capabilities

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

### Database Setup

Run Entity Framework migrations to set up the required database tables:

```bash
dotnet ef database update --project src/OptiGraphExtensions
```

## Project Structure

```
src/
├── OptiGraphExtensions/           # Main AddOn library
│   ├── Administration/            # Admin controllers and view models
│   ├── Entities/                 # Entity Framework models
│   ├── Features/
│   │   ├── Configuration/        # Service registration and setup
│   │   ├── Synonyms/            # Synonym management feature
│   │   │   ├── Services/        # Business logic services
│   │   │   ├── Repositories/    # Data access layer
│   │   │   └── Models/          # Request/response models
│   │   └── PinnedResults/       # Pinned results management feature
│   │       ├── Services/        # Business logic services
│   │       ├── Repositories/    # Data access layer
│   │       └── Models/          # Request/response models
│   ├── Menus/                   # CMS menu integration
│   └── Views/                   # Razor views and layouts
├── OptiGraphExtensions.Tests/    # NUnit test project
└── Sample/
    └── SampleCms/               # Example implementation
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

### Clean Architecture
The AddOn follows clean architecture principles with clear separation of concerns:

- **Entities**: Domain models and Entity Framework configuration
- **Repositories**: Data access layer with interface abstractions
- **Services**: Business logic layer with validation and API integration
- **Controllers**: API endpoints and admin interface controllers
- **Components**: Blazor components for interactive UI

### Key Components

#### Data Layer
- Entity Framework Core with SQL Server provider
- Database migrations for version management
- Repository pattern for data access abstraction

#### Business Logic
- Service layer with dependency injection
- Validation services for data integrity
- API services for Optimizely Graph integration
- Pagination services for large datasets

#### UI Layer
- MVC controllers for admin interface
- Blazor components for interactive functionality
- Razor views with responsive design
- Integration with Optimizely CMS admin interface

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
- `tbl_OptiGraphExtensions_PinnedResultsCollections`: Stores pinned results collections
- `tbl_OptiGraphExtensions_PinnedResults`: Stores individual pinned results

## API Endpoints

### Synonyms API
- `GET /api/synonyms` - Get all synonyms with pagination
- `POST /api/synonyms` - Create a new synonym
- `PUT /api/synonyms/{id}` - Update an existing synonym
- `DELETE /api/synonyms/{id}` - Delete a synonym

### Pinned Results API
- `GET /api/pinned-results-collections` - Get all collections
- `POST /api/pinned-results-collections` - Create a new collection
- `PUT /api/pinned-results-collections/{id}` - Update a collection
- `DELETE /api/pinned-results-collections/{id}` - Delete a collection
- `GET /api/pinned-results/{collectionId}` - Get pinned results for a collection
- `POST /api/pinned-results` - Create a new pinned result
- `PUT /api/pinned-results/{id}` - Update a pinned result
- `DELETE /api/pinned-results/{id}` - Delete a pinned result

## Optimizely Graph Integration

The AddOn provides seamless integration with Optimizely Graph:

- **Automatic Synchronization**: Keep local data in sync with Graph collections
- **Bidirectional Updates**: Changes flow both ways between local database and Graph
- **Error Handling**: Graceful fallback to local data when Graph is unavailable
- **Authentication**: Supports Optimizely Graph authentication requirements

## Authorization

The admin interface requires users to be members of one of the following roles:
- CmsAdmins
- Administrator
- WebAdmins

## Testing

The project includes comprehensive NUnit tests covering:
- Service layer functionality
- Repository operations
- Validation logic
- Controller standards and conventions

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
- NUnit for testing

## License

This project is licensed under the MIT License. See the LICENSE file for details.

## Support

For issues, questions, or contributions, please visit the project repository or contact the development team.