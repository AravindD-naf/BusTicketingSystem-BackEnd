# Bus Ticketing System - API Documentation

## Overview
A comprehensive Bus Ticketing System built with .NET 10, designed with simplicity and cleaner architecture. The system manages buses, routes, schedules, bookings, and seat management with proper role-based access control.

## Key Features

### 1. Bus Management
- **Create Bus**: Admin can create buses with seats (max 40 seats)
- **Initial Status**: Buses start in **Inactive** state
- **Activation**: Buses automatically become **Active** when scheduled to a route
- **Bus Operations**: View, Update, Delete buses
- **Operator Filtering**: Search buses by operator

### 2. Master Tables (Source & Destination)
- **Centralized Management**: Separate tables for sources and destinations
- **Easy Lookup**: Get source/destination by ID instead of string matching
- **Active Management**: Enable/disable sources and destinations
- **Search Support**: All listing endpoints support pagination

### 3. Route Management
- **Distance Tracking**: Routes include distance information (0.1 - 10000 km)
- **Travel Duration**: Estimated travel time in minutes (1 - 1440 minutes)
- **Base Fare**: Dynamic pricing support
- **Advanced Search**: Search by source, destination, or both

### 4. Schedule Management
- **Automatic Seat Generation**: Uses stored procedure `sp_GenerateSeatsForSchedule`
- **Seat Layout**: Generates seats in format A1, A2, B1, B2, etc.
- **Max 40 Seats**: Enforced per schedule
- **Bus Activation**: Bus becomes active when first scheduled

### 5. Seat Management
- **Automatic Generation**: Seats created via stored procedure
- **Status Tracking**: Available, Locked, Booked
- **Seat Locking**: 5-minute timeout on locked seats
- **Booking Support**: Seat allocation during booking

### 6. Booking System
- **Complete Workflow**: Lock seats → Create booking → Process payment
- **Seat Lock Management**: Automatic cleanup of expired locks
- **Refund Support**: Cancel bookings with refund processing
- **Booking History**: Track all user bookings

### 7. API Endpoints Conversion
- **All GET → POST**: List endpoints converted to POST with pagination in body
- **Pagination Standard**: PageNumber (default: 1) and PageSize (default: 10)
- **Search Support**: Advanced search with sorting capability
- **Status Codes**: 
  - 200: Success
  - 201: Created
  - 400: Bad Request
  - 401: Unauthorized
  - 404: Not Found
  - 500: Server Error

### 8. Sorting Features
- **Sort by Time**: Schedule departure/arrival times
- **Sort by Price**: Route base fare
- **Sort by Distance**: Route distance
- **Sort by Duration**: Travel duration
- **Sort by Source/Destination**: Route sorting
- **Sort by Availability**: Seat availability

## API Endpoints

### Buses
```
POST   /api/v1/buses                    - Create bus
POST   /api/v1/buses/get-all           - List all buses (with pagination)
POST   /api/v1/buses/{id}              - Get bus by ID
PUT    /api/v1/buses/{id}              - Update bus
DELETE /api/v1/buses/{id}              - Delete bus
POST   /api/v1/buses/search-by-operator - Search by operator
```

### Sources & Destinations
```
POST   /api/v1/sources                 - Create source
POST   /api/v1/sources/get-all        - List sources
POST   /api/v1/sources/{id}           - Get source by ID
PUT    /api/v1/sources/{id}           - Update source
DELETE /api/v1/sources/{id}           - Delete source

POST   /api/v1/destinations            - Create destination
POST   /api/v1/destinations/get-all   - List destinations
POST   /api/v1/destinations/{id}      - Get destination by ID
PUT    /api/v1/destinations/{id}      - Update destination
DELETE /api/v1/destinations/{id}      - Delete destination
```

### Routes
```
POST   /api/v1/routes                    - Create route
POST   /api/v1/routes/get-all           - List routes (with pagination)
POST   /api/v1/routes/{id}              - Get route by ID
PUT    /api/v1/routes/{id}              - Update route
DELETE /api/v1/routes/{id}              - Delete route
POST   /api/v1/routes/search-by-source  - Search by source
POST   /api/v1/routes/search-by-destination - Search by destination
POST   /api/v1/routes/search            - Advanced search
```

### Schedules
```
POST   /api/v1/schedules                 - Create schedule
POST   /api/v1/schedules/get-all        - List schedules
POST   /api/v1/schedules/{id}           - Get schedule by ID
PUT    /api/v1/schedules/{id}           - Update schedule
DELETE /api/v1/schedules/{id}           - Delete schedule
POST   /api/v1/schedules/search-by-from-city - Search by origin
POST   /api/v1/schedules/search-by-to-city   - Search by destination
POST   /api/v1/schedules/search              - Advanced search
```

### Bookings
```
POST   /api/v1/booking                     - Create booking
POST   /api/v1/booking/schedules/get-all  - List schedules
POST   /api/v1/booking/schedules/search   - Search schedules
POST   /api/v1/booking/my-bookings        - Get user's bookings
POST   /api/v1/booking/get-all            - Get all bookings (Admin)
POST   /api/v1/booking/{id}               - Get booking by ID
POST   /api/v1/booking/seats/{scheduleId} - Get seat layout
POST   /api/v1/booking/seats/lock         - Lock seats
POST   /api/v1/booking/seats/release      - Release seats
PUT    /api/v1/booking/cancel/{id}        - Cancel booking
```

### Audit Logs
```
POST   /api/v1/auditlogs/get-all  - Get audit logs with filtering
```

## Request/Response Models

### Pagination Request
```json
{
  "pageNumber": 1,
  "pageSize": 10
}
```

### API Response Format
```json
{
  "success": true,
  "message": "Operation successful",
  "data": {}
}
```

### Error Response
```json
{
  "success": false,
  "message": "Error description",
  "data": null
}
```

## Exception Handling
- **BadRequestException**: Invalid input (400)
- **UnauthorizedException**: Missing/invalid authentication (401)
- **NotFoundException**: Resource not found (404)
- **ConflictException**: Resource already exists (409)
- **ValidationException**: Field validation failed
- **ApplicationException**: General errors (500)

## Database Features
- **Soft Deletes**: IsDeleted flag on all entities
- **Audit Logging**: Track all changes with user and IP
- **Stored Procedures**: `sp_GenerateSeatsForSchedule` for seat creation
- **Query Filters**: Automatically excludes deleted records
- **Timestamps**: CreatedAt and UpdatedAt on all entities

## Authentication & Authorization
- **JWT Bearer**: Token-based authentication
- **Roles**: Admin and Customer
- **Rate Limiting**: 100 requests/minute globally, 5/minute for login
- **IP Tracking**: All operations logged with IP address

## Code Quality Standards
- **Beginner-Friendly**: Simple, clean code with minimal complexity
- **Proper Exception Handling**: All operations return appropriate HTTP status codes
- **DI/IoC**: All dependencies injected
- **Repository Pattern**: Data access abstraction
- **Service Layer**: Business logic separation
- **DTOs**: Request/Response data models

## Unit Testing
- **Service Tests**: BusServiceTests, SourceServiceTests, DestinationServiceTests
- **Model Tests**: ModelValidationTests covering all entities
- **DTO Tests**: DTOValidationTests with validation scenarios
- **Moq Framework**: Mocking for dependencies
- **xUnit**: Modern testing framework

## Running the Application

### Prerequisites
- .NET 10.0 SDK
- SQL Server 2019 or later
- Visual Studio 2022 or VS Code

### Setup
1. Clone repository
2. Update connection string in `appsettings.json`
3. Run migrations: `dotnet ef database update`
4. Run application: `dotnet run`

### Running Tests
```bash
dotnet test BusTicketingSystem.Tests
```

## Migration Strategy
All migrations are managed through Entity Framework Core. Key migrations:
- Initial schema setup
- Add Source and Destination tables
- Create stored procedure for seat generation
- Add route distance and travel duration properties

## Future Enhancements
- Real-time seat availability
- Payment gateway integration
- SMS/Email notifications
- Refund automation
- Mobile app API
- Analytics dashboard
