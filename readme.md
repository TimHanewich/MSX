# MSX
A command-line tool for interfacing with [MSX](https://microsoftsales.crm.dynamics.com), Microsoft's internal CRM system, powered by Dynamics 365. Run a single command, get a result — no interactive menus.

## Installing
Download `msx.exe` to begin using this tool below:
|Version|Note|
|-|-|
|[0.1.0](https://github.com/TimHanewich/MSX/releases/download/0.1.0/MSX.exe)|First release|
|[0.2.0](https://github.com/TimHanewich/MSX/releases/download/0.2.0/MSX.exe)|Can specify a *Task Category* when creating new tasks and capturing *forecast comments* when pulling down opportunities.|

## Authentication
MSX authenticates with a browser cookie. Grab it once and save it:

```bash
msx auth set "<cookie_value>"
```

The cookie is stored at `%LocalAppData%\MSX\cookie.txt` (Windows) or `~/.local/share/MSX/cookie.txt` (macOS/Linux). When it expires, just run `auth set` again with a fresh one.

## Commands

| Command | Description |
|---|---|
| `msx auth set <cookie>` | Save your authentication cookie |
| `msx auth show` | Display the saved cookie |
| `msx auth path` | Show the cookie file path |
| `msx auth clear` | Remove the saved cookie |
| `msx auth check` | Check if a cookie is saved |
| `msx whoami` | Get your system user GUID |
| `msx users <name>` | Search users by name |
| `msx accounts <search>` | Search accounts by name |
| `msx opps search <account_id> <search>` | Search open opportunities for an account |
| `msx opps mine` | Get your associated opportunities (includes forecast comments) |
| `msx opps user <user_id>` | Get opportunities for a specific user (includes forecast comments) |
| `msx tasks mine` | List your recent tasks (defaults to last 30 days) |
| `msx tasks mine --after <date> --before <date>` | List your tasks in a date range (max 12 months) |
| `msx tasks user <user_id>` | List tasks for a specific user (defaults to last 30 days) |
| `msx tasks user <user_id> --after <date> --before <date>` | List tasks for a user in a date range |
| `msx tasks create <title> <desc> <date> [opts]` | Create a task (see options below) |
| `msx query <odata_query>` | Run a raw OData query |

## Task Creation Options

`msx tasks create <title> <description> <date> [--category <category>] [--account <id>] [--opportunity <id>]`

| Option | Description |
|---|---|
| `--category <category>` | Task category (see values below) |
| `--account <id>` | Link the task to an account |
| `--opportunity <id>` | Link the task to an opportunity |

### Task Categories
| Value | Name |
|---|---|
| 606820000 | ACE |
| 606820001 | CrossSegment |
| 606820002 | CrossWorkload |
| 606820003 | PostSales |
| 606820004 | TechSupport |
| 606820005 | TechnicalCloseWinPlan |
| 861980000 | CustomerEngagement |
| 861980001 | Workshop |
| 861980002 | Demo |
| 861980003 | NegotiatePricing |
| 861980004 | ArchitectureDesignSession |
| 861980005 | PoCPilot |
| 861980006 | BlockerEscalation |
| 861980007 | ConsumptionPlan |
| 861980008 | Briefing |
| 861980009 | RFPRFI |
| 861980010 | CallBackRequested |
| 861980011 | NewPartnerRequest |
| 861980012 | Internal |
| 861980013 | ExternalCoCreationOfValue |

## Usage Examples

```bash
# Who am I?
msx whoami

# Find an account
msx accounts "Contoso"

# Search opportunities under that account
msx opps search "b1c2d3e4-..." "Azure"

# List my open opportunities
msx opps mine

# View my recent tasks (last 30 days)
msx tasks mine

# View my tasks in a specific date range
msx tasks mine --after 2026-01-01 --before 2026-03-31

# View tasks for another user
msx tasks user "a1b2c3d4-..."

# View tasks for another user in a date range
msx tasks user "a1b2c3d4-..." --after 2026-01-01 --before 2026-03-31

# Create a task tied to an account
msx tasks create "Follow up call" "Discuss renewal timeline" 2026-04-01 --category CustomerEngagement --account "b1c2d3e4-..."

# Create a task tied to an opportunity
msx tasks create "Send proposal" "Draft and send SOW" 2026-04-05 --category Demo --opportunity "a9b8c7d6-..."

# Create a task without a category
msx tasks create "Quick note" "Internal reminder" 2026-04-10

# Look up a colleague
msx users "Jane Smith"

# Run a raw OData query
msx query "contacts?\$top=5&\$select=fullname,emailaddress1"
```

## Building
```
dotnet publish MSX.csproj -c Release --self-contained true
```