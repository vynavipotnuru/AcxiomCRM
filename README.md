# AcxiomCRM

**Enterprise Sales & Customer Relationship Management Web Application**  
*Built with ASP.NET Core 8.0 MVC, Entity Framework Core, ASP.NET Core Identity, Bootstrap 5, and Chart.js.*

---

## 1. Architectural Overview

AcxiomCRM follows a strict **Clean Layered Architecture** separating presentation, business logic, entities, and data persistence:

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
   - Lead capture, source, status progression (`New` → `Contacted` → `Qualified` → `Converted` / `Lost`).
   - Server-enforced status transition engine.
   - **Lead Conversion Workflow**: Converts qualified leads directly into active Customers and optional Opportunities.
5. **Opportunity Management & Pipeline**:
   - Stages: `Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`.
   - Business rules enforced server-side: Amount > 0, Probability 0–100%, future close date for active deals.
   - Weighted pipeline calculated dynamically: Amount × (Probability / 100).
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

*(Quick-fill buttons for each account are also available directly on the login screen).*

---

## 4. Setup & Running Instructions

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher.

### Quick Start (Out-of-the-box with SQLite)
The solution is pre-configured with SQLite enabled by default (`"UseSqlite": true` in `appsettings.json`) so it runs instantly on any machine without requiring local SQL Server setup.

1. Open a terminal in the solution directory:
   ```powershell
   cd C:\Users\vynav\.gemini\antigravity\scratch\AcxiomCRM
