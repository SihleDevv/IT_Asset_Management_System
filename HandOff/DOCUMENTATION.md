# IT Asset Management System - Developer Documentation

## Table of Contents
1. [System Overview](#system-overview)
2. [Technology Stack](#technology-stack)
3. [System Architecture](#system-architecture)
4. [Database Structure](#database-structure)
5. [User Roles and Permissions](#user-roles-and-permissions)
6. [Setup and Installation](#setup-and-installation)
7. [Key Features and Workflows](#key-features-and-workflows)
8. [Screenshots and UI Guide](#screenshots-and-ui-guide)
9. [API Endpoints](#api-endpoints)
10. [Common Development Tasks](#common-development-tasks)

---

## System Overview

The IT Asset Management System (ITASMS) is a comprehensive web application designed to manage IT assets including computers, servers, and software applications. The system provides role-based access control, asset tracking, reporting, and IT support ticket management.

**Key Capabilities:**
- Asset Management (Computers, Servers, Applications)
- User Management with Role-Based Access Control
- IT Support Ticket System
- Comprehensive Reporting (CSV/PDF Export)
- Audit Logging
- License Management

---

## Technology Stack

### Backend
- **Framework:** ASP.NET Core 8.0 MVC
- **Language:** C#
- **ORM:** Entity Framework Core 8.0
- **Database:** SQL Server
- **Authentication:** ASP.NET Core Identity 8.0
- **PDF Generation:** QuestPDF 2023.12.1

### Frontend
- **UI Framework:** Bootstrap 5
- **JavaScript Libraries:** jQuery, jQuery Validation
- **Icons:** Font Awesome 6.4.0

### Development Tools
- **IDE:** Visual Studio / Visual Studio Code
- **Version Control:** Git
- **Package Manager:** NuGet

---

## System Architecture

### Project Structure
```
IT_Asset_Management_System/
├── Controllers/          # MVC Controllers
│   ├── AccountController.cs
│   ├── AssetsController.cs
│   ├── ComputersController.cs
│   ├── ServersController.cs
│   ├── ApplicationsController.cs
│   ├── UsersController.cs
│   ├── ITSupportController.cs
│   ├── ReportsController.cs
│   └── HomeController.cs
├── Models/              # Data Models
│   ├── ApplicationUser.cs
│   ├── BaseAsset.cs
│   ├── Computer.cs
│   ├── Server.cs
│   ├── Application.cs
│   ├── ITSupportTicket.cs
│   └── AuditLog.cs
├── Views/               # Razor Views
│   ├── Shared/
│   │   └── _Layout.cshtml
│   ├── Home/
│   ├── Assets/
│   ├── Users/
│   ├── ITSupport/
│   └── Reports/
├── Data/
│   └── ApplicationDbContext.cs
├── wwwroot/            # Static Files
│   ├── css/
│   ├── js/
│   └── lib/
└── Program.cs          # Application Entry Point
```

### Architecture Pattern
- **MVC (Model-View-Controller)** pattern
- **Table-Per-Type (TPT) Inheritance** for asset types
- **Repository Pattern** (via DbContext)
- **Policy-Based Authorization**

---

## Database Structure

### Key Tables

#### Assets (Base Table)
- `Id` (Primary Key)
- `AssetTag` (Unique identifier)
- `AssetName`
- `AssetType` (Computer/Server/Application)
- `Brand`, `Model`, `Location`
- `Status`, `Vendor`
- `PurchaseDate`, `PurchasePrice`
- `CreatedDate`, `ModifiedDate`
- `CreatedBy`, `ModifiedBy`

#### Computers (Inherits from Assets)
- `AssignedTo`
- `OperatingSystem`
- `Processor`, `RAM`, `Storage`

#### Servers (Inherits from Assets)
- `IPAddress`
- `ServerType`
- `OperatingSystem`
- `Processor`, `RAM`, `Storage`
- `ProjectManagerName`
- `BackupRequired`, `BackupComments`

#### Applications (Inherits from Assets)
- `Version`
- `Category`, `BusinessUnit`
- `ApplicationOwner`, `LicenseHolder`
- `RequiresLicense`
- `LicenseType`, `LicenseExpiryDate`
- `TotalLicenses`, `UsedLicenses`

#### Users (ASP.NET Identity)
- Standard Identity fields
- `FullName`, `Department`
- `IsActive`, `MustChangePassword`
- `PasswordChangedDate`

#### ITSupportTickets
- `Subject`, `Description`
- `Status`, `Priority`
- `ReportedByUserId`, `AssignedToUserId`
- `RelatedAssetId`, `RelatedAssetName`
- `CreatedDate`, `ResolvedDate`

#### AuditLogs
- `UserName`, `Action`
- `EntityType`, `EntityId`
- `Details`, `IPAddress`
- `Timestamp`

---

## User Roles and Permissions

### Role Hierarchy
1. **Admin** - Full system access (can delete assets)
2. **IT Manager** - Management access (similar to Admin, but cannot delete assets)
3. **IT Support Supervisor** - Supervise IT Support team
4. **IT Support** - Handle support tickets assigned to them
5. **Employee** - View assigned assets only
6. **Read Only** - Read-only access to assigned assets

### Important Note: Admin and IT Manager Similarities
**Admin and IT Manager share nearly identical functionality**, with the only key difference being:
- **Admin** can delete assets
- **IT Manager** cannot delete assets

Both roles have access to:
- All assets (view, create, edit)
- User management
- All reports and exports
- Audit logs
- IT Support ticket management
- Full dashboard access

### Permission Matrix

| Feature | Admin | IT Manager | IT Support Supervisor | IT Support | Employee | Read Only |
|---------|-------|------------|----------------------|------------|----------|-----------|
| View All Assets | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| View Assigned Assets | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Create/Edit Assets | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Delete Assets | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Manage Users | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| View All Tickets | ✅ | ✅ | ✅ | Limited | ❌ | ❌ |
| Create Tickets | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| View Reports | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| View Audit Logs | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |

---

## Setup and Installation

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or SQL Server Express)
- Visual Studio 2022 or VS Code
- Git

### Installation Steps

1. **Clone the Repository**
   ```bash
   git clone [repository-url]
   cd IT_Asset_Management_System
   ```

2. **Restore NuGet Packages**
   ```bash
   cd IT_Asset_Management_System
   dotnet restore
   ```

3. **Configure Database Connection**
   - Open `appsettings.json`
   - Update the connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ITAssetManagement;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```

4. **Run Database Migrations**
   ```bash
   dotnet ef database update
   ```

5. **Seed Initial Data** (if applicable)
   - Check for seed data scripts in the project

6. **Run the Application**
   ```bash
   dotnet run
   ```

7. **Access the Application**
   - Navigate to: `http://localhost:5062` (or port shown in terminal)
   - Default admin credentials: [To be configured]

### Initial Setup
1. Create first admin user through registration or seed data
2. Configure password policies in `Program.cs`
3. Set up email settings (if email functionality is enabled)

---

## Key Features and Workflows

### 1. Asset Management

#### Adding a New Asset
**Screenshot Placeholders:**
- `screenshots/assets/create-asset-admin.png` - Admin view (with delete option visible)
- `screenshots/assets/create-asset-itmanager.png` - IT Manager view (no delete option)
- `screenshots/assets/create-asset-employee.png` - Employee view (create disabled, view only)

**Note:** Admin and IT Manager see the same create form. Only Admin can delete assets after creation.

#### Importing Assets from CSV
**Screenshot Placeholders:**
- `screenshots/assets/import-assets-admin.png` - Admin importing assets via CSV
- `screenshots/assets/import-results.png` - Import results showing success/failure counts

**Features:**
- CSV import for Computers, Servers, and Applications
- Template download available for each asset type
- Asset Type column in CSV ensures correct asset type detection
- All fields from manual creation are supported
- User assignment validation (matches by Full Name or Email)
- Import results show detailed success/error messages
- Example CSV files available in `Import/` folder

**CSV Import Format:**
- First column must be "Asset Type" (Computer/Server/Application)
- Required fields: Asset Tag, Asset Name
- All other fields are optional but recommended
- User assignment fields: "Assigned To" (Computer), "Project Manager Name" (Server), "Application Owner" (Application)

#### Viewing Assets (Role-Based Views)
**Screenshot Placeholders:**
- `screenshots/assets/index-view-admin.png` - Admin view (all assets, all actions including delete)
- `screenshots/assets/index-view-itmanager.png` - IT Manager view (all assets, edit but no delete)
- `screenshots/assets/index-view-itsupport.png` - IT Support view (no assets menu, hidden from sidebar)
- `screenshots/assets/index-view-employee.png` - Employee view (only assigned assets, view only)
- `screenshots/assets/index-view-readonly.png` - Read Only view (only assigned assets, no actions)

**Key Differences:**
- **Admin/IT Manager:** See all assets, can create/edit, Admin can delete
- **IT Support:** Assets menu hidden from sidebar
- **Employee/Read Only:** Only see assigned assets, no create/edit/delete

#### Editing Assets
**Screenshot Placeholders:**
- `screenshots/assets/edit-asset-admin.png` - Admin edit view
- `screenshots/assets/edit-asset-itmanager.png` - IT Manager edit view (same as Admin)
- `screenshots/assets/edit-asset-employee.png` - Employee view (edit disabled)

#### Bulk Operations on Assets
**Screenshot Placeholders:**
- `screenshots/assets/bulk-actions-bar.png` - Bulk actions bar with selected count
- `screenshots/assets/bulk-edit-form.png` - Bulk edit form showing only common fields
- `screenshots/assets/select-all-checkbox.png` - Select All checkbox in table header

**Features:**
- **Select All Checkbox:** Select/deselect all records at once
- **Bulk Actions Bar:** Appears when records are selected, showing:
  - Selected record count
  - Edit Selected button
  - Delete Selected button (spaced between buttons)
- **Bulk Edit:**
  - Single selection: Goes to regular edit page
  - Multiple selections: Opens bulk edit form
  - Only common fields are editable (Brand, Model, Serial Number, Location, Status, Purchase Date, Purchase Price, Vendor, Warranty Expiry Date, Notes)
  - Empty fields are ignored (preserves existing values)
  - Updates all selected assets simultaneously
  - Shows list of assets being edited
- **Bulk Delete:**
  - Validates dependencies before deletion
  - Shows confirmation dialog with count
  - Logs all deletions in audit log
  - Displays success/error messages

### 2. User Management

#### Creating Users
**Screenshot Placeholders:**
- `screenshots/users/create-user-admin.png` - Admin creating user (all roles available)
- `screenshots/users/create-user-itmanager.png` - IT Manager creating user (same as Admin)
- `screenshots/users/create-user-other-roles.png` - Other roles (user management hidden)

**Note:** Admin and IT Manager have identical user management capabilities.

#### Importing Users from CSV
**Screenshot Placeholders:**
- `screenshots/users/import-users-admin.png` - Admin importing users via CSV
- `screenshots/users/import-results.png` - Import results showing success/failure counts

**Features:**
- CSV import for bulk user creation
- Template download available
- Default password: `TempPass123!@#` (all imported users)
- Mandatory password change on first login
- Required fields: Full Name, Email
- Optional fields: Department, Role, IsActive
- Import results show detailed success/error messages
- Example CSV file available in `Import/Users_Import_5Records.csv`

**CSV Import Format:**
- Columns: Full Name, Email, Department, Role, IsActive
- Email must be unique (skips duplicates)
- Role must exist in system (defaults to "Employee" if invalid)
- All imported users must change password on first login

#### Managing User Roles
**Screenshot Placeholders:**
- `screenshots/users/manage-roles-admin.png` - Admin managing roles
- `screenshots/users/manage-roles-itmanager.png` - IT Manager managing roles (same interface)
- `screenshots/users/user-list-admin.png` - Admin user list view
- `screenshots/users/user-list-itmanager.png` - IT Manager user list view (identical)

#### Bulk Operations on Users
**Screenshot Placeholders:**
- `screenshots/users/bulk-actions-bar.png` - Bulk actions bar with selected count
- `screenshots/users/bulk-edit-users.png` - Bulk edit (redirects to individual edit for first selected)

**Features:**
- **Select All Checkbox:** Select/deselect all users at once
- **Bulk Actions Bar:** Appears when users are selected
- **Bulk Edit:**
  - Single selection: Goes to regular edit page
  - Multiple selections: Prompts to edit first selected user
- **Bulk Delete:**
  - Validates dependencies (assets, IT Support tickets)
  - Prevents deleting own account
  - Shows confirmation dialog
  - Logs all deletions in audit log

### 3. IT Support Tickets

#### Creating a Ticket
**Screenshot Placeholders:**
- `screenshots/itsupport/create-ticket-all-roles.png` - All users can create tickets (same interface)
- `screenshots/itsupport/create-ticket-employee.png` - Employee creating ticket

**Note:** Ticket creation interface is the same for all roles.

#### Managing Tickets (Role-Based Views)
**Screenshot Placeholders:**
- `screenshots/itsupport/ticket-list-admin.png` - Admin view (all tickets, full management)
- `screenshots/itsupport/ticket-list-itmanager.png` - IT Manager view (all tickets, full management, same as Admin)
- `screenshots/itsupport/ticket-list-itsupportsupervisor.png` - IT Support Supervisor view (all tickets, can assign)
- `screenshots/itsupport/ticket-list-itsupport.png` - IT Support view (only assigned/unassigned tickets)
- `screenshots/itsupport/ticket-list-employee.png` - Employee view (only own tickets, view status only)

**Key Differences:**
- **Admin/IT Manager:** See all tickets, can manage everything
- **IT Support Supervisor:** See all tickets, can assign to technicians
- **IT Support:** See assigned tickets and unassigned tickets
- **Employee/Read Only:** See only their own tickets

#### Bulk Operations on IT Support Tickets
**Screenshot Placeholders:**
- `screenshots/itsupport/bulk-actions-bar.png` - Bulk actions bar with selected count

**Features:**
- **Select All Checkbox:** Select/deselect all tickets at once
- **Bulk Actions Bar:** Appears when tickets are selected (Admin/IT Manager only)
- **Bulk Edit:**
  - Single selection: Goes to regular edit page
  - Multiple selections: Prompts to edit first selected ticket
- **Bulk Delete:**
  - Available to Admin and IT Manager only
  - Shows confirmation dialog
  - Logs all deletions in audit log

### 4. Reports

**Note:** Reports are only accessible to Admin and IT Manager. Both roles see identical report interfaces.

#### Computer Report
**Screenshot Placeholders:**
- `screenshots/reports/computer-report-admin.png` - Admin view
- `screenshots/reports/computer-report-itmanager.png` - IT Manager view (identical to Admin)
- `screenshots/reports/computer-report-other-roles.png` - Other roles (reports menu hidden)

#### Server Report
**Screenshot Placeholders:**
- `screenshots/reports/server-report-admin.png` - Admin view
- `screenshots/reports/server-report-itmanager.png` - IT Manager view (identical to Admin)

#### Application Report
**Screenshot Placeholders:**
- `screenshots/reports/application-report-admin.png` - Admin view
- `screenshots/reports/application-report-itmanager.png` - IT Manager view (identical to Admin)

#### License Report
**Screenshot Placeholders:**
- `screenshots/reports/license-report-admin.png` - Admin view
- `screenshots/reports/license-report-itmanager.png` - IT Manager view (identical to Admin)

#### Asset Summary
**Screenshot Placeholders:**
- `screenshots/reports/asset-summary-admin.png` - Admin view
- `screenshots/reports/asset-summary-itmanager.png` - IT Manager view (identical to Admin)

#### Audit Log
**Screenshot Placeholders:**
- `screenshots/reports/audit-log-admin.png` - Admin view
- `screenshots/reports/audit-log-itmanager.png` - IT Manager view (identical to Admin)
- `screenshots/reports/audit-log-other-roles.png` - Other roles (logs menu hidden)

### 5. Dashboard

#### Home Dashboard (Role-Based Views)
**Screenshot Placeholders:**
- `screenshots/home/dashboard-admin.png` - Admin dashboard (all assets, all tickets, full statistics)
- `screenshots/home/dashboard-itmanager.png` - IT Manager dashboard (identical to Admin)
- `screenshots/home/dashboard-itsupportsupervisor.png` - IT Support Supervisor dashboard (total tickets overview)
- `screenshots/home/dashboard-itsupport.png` - IT Support dashboard (assigned tickets count only)
- `screenshots/home/dashboard-employee.png` - Employee dashboard (only assigned assets)
- `screenshots/home/dashboard-readonly.png` - Read Only dashboard (only assigned assets, read-only)

**Key Differences:**
- **Admin/IT Manager:** Full dashboard with all asset counts, ticket statistics, recent activity
- **IT Support Supervisor:** Shows total tickets overview
- **IT Support:** Shows only assigned tickets count
- **Employee/Read Only:** Shows only assigned assets count

---

## Screenshots and UI Guide

### Navigation Structure

#### Sidebar Navigation (Role-Based)
**Screenshot Placeholders:**
- `screenshots/navigation/sidebar-admin.png` - Admin sidebar (all menu items visible)
- `screenshots/navigation/sidebar-itmanager.png` - IT Manager sidebar (identical to Admin)
- `screenshots/navigation/sidebar-itsupportsupervisor.png` - IT Support Supervisor sidebar (Assets hidden)
- `screenshots/navigation/sidebar-itsupport.png` - IT Support sidebar (Assets hidden)
- `screenshots/navigation/sidebar-employee.png` - Employee sidebar (Users, Reports, Logs hidden)
- `screenshots/navigation/sidebar-readonly.png` - Read Only sidebar (Users, Reports, Logs hidden)

**Menu Order (from top to bottom):**
1. Dashboard
2. My Profile
3. Users (Admin/IT Manager only)
4. **Assets** (starts here - before IT Support)
5. Reports (Admin/IT Manager only)
6. **IT Support** (appears after Assets)
7. Logs (Admin/IT Manager only)

**Menu Visibility by Role:**
- **Admin/IT Manager:** Dashboard, My Profile, Users, Assets, Reports, IT Support, Logs
- **IT Support Supervisor:** Dashboard, My Profile, Users, Reports, IT Support, Logs (Assets hidden)
- **IT Support:** Dashboard, My Profile, Reports, IT Support, Logs (Users, Assets hidden)
- **Employee/Read Only:** Dashboard, My Profile, Assets (assigned only), IT Support (own tickets only)

**Important:** Assets menu item appears **before** IT Support in the sidebar navigation. IT Support is positioned last among the main menu items (before Logs).

#### Top Navigation Bar (Role Badge)
**Screenshot Placeholders:**
- `screenshots/navigation/top-navbar-admin.png` - Shows "Admin" badge
- `screenshots/navigation/top-navbar-itmanager.png` - Shows "IT Manager" badge
- `screenshots/navigation/top-navbar-itsupportsupervisor.png` - Shows "IT Support Supervisor" badge
- `screenshots/navigation/top-navbar-itsupport.png` - Shows "IT Support" badge
- `screenshots/navigation/top-navbar-employee.png` - Shows "Employee" badge
- `screenshots/navigation/top-navbar-readonly.png` - Shows "Read Only" badge

**Note:** The role badge appears in the top-right corner of the navbar for all logged-in users, showing their highest-priority role.

### Key UI Components

#### Action Buttons
**Screenshot Placeholder:** `screenshots/ui/action-buttons.png`
- Icons only (no text labels)
- Horizontal alignment
- Color coding:
  - Blue: View/Details
  - Yellow: Edit
  - Red: Delete
  - Green: Manage Applications

#### Export Dropdown
**Screenshot Placeholder:** `screenshots/ui/export-dropdown.png`
- Single "Export" button (green, small size)
- Dropdown menu with:
  - Export as CSV
  - Export as PDF
- Available on:
  - Assets Index page
  - All Report pages (Computer, Server, Application, License, Asset Summary)
  - IT Support Index page
  - Audit Log page
- Respects user permissions (users can only export what they can view)

#### Search and Filter
**Screenshot Placeholder:** `screenshots/ui/search-filter.png`
- Search input field
- Filter dropdowns
- Filter and Clear buttons

#### Tables
**Screenshot Placeholder:** `screenshots/ui/data-tables.png`
- Responsive tables
- **Select All Checkbox:** First column in table header (Assets, Users, IT Support)
- Asset Tag column (first data column, bold)
- Status badges with color coding
- Action icons in last column (horizontal alignment, icon-only)

#### Import Button
**Screenshot Placeholder:** `screenshots/ui/import-button.png`
- Green button, same size as Export button
- Located next to Export dropdown
- Available on:
  - Assets Index page (for asset import)
  - Users Index page (for user import)
- Template download available for each import type

---

## API Endpoints

### Assets Controller
- `GET /Assets` - List all assets (filtered by user permissions)
- `GET /Assets/Create` - Create new asset form
- `POST /Assets/Create` - Create new asset
- `GET /Assets/Edit/{id}` - Edit asset form
- `POST /Assets/Edit/{id}` - Update asset
- `GET /Assets/BulkEdit` - Bulk edit form (multiple assets)
- `POST /Assets/BulkEdit` - Update multiple assets
- `GET /Assets/Details/{id}` - View asset details
- `GET /Assets/Delete/{id}` - Delete confirmation
- `POST /Assets/Delete/{id}` - Delete asset
- `POST /Assets/BulkDelete` - Delete multiple assets
- `GET /Assets/Import` - Import assets from CSV form
- `POST /Assets/Import` - Process CSV import
- `GET /Assets/DownloadTemplate` - Download CSV template
- `GET /Assets/ExportToCsv` - Export assets to CSV
- `GET /Assets/ExportToPdf` - Export assets to PDF

### Reports Controller
- `GET /Reports/ComputerReport` - Computer report view
- `GET /Reports/ServerReport` - Server report view
- `GET /Reports/ApplicationReport` - Application report view
- `GET /Reports/LicenseReport` - License report view
- `GET /Reports/AssetSummary` - Asset summary view
- `GET /Reports/AuditLog` - Audit log view (Admin only)
- `GET /Reports/ExportComputerReportToCsv` - Export computer report CSV
- `GET /Reports/ExportComputerReportToPdf` - Export computer report PDF
- `GET /Reports/ExportServerReportToCsv` - Export server report CSV
- `GET /Reports/ExportServerReportToPdf` - Export server report PDF
- `GET /Reports/ExportApplicationReportToCsv` - Export application report CSV
- `GET /Reports/ExportApplicationReportToPdf` - Export application report PDF
- `GET /Reports/ExportLicenseReportToCsv` - Export license report CSV
- `GET /Reports/ExportLicenseReportToPdf` - Export license report PDF
- `GET /Reports/ExportAuditLogToCsv` - Export audit log CSV (Admin only)
- `GET /Reports/ExportAuditLogToPdf` - Export audit log PDF (Admin only)

### ITSupport Controller
- `GET /ITSupport` - List tickets (filtered by user permissions)
- `GET /ITSupport/Create` - Create ticket form
- `POST /ITSupport/Create` - Create ticket
- `GET /ITSupport/Details/{id}` - View ticket details
- `GET /ITSupport/Edit/{id}` - Edit ticket form
- `POST /ITSupport/Edit/{id}` - Update ticket
- `POST /ITSupport/BulkDelete` - Delete multiple tickets (Admin/IT Manager only)
- `GET /ITSupport/ExportToCsv` - Export tickets to CSV
- `GET /ITSupport/ExportToPdf` - Export tickets to PDF

### Users Controller
- `GET /Users` - List all users (Admin/IT Manager only)
- `GET /Users/Create` - Create new user form
- `POST /Users/Create` - Create new user
- `GET /Users/Edit/{id}` - Edit user form
- `POST /Users/Edit/{id}` - Update user
- `GET /Users/Delete/{id}` - Delete confirmation
- `POST /Users/Delete/{id}` - Delete user
- `POST /Users/BulkDelete` - Delete multiple users
- `GET /Users/Import` - Import users from CSV form
- `POST /Users/Import` - Process CSV import
- `GET /Users/DownloadTemplate` - Download CSV template
- `GET /Users/ExportToCsv` - Export users to CSV

---

## Common Development Tasks

### Adding a New Asset Field

1. **Update Base Model** (`Models/BaseAsset.cs` or specific model)
   ```csharp
   public string? NewField { get; set; }
   ```

2. **Create Migration**
   ```bash
   dotnet ef migrations add AddNewFieldToAssets
   ```

3. **Update Database**
   ```bash
   dotnet ef database update
   ```

4. **Update Views**
   - Add field to Create/Edit forms
   - Add column to Index/Details views
   - Update export methods (CSV/PDF)

### Adding a New Report

1. **Create Controller Action** (`Controllers/ReportsController.cs`)
   ```csharp
   public async Task<IActionResult> NewReport()
   {
       // Query logic
       return View(data);
   }
   ```

2. **Create View** (`Views/Reports/NewReport.cshtml`)
   - Add search/filter form
   - Add export dropdown
   - Display data table

3. **Add Export Methods**
   - `ExportNewReportToCsv`
   - `ExportNewReportToPdf`

4. **Add Navigation Link** (if needed)
   - Update `_Layout.cshtml` sidebar

### Modifying User Permissions

1. **Update Authorization Attributes**
   ```csharp
   [Authorize(Policy = "RequireAdmin")]
   [Authorize(Roles = "Admin,IT Manager")]
   ```

2. **Update View Logic**
   ```csharp
   @if (User.IsInRole("Admin"))
   {
       // Admin-only content
   }
   ```

3. **Update Controller Logic**
   ```csharp
   if (User.IsInRole("Admin") || User.IsInRole("IT Manager"))
   {
       // Permission-based logic
   }
   ```

### Adding Export Functionality

1. **CSV Export**
   ```csharp
   public async Task<IActionResult> ExportToCsv()
   {
       var data = await GetData();
       var csv = new StringBuilder();
       csv.AppendLine("Column1,Column2,Column3");
       foreach (var item in data)
       {
           csv.AppendLine($"{item.Field1},{item.Field2},{item.Field3}");
       }
       return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "filename.csv");
   }
   ```

2. **PDF Export**
   ```csharp
   public async Task<IActionResult> ExportToPdf()
   {
       var data = await GetData();
       QuestPDF.Settings.License = LicenseType.Community;
       var document = Document.Create(container => {
           // PDF structure
       });
       var pdfBytes = document.GeneratePdf();
       return File(pdfBytes, "application/pdf", "filename.pdf");
   }
   ```

### Adding Bulk Operations

1. **Bulk Edit Implementation**
   ```csharp
   [HttpGet]
   [Authorize(Roles = "Admin")]
   public async Task<IActionResult> BulkEdit(string ids)
   {
       // Parse IDs, load assets, return view with common fields only
   }

   [HttpPost]
   [ValidateAntiForgeryToken]
   [Authorize(Roles = "Admin")]
   public async Task<IActionResult> BulkEdit(string assetIds, IFormCollection form)
   {
       // Update only provided fields, leave empty fields unchanged
   }
   ```

2. **Bulk Delete Implementation**
   ```csharp
   [HttpPost]
   [ValidateAntiForgeryToken]
   [Authorize(Roles = "Admin")]
   public async Task<IActionResult> BulkDelete(List<int> ids)
   {
       // Validate dependencies, delete assets, log actions
   }
   ```

3. **Frontend JavaScript**
   - Add "Select All" checkbox in table header
   - Add bulk actions bar (shows when records selected)
   - Handle bulk edit/delete form submissions
   - Update selected count dynamically

### Adding CSV Import Functionality

1. **Import GET Action**
   ```csharp
   [HttpGet]
   [Authorize(Policy = "RequireAdmin")]
   public IActionResult Import(string? assetType)
   {
       ViewBag.AssetType = assetType ?? "";
       return View();
   }
   ```

2. **Import POST Action**
   ```csharp
   [HttpPost]
   [ValidateAntiForgeryToken]
   [Authorize(Policy = "RequireAdmin")]
   public async Task<IActionResult> Import(IFormFile csvFile, string? assetType)
   {
       // Parse CSV, validate data, create assets, return results
   }
   ```

3. **Template Download**
   ```csharp
   [HttpGet]
   public IActionResult DownloadTemplate(string? assetType)
   {
       // Generate CSV template with headers and example row
   }
   ```

---

## Important Notes

### Security Considerations
- All controllers require `[Authorize]` attribute
- Password expiration middleware is active
- Audit logging tracks all user actions
- Role-based access control enforced throughout

### Performance Considerations
- Reports are limited to 500 records for audit logs
- Large exports may take time - consider pagination
- Database queries use Include() for related data

### Customization Points
- Password policies in `Program.cs`
- Session timeout settings
- Email configuration (if implemented)
- Report column configurations

---

## Troubleshooting

### Common Issues

1. **Database Connection Error**
   - Check connection string in `appsettings.json`
   - Verify SQL Server is running
   - Check database exists

2. **Migration Errors**
   - Ensure database is up to date: `dotnet ef database update`
   - Check for pending migrations

3. **Permission Denied Errors**
   - Verify user has correct role
   - Check authorization attributes on controllers

4. **Export Not Working**
   - Check QuestPDF license is set
   - Verify file permissions
   - Check browser download settings

---

## Contact and Support

For questions or issues during development:
- Review this documentation
- Check code comments
- Review existing implementations for patterns
- Contact: [Your Contact Information]

---

## Version History

- **v1.1** - Enhanced Features (Current)
  - Bulk operations (Select All, Bulk Edit, Bulk Delete) for Assets, Users, and IT Support
  - CSV Import functionality for Assets and Users
  - Bulk Edit form with common fields only
  - Export dropdowns (CSV/PDF) across all sections
  - Improved user experience with bulk actions bar
  - Default password assignment for imported users
  - Mandatory password change for imported users

- **v1.0** - Initial release
  - Asset management
  - User management
  - IT Support tickets
  - Reporting with CSV/PDF export
  - Role-based access control
  - Audit logging

---

**Last Updated:** January 2025
**Documentation Version:** 1.1
