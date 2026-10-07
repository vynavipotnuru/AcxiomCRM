# AcxiomCRM

**Enterprise Sales & Customer Relationship Management Web Application**

Built with **ASP.NET Core 8 MVC, Entity Framework Core, ASP.NET Core Identity, Bootstrap 5, and Chart.js**.

## Demo & Documentation

* **Live Demo:** https://diploma-sticker-avon-accurate.trycloudflare.com

### Demo Credentials

| Role            | Email                | Password       |
| --------------- | -------------------- | -------------- |
| Admin           | `admin@acxiom.com`   | `Acxiom@2026!` |
| Manager         | `manager@acxiom.com` | `Acxiom@2026!` |
| Sales Executive | `sales@acxiom.com`   | `Acxiom@2026!` |

## Summary

AcxiomCRM is a **role-based CRM system** designed to manage customers, leads, sales opportunities, follow-ups, activities, and business reports.

It follows a **Clean Layered Architecture** with separate **Domain, Application, Infrastructure, Web, and Test** projects.

## Key Features

* Authentication with ASP.NET Core Identity
* Role-Based Access Control — Admin, Manager, Sales Executive
* Customer and Lead Management
* Lead-to-Customer Conversion
* Opportunity and Sales Pipeline Management
* Follow-ups and Activity Tracking
* Dashboard with KPIs and Chart.js analytics
* Audit Trail for important system actions
* Customer, Lead, Pipeline and Conversion Reports
* REST API with Swagger documentation
* Business-rule validation and automated tests

## Architecture

```text
AcxiomCRM
├── Domain          → Entities & Business Rules
├── Application     → Services, DTOs & Interfaces
├── Infrastructure  → EF Core, Identity & Database
├── Web             → MVC, API & UI
└── Tests            → Unit & Authorization Tests
```

## Tech Stack

**Backend:** ASP.NET Core 8, C#
**Database:** SQLite / SQL Server
**ORM:** Entity Framework Core
**Authentication:** ASP.NET Core Identity
**Frontend:** Razor Views, Bootstrap 5, JavaScript
**Charts:** Chart.js
**Testing:** xUnit, Moq, EF Core In-Memory
**API:** REST + Swagger

## Run Locally

### Prerequisites

* .NET 8 SDK

```bash
git clone https://github.com/vynavipotru/AcxiomCRM.git
cd AcxiomCRM
dotnet run --project AcxiomCRM.Web/AcxiomCRM.Web.csproj
```

Open the URL shown in the terminal and log in using one of the demo accounts.

### Run Tests

```bash
dotnet test
```

## Main API Endpoints

| Method | Endpoint                | Purpose             |
| ------ | ----------------------- | ------------------- |
| POST   | `/api/auth/login`       | User authentication |
| GET    | `/api/customers`        | Customer listing    |
| POST   | `/api/customers`        | Create customer     |
| GET    | `/api/leads`            | Lead listing        |
| POST   | `/api/leads`            | Create lead         |
| GET    | `/api/opportunities`    | Opportunity listing |
| POST   | `/api/opportunities`    | Create opportunity  |
| GET    | `/api/followups`        | Follow-up listing   |
| POST   | `/api/followups`        | Schedule follow-up  |
| GET    | `/api/reports/pipeline` | Pipeline analytics  |
