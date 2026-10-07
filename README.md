# AcxiomCRM

**Enterprise Sales & Customer Relationship Management Web Application**  
*Built with ASP.NET Core 8.0 MVC, Entity Framework Core, ASP.NET Core Identity, Bootstrap 5, and Chart.js.*

---

## 1. Architectural Overview

AcxiomCRM follows a strict **Clean Layered Architecture** separating presentation, business logic, entities, and data persistence:

```
AcxiomCRM/
│
├── AcxiomCRM.Domain/          # Core Domain Entities, Enums, and Business Rules
│   ├── Entities/              # Customer, Lead, Opportunity, FollowUp, Activity, AuditLog
│   ├── Enums/                 # LeadStatus, OpportunityStage, FollowUpStatus, Priority, etc.
│   └── BusinessRules/         # LeadStatusTransitionRule, OpportunityValidationRule, etc.
│
├── AcxiomCRM.Application/     # Application Logic, Use Cases, DTOs, and Service Interfaces
│   ├── DTOs/                  # CustomerDtos, LeadDtos, OpportunityDtos, PagedResult, etc.
│   ├── Interfaces/            # ICustomerService, ILeadService, IOpportunityService, etc.
│   └── Services/              # CustomerService, LeadService, OpportunityService, etc.
│
├── AcxiomCRM.Infrastructure/  # EF Core, Identity, DbContext, Fluent API, and Audit Persistence
│   ├── Data/                  # ApplicationDbContext, DbInitializer (Seed Data)
│   ├── Identity/              # ApplicationUser, ApplicationRole, UserService
│   └── Migrations/            # EF Core Migrations (InitialCreate)
│
├── AcxiomCRM.Web/             # ASP.NET Core MVC & REST Web API Presentation Layer
│   ├── Controllers/           # Account, Dashboard, Customer, Lead, Opportunity, etc.
│   ├── Controllers/Api/       # CustomersApi, LeadsApi, OpportunitiesApi, FollowUpsApi, etc.
│   ├── Views/                 # Razor Views with responsive Bootstrap 5 and Chart.js
│   ├── wwwroot/               # Static assets, CSS, JavaScript, icons
│   └── Program.cs             # DI configuration, Identity security, Middleware, Swagger
│
└── AcxiomCRM.Tests/           # Automated Test Suite (xUnit + Moq + EF Core In-Memory)
    ├── Unit/                  # Service unit tests & Lead conversion workflow tests
    ├── Validation/            # Business rule validation tests (Amount, Date, Status transitions)
    └── Authorization/         # Role-based scoping and REST API contract tests
```

---

## 2. Core Modules & Capabilities

1. **Authentication & Identity**:
   - Framework-managed **ASP.NET Core Identity** (no custom password tables).
   - Adaptive password hashing, 8+ character complexity rules, and account lockout protection after repeated failed logins.
   - Anti-forgery tokens (`ValidateAntiForgeryToken`) on all state-changing MVC requests.
2. **Dashboard & Analytics**:
   - 8 Real-time KPI Cards: Total Customers, Total Leads, Open Leads, Total Opportunities, Open Opportunities, Won Opportunities, Lost Opportunities, Total Pipeline Value.
   - 3 Interactive Chart.js charts: Lead Status breakdown, Opportunity Pipeline stages, Monthly Sales.
   - Date range filters: All Time, Today, This Week, This Month.
3. **Customer Management**:
   - Customer Master Data with duplicate prevention and server-enforced email and phone uniqueness.
   - Profile overview with linked opportunities, follow-ups, and activity history.
4. **Lead Management & Conversion**:
   - Lead capture, source, status progression (`New` $\rightarrow$ `Contacted` $\rightarrow$ `Qualified` $\rightarrow$ `Converted` / `Lost`).
   - Server-enforced status transition engine.
   - **Lead Conversion Workflow**: Converts qualified leads directly into active Customers and optional Opportunities.
5. **Opportunity Management & Pipeline**:
   - Stages: `Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`.
   - Business rules enforced server-side: Amount $> 0$, Probability $0-100\%$, future close date for active deals.
   - Weighted pipeline calculated dynamically: $\text{Amount} \times \frac{\text{Probability}}{100}$.
   - Kanban Pipeline Board with stage movement and summary valuations.
6. **Follow-Up Management**:
   - Multi-channel touchpoints: Call, Meeting, Email, Task.
   - Business rule: Scheduled date cannot be in the past for new/planned activities.
   - Rescheduling and completion workflows with audit logging.
7. **Activity History**:
   - Comprehensive audit and interaction timeline across Customers and Leads.
8. **Role-Based Authorization (RBAC)**:
   - **Admin**: Full system control, user and role administration, and audit logs.
   - **Manager**: Team scope visibility and management reports.
   - **Sales Executive**: Restricted strictly to assigned customers, leads, opportunities, and follow-ups.
9. **Audit Trail**:
   - Append-only logging of Logins, Failed Logins, Logouts, CRUD actions, Lead Conversions, Role changes, and Reschedules.
10. **Reports**:
    - Customer, Lead, Pipeline, Conversion, and User Activity reports with print and export capabilities.
11. **REST API & Swagger**:
    - Documented REST endpoints under `/api/*` utilizing DTOs and standard HTTP status codes (200, 201, 400, 401, 403, 404, 409).
    - Swagger UI available at `/swagger`.

---

## 3. Demo & Evaluation Accounts

The database is pre-seeded with three accounts representing each role (Default password: `Acxiom@2026!`):

| Role | Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiom.com` | `Acxiom@2026!` | Full system administration, user management, audit logs, all records |
| **Manager** | `manager@acxiom.com` | `Acxiom@2026!` | Team pipeline, customer/lead management, team reports |
| **Sales Executive** | `sales@acxiom.com` | `Acxiom@2026!` | Assigned records only (customers, leads, deals, follow-ups) |

---

## 4. Setup & Running Instructions

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher.

### Quick Start (Out-of-the-box with SQLite)
The solution is pre-configured with SQLite enabled by default (`"UseSqlite": true` in `appsettings.json`) so it runs instantly on any machine without requiring local SQL Server setup.

1. Clone and navigate to the project directory:
   ```bash
   git clone https://github.com/vynavipotnuru/AcxiomCRM.git
   cd AcxiomCRM
   ```

2. Run the web application:
   ```powershell
   dotnet run --project AcxiomCRM.Web/AcxiomCRM.Web.csproj
   ```

3. Open your browser and navigate to:
   * **Web App**: [http://localhost:5000](http://localhost:5000) (or the URL printed in the console)
   * **REST API Swagger Docs**: [http://localhost:5000/swagger](http://localhost:5000/swagger)

4. Log in using any of the demo accounts above (or use the one-click demo credentials buttons on the login screen).

### Using SQL Server (Optional)
To use Microsoft SQL Server or LocalDB instead of SQLite:
1. In `AcxiomCRM.Web/appsettings.json`, set:
   ```json
   "UseSqlite": false
   ```
2. Adjust the `"SqlServerConnection"` string if your SQL Server instance name differs.
3. Apply migrations to SQL Server:
   ```powershell
   dotnet ef database update --project AcxiomCRM.Infrastructure/AcxiomCRM.Infrastructure.csproj --startup-project AcxiomCRM.Web/AcxiomCRM.Web.csproj
   ```

---

## 5. Running Automated Tests

Execute the full suite of unit, validation, authorization, and API tests:

```powershell
dotnet test
```

All 16 test suites covering business rules, opportunity calculations, lead conversions, duplicate prevention, and authorization boundaries execute and pass with zero errors.

---

## 6. REST API Endpoint Catalog

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Authenticate user credentials and return role claims |
| `POST` | `/api/auth/logout` | Terminate active user session |
| `GET` | `/api/customers` | Search and list customers (scoped by role) |
| `GET` | `/api/customers/{id}` | Retrieve specific customer details |
| `POST` | `/api/customers` | Create a customer with validation & duplicate checks |
| `PUT` | `/api/customers/{id}` | Update existing customer details |
| `DELETE` | `/api/customers/{id}` | Delete or deactivate customer record |
| `GET` | `/api/leads` | Search and list leads |
| `POST` | `/api/leads` | Create a new lead |
| `GET` | `/api/opportunities` | Search and list sales opportunities |
| `POST` | `/api/opportunities` | Create a new opportunity with business validation |
| `GET` | `/api/followups` | List upcoming and scheduled follow-ups |
| `POST` | `/api/followups` | Schedule a new follow-up activity |
| `GET` | `/api/reports/pipeline` | Retrieve aggregated pipeline analytics data |
