# IT Asset Management System

A comprehensive web-based IT Asset Management System built with ASP.NET Core MVC for tracking and managing IT assets, including computers, servers, and applications. The system includes a complete IT Support ticket management workflow with role-based access control.

## 🚀 Features

### Asset Management
- **Multi-Asset Type Support**: Manage Computers, Servers, and Applications
- **Asset Tracking**: Track asset tags, serial numbers, purchase dates, warranties, and locations
- **User Assignment**: Assign assets to specific employees
- **Status Management**: Track asset status (Active, Inactive, Retired, etc.)
- **Asset Search & Filtering**: Filter assets by type, status, and search by name/tag
- **Asset Details**: Comprehensive asset information with full history

### IT Support Ticket System
- **Ticket Creation**: Users can create tickets for asset-related issues
- **Auto-Assignment**: New tickets are automatically assigned to IT Support Supervisors
- **Status Workflow**: 
  - Pending → In Progress → Resolved → Closed
  - Technicians cannot skip statuses (must go through In Progress before Resolved)
- **Ticket Assignment**: Supervisors can assign tickets to technicians
- **Priority Levels**: Low, Medium, High, Critical
- **Expiration Tracking**: 
  - Warning after 2 days
  - Expired status after 3 days
  - Automatic notifications to supervisors for overdue tickets
- **Technician Notes**: Technicians can add notes with timestamps
- **Resolution Notes**: Required when resolving tickets
- **User Follow-ups**: Users can add follow-up comments to their tickets
- **Replacement Requests**: Technicians can request asset replacements from admins
- **CSV Export**: Export filtered ticket data to CSV format

### Dashboard & Reporting
- **Role-Based Metrics**: 
  - Admin/IT Manager: View all assets and tickets
  - IT Support Supervisor: Total tickets overview
  - IT Support Technician: Assigned tickets count
  - Employee/Read Only: View only assigned assets
- **Asset Statistics**: Total assets by type, status breakdown
- **Ticket Metrics**: Total tickets, assigned tickets, status distribution
- **Comprehensive Reports**: Generate reports on assets, users, and tickets

### User Management
- **Role-Based Access Control (RBAC)**: Six distinct user roles with granular permissions
- **User Profiles**: Manage user information, departments, and assignments
- **Password Management**: Password expiration and forced password changes
- **Active/Inactive Status**: Enable or disable user accounts

### Security & Audit
- **Authentication**: ASP.NET Core Identity with secure password policies
- **Authorization**: Policy-based authorization for fine-grained access control
- **Audit Logging**: Track all significant system changes and user actions
- **Session Management**: Secure session handling with configurable timeouts

## 🛠️ Technologies

- **.NET 8.0**: Latest .NET framework
- **ASP.NET Core MVC**: Web application framework
- **Entity Framework Core 8.0**: ORM for database operations
- **SQL Server**: Database (LocalDB or SQL Server)
- **ASP.NET Core Identity**: Authentication and authorization
- **Bootstrap**: Frontend framework for responsive UI
- **Razor Pages**: Server-side rendering

## 📋 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) (recommended)
- Git (for cloning the repository)

## 🔧 Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/Siphesiihe/IT_Asset_Management_System.git
   cd IT_Asset_Management_System/IT_Asset_Management_System
   ```

2. **Configure the database connection**
   
   Update `appsettings.json` with your SQL Server connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=IT_Asset_Management_System_DB2;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
     }
   }
   ```

3. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

4. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```
   
   Or using Package Manager Console in Visual Studio:
   ```powershell
   Update-Database
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the application**
   
   Navigate to `https://localhost:5001` or `http://localhost:5000`

## 👤 Default Admin Credentials

Upon first run, the system creates a default admin user:

- **Email**: `admin@itasms.com`
- **Password**: `Admin@123`

**⚠️ Important**: Change the default password immediately after first login!

## 🔐 User Roles & Permissions

### Admin
- Full system access
- User management (create, edit, delete users)
- Asset management (all asset types)
- Ticket management (view, assign, resolve)
- Reports and audit logs
- Can create IT Support roles

### IT Manager
- Similar to Admin (full system access)
- User management
- Asset management
- Ticket oversight

### IT Support Supervisor
- View all tickets
- Assign tickets to technicians
- Update ticket status and priority
- View technician notes
- Cannot add resolution notes (technician-only)
- Dashboard shows total tickets
- **Sidebar**: Dashboard, My Profile, Users, Assets, Report, IT Support, Logs

### IT Support (Technician)
- View assigned and unassigned tickets
- Update ticket status (with workflow restrictions)
- Add technician notes
- Add resolution notes (required when resolving)
- Request asset replacements
- Cannot skip statuses (Pending → In Progress → Resolved)
- Dashboard shows assigned tickets count
- **Sidebar**: Dashboard, My Profile, Report, IT Support, Logs (Assets hidden)

### Employee
- View assigned assets only
- Create tickets for assigned assets
- View ticket status
- Add follow-up comments to own tickets
- Dashboard shows only assigned assets

### Read Only
- View assigned assets only
- View ticket status
- No create/edit permissions
- Dashboard shows only assigned assets

## 📁 Project Structure

```
IT_Asset_Management_System/
├── Controllers/          # MVC Controllers
│   ├── AccountController.cs
│   ├── AssetsController.cs
│   ├── ComputersController.cs
│   ├── ServersController.cs
│   ├── ApplicationsController.cs
│   ├── ITSupportController.cs
│   ├── UsersController.cs
│   ├── ReportsController.cs
│   └── HomeController.cs
├── Models/              # Data Models
│   ├── BaseAsset.cs
│   ├── Computer.cs
│   ├── Server.cs
│   ├── Application.cs
│   ├── ITSupportTicket.cs
│   ├── ApplicationUser.cs
│   ├── AuditLog.cs
│   └── ViewModels/
├── Views/               # Razor Views
│   ├── Assets/
│   ├── ITSupport/
│   ├── Users/
│   ├── Reports/
│   └── Shared/
├── Data/                # Data Access Layer
│   ├── ApplicationDbContext.cs
│   └── DbInitializer.cs
├── Migrations/          # EF Core Migrations
├── Middleware/          # Custom Middleware
│   └── PasswordExpirationMiddleware.cs
├── wwwroot/            # Static Files (CSS, JS, Images)
├── Program.cs          # Application Entry Point
└── appsettings.json    # Configuration
```

## 🎯 Usage

### Creating a Ticket

1. Navigate to **Assets** → Select an asset → Click **Report Issue**
2. Or go to **IT Support** → **Create New Ticket**
3. Select Asset Type and Related Asset (required)
4. Enter Subject, Description, and Priority
5. Submit ticket (automatically assigned to IT Support Supervisor)

### Managing Tickets (Supervisor)

1. Go to **IT Support** → View all tickets
2. Filter by status, priority, or search term
3. Assign tickets to technicians
4. Update status and priority as needed
5. Monitor expired tickets (warnings after 2 days, expired after 3 days)

### Resolving Tickets (Technician)

1. View assigned tickets in **IT Support**
2. Change status from **Pending** to **In Progress**
3. Add technician notes as needed
4. When ready, change status to **Resolved**
5. **Required**: Add resolution notes before resolving
6. Users can close tickets after resolution

### Requesting Asset Replacement

1. As a technician, open a ticket
2. Click **Request Replacement**
3. Enter replacement reason
4. Admin/IT Manager will review and approve/deny

### Exporting Tickets

1. Apply filters to tickets list (status, priority, search)
2. Click **Export CSV** button
3. Download filtered ticket data in CSV format

## 🔄 Database Migrations

The system uses Entity Framework Core migrations for database schema management.

**Create a new migration:**
```bash
dotnet ef migrations add MigrationName
```

**Apply migrations:**
```bash
dotnet ef database update
```

**List applied migrations:**
```bash
dotnet ef migrations list
```

## 🐛 Troubleshooting

### Database Connection Issues
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Ensure database exists or migrations will create it

### Migration Errors
- Ensure all previous migrations are applied
- Check for conflicting schema changes
- Review migration files for errors

### Authentication Issues
- Clear browser cookies
- Verify user account is active
- Check password expiration settings

## 📝 License

This project is proprietary software. All rights reserved.

## 👥 Contributing

This is a private project. For contributions or issues, please contact the project maintainer.

## 📧 Contact

For questions or support, please contact the development team.

---

**Version**: 1.0  
**Last Updated**: January 2025
