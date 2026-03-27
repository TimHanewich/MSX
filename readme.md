# MSX
A command-line tool for interfacing with [MSX](https://microsoftsales.crm.dynamics.com), Microsoft's internal CRM system, powered by Dynamics 365. Run a single command, get a result — no interactive menus.

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
| `msx whoami` | Get your system user GUID |
| `msx users <name>` | Search users by name |
| `msx accounts <search>` | Search accounts by name |
| `msx opps search <account_id> <search>` | Search open opportunities for an account |
| `msx opps mine` | Get your associated opportunities |
| `msx opps user <user_id>` | Get opportunities for a specific user |
| `msx tasks` | List your recent tasks |
| `msx tasks create <title> <desc> <date> [opts]` | Create a task |
| `msx query <odata_query>` | Run a raw OData query |

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

# View my recent tasks
msx tasks

# Create a task tied to an account
msx tasks create "Follow up call" "Discuss renewal timeline" 2026-04-01 --account "b1c2d3e4-..."

# Create a task tied to an opportunity
msx tasks create "Send proposal" "Draft and send SOW" 2026-04-05 --opportunity "a9b8c7d6-..."

# Look up a colleague
msx users "Jane Smith"

# Run a raw OData query
msx query "contacts?\$top=5&\$select=fullname,emailaddress1"
```

## Building
```
dotnet publish MSX.csproj -c Release --self-contained true
```
