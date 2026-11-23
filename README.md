# AcxiomCRM Enterprise — Role-Based Customer Relationship Management

[![ASP.NET Core 8.0](https://img.shields.io/badge/ASP.NET%20Core-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-SQLite-512BD4.svg)](https://learn.microsoft.com/ef/core/)
[![Bootstrap 5](https://img.shields.io/badge/Bootstrap-5.3-7952B3.svg)](https://getbootstrap.com/)
[![Swagger OpenAPI](https://img.shields.io/badge/Swagger-OpenAPI%203.0-85EA2D.svg)](https://swagger.io/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

AcxiomCRM is a role-governed Customer Relationship Management (CRM) web application designed for enterprise sales operations. It provides customer tracking, lead conversion workflows, weighted revenue pipeline management, scheduled follow-up cadences, security audit trails, and interactive Chart.js analytics.

---

## 1. Key Features

- **Role-Based Access Control (RBAC)**: Strict server-side permission enforcement for `Admin`, `Manager`, and `SalesExecutive`.
  - **Admin**: Full system management, user & role provisioning, customer/lead/opportunity access, system audit trails, and all reports.
  - **Manager**: Team-wide pipeline management, opportunity assignments, team follow-ups, and operational reporting.
  - **SalesExecutive**: Access strictly limited to self-assigned accounts, leads, deals, activities, and personal sales metrics.
- **Customer Management**: Complete CRUD operations, unique email/phone validation, detailed history timeline, and status toggle.
- **Lead Pipeline & Conversion**: Multi-stage lead management (`New`, `Contacted`, `Qualified`, `Unqualified`, `Converted`, `Lost`) with automated conversion into permanent Customer accounts and Opportunity deals.
- **Opportunity & Revenue Pipeline**: Stage progression (`Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`), real-time weighted pipeline calculations (`Amount × Probability ÷ 100`), and close-date validation.
- **Follow-up & Activity Tracking**: Scheduling cadences (Calls, Meetings, Emails, Demos) with pending/overdue filters and outcome logging.
- **Executive Analytics Dashboard**: Real-time KPI summary cards, interactive Chart.js visualizations (Lead Status Doughnut, Pipeline Stage Bar, Monthly Won Trend Line), and date filtering.
- **Security & Audit Trail**: Centralized logging capturing login successes/failures, record mutations, field diffs (previous vs. new values), timestamps, and IP addresses.
- **Reporting Module**: 8 comprehensive reports with direct one-click CSV streaming.
- **Secured REST APIs**: Documented with Swagger OpenAPI 3.0 at `/swagger`.
- **OmniRoute AI Integration**: Configurable OpenAI-compatible assistant for account health synthesis and pipeline insights with offline fallback.

---

## 2. Technology Stack

- **Backend**: C# 12, ASP.NET Core 8.0 MVC & Web API, ASP.NET Core Identity.
- **Data Persistence**: Entity Framework Core 8.0, SQLite (`acxiomcrm.db`), Data Annotations.
- **Frontend**: Razor Views, HTML5, CSS3, Bootstrap 5.3, Bootstrap Icons, Chart.js 4.4.
- **API Documentation**: Swashbuckle Swagger UI.
- **AI Gateway (Optional)**: OmniRoute / OpenAI-compatible endpoint (`http://localhost:20128/v1`).

---

## 3. Project Architecture & Directory Structure

```text
AcxiomCRM/
├── Controllers/
│   ├── AccountController.cs       # Authentication, lockout, registration
│   ├── HomeController.cs          # Dashboard KPIs, chart endpoints, AI synthesis
│   ├── CustomersController.cs     # Customer CRUD & account history
│   ├── LeadsController.cs         # Lead qualification & conversion workflows
│   ├── OpportunitiesController.cs # Revenue pipeline & stage management
│   ├── FollowUpsController.cs     # Follow-up scheduling & status tracking
│   ├── ActivitiesController.cs    # Touchpoint timeline (calls, meetings, tasks)
│   ├── UsersController.cs         # Admin user & role provisioning
│   ├── AuditLogsController.cs     # Admin operational & security audit log
│   ├── ReportsController.cs       # 8 reporting views & CSV export engine
│   └── Api/                       # Secured REST API Controllers
│       ├── AuthController.cs
│       ├── CustomersApiController.cs
│       ├── LeadsApiController.cs
│       ├── OpportunitiesApiController.cs
│       ├── FollowUpsApiController.cs
│       └── ReportsApiController.cs
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DB context & constraints
│   └── DbInitializer.cs           # Database seeding (roles, users, sample data)
├── Models/
│   ├── ApplicationUser.cs         # Identity user extension
│   ├── Customer.cs                # Customer entity
│   ├── Lead.cs                    # Sales lead entity
│   ├── Opportunity.cs             # Revenue opportunity entity
│   ├── FollowUp.cs                # Follow-up task entity
│   ├── Activity.cs                # Activity touchpoint entity
│   ├── AuditLog.cs                # Audit record entity
│   └── Dto/                       # REST API Data Transfer Objects
├── Services/
│   ├── IAuditService.cs           # Audit logging contract
│   ├── AuditService.cs            # Context-aware audit implementation
│   ├── IOmniRouteService.cs       # AI integration abstraction
│   └── OmniRouteService.cs        # OmniRoute client with rule-based fallback
├── ViewModels/                    # Strongly-typed Razor ViewModels
├── Views/                         # Responsive Bootstrap 5 Razor views
├── Properties/                    # Launch profiles
├── appsettings.json               # Application configuration
├── Program.cs                     # Service injection, security middleware, startup
└── README.md
```

---

## 4. Pre-Configured Demo Credentials

The application automatically provisions four sample accounts with diverse roles and permissions upon first launch:

| Role | Email Address | Password | Permissions Scope |
|---|---|---|---|
| **Admin** | `admin@acxiom.com` | `Admin@12345` | Full system access, User/Role Admin, Audit Logs, All Reports |
| **Manager** | `manager@acxiom.com` | `Manager@12345` | Team pipeline, opportunity management, operational reports |
| **SalesExecutive** | `sales1@acxiom.com` | `Sales@12345` | Assigned records only (Customers, Leads, Deals, Tasks) |
| **SalesExecutive** | `sales2@acxiom.com` | `Sales@12345` | Assigned records only (Customers, Leads, Deals, Tasks) |

---

## 5. Getting Started & Installation

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Clone & Run
```bash
# Clone the repository
git clone https://github.com/Nikhilreddy1011/Acxiom-CRM.git
cd Acxiom-CRM

# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

Access the web interface at:
- Web App: `http://localhost:5000` or `https://localhost:5001`
- Swagger REST API: `http://localhost:5000/swagger`

---

## 6. REST API Endpoints

All endpoints require authentication via session cookie or bearer token:

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/login` | Authenticate API client and establish session |
| `POST` | `/api/auth/logout` | Terminate session |
| `GET` | `/api/customers` | Retrieve scoped customer accounts |
| `GET` | `/api/customers/{id}` | Retrieve specific customer record |
| `POST` | `/api/customers` | Create new customer account |
| `PUT` | `/api/customers/{id}` | Update existing customer record |
| `DELETE` | `/api/customers/{id}` | Deactivate customer account |
| `GET` | `/api/leads` | Retrieve sales leads |
| `POST` | `/api/leads` | Create new sales lead |
| `GET` | `/api/opportunities` | Retrieve opportunity pipeline |
| `POST` | `/api/opportunities` | Create opportunity deal |
| `GET` | `/api/followups` | Retrieve follow-up schedule |
| `POST` | `/api/followups` | Schedule new follow-up |
| `GET` | `/api/reports/pipeline` | Pipeline summary metrics and stage breakdown |

---

## 7. OmniRoute AI Gateway Integration (Optional)

AcxiomCRM integrates with an OmniRoute or OpenAI-compatible endpoint to generate automated account summaries and sales recommendations.

### Configuration (`appsettings.json` or Environment Variables)
```json
"OmniRoute": {
  "BaseUrl": "http://localhost:20128/v1",
  "Model": "auto"
}
```
Set environment variable `OMNIROUTE_API_KEY` to provide your API key. If the AI gateway is offline, AcxiomCRM seamlessly falls back to local heuristic synthesis.

---

## 8. Security & Compliance Features

- **Password Policy**: Minimum 6 characters with mandatory uppercase, lowercase, and numeric digits.
- **Account Lockout**: Automatically locks user accounts for 15 minutes after 5 consecutive failed login attempts.
- **CSRF Protection**: ASP.NET Core anti-forgery tokens enforced on all POST forms.
- **SQL Injection Prevention**: Parameterized EF Core queries throughout.
- **Audit Logging**: Immutable operational log tracking user IDs, actions, entity changes, and client IP addresses.

---

## 9. License

This project is licensed under the MIT License.
