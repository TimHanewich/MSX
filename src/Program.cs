using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MSX
{
    public class Program
    {
        private static readonly string StorageDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSX");
        private static readonly string CookiePath = Path.Combine(StorageDir, "cookie.txt");

        public static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            string command = args[0].ToLowerInvariant();

            try
            {
                switch (command)
                {
                    case "auth":
                        return HandleAuth(args);
                    case "whoami":
                        return await HandleWhoAmI();
                    case "users":
                        return await HandleSearchUsers(args);
                    case "accounts":
                        return await HandleSearchAccounts(args);
                    case "opps":
                        return await HandleOpportunities(args);
                    case "tasks":
                        return await HandleTasks(args);
                    case "query":
                        return await HandleQuery(args);
                    case "help":
                    case "--help":
                    case "-h":
                        PrintUsage();
                        return 0;
                    default:
                        Console.Error.WriteLine($"Unknown command: {command}");
                        PrintUsage();
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static void PrintUsage()
        {
            string version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown";
            Console.WriteLine($"MSX v{version} - Microsoft CRM CLI Tool");
            Console.WriteLine("https://github.com/TimHanewich/MSX");
            Console.WriteLine();
            Console.WriteLine("Usage: msx <command> [arguments]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  auth set <cookie>                       Save your MSX authentication cookie");
            Console.WriteLine("  auth clear                              Remove saved cookie");
            Console.WriteLine("  auth show                               Display the saved cookie");
            Console.WriteLine("  auth path                               Show cookie file path");
            Console.WriteLine("  whoami                                  Get your system user ID");
            Console.WriteLine("  users <name>                            Search users by name");
            Console.WriteLine("  accounts <search>                       Search accounts by name");
            Console.WriteLine("  opps search <account_id> <search>       Search opportunities for an account");
            Console.WriteLine("  opps mine                               Get your associated opportunities");
            Console.WriteLine("  opps user <user_id>                     Get opportunities for a specific user");
            Console.WriteLine("  tasks mine                              Get your recent tasks");
            Console.WriteLine("  tasks user <user_id>                    Get tasks for a specific user");
            Console.WriteLine("  tasks create <title> <desc> <date>      Create a task (date: yyyy-MM-dd)");
            Console.WriteLine("       [--account <id>]                     Tie task to an account");
            Console.WriteLine("       [--opportunity <id>]                 Tie task to an opportunity");
            Console.WriteLine("  query <odata_query>                     Run a raw OData query");
        }

        // ── Auth ──

        private static int HandleAuth(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: msx auth set <cookie> | msx auth clear | msx auth show");
                return 1;
            }

            string sub = args[1].ToLowerInvariant();

            switch (sub)
            {
                case "set":
                    if (args.Length < 3)
                    {
                        Console.Error.WriteLine("Usage: msx auth set <cookie>");
                        return 1;
                    }
                    Directory.CreateDirectory(StorageDir);
                    File.WriteAllText(CookiePath, args[2]);
                    Console.WriteLine("Cookie saved.");
                    return 0;

                case "clear":
                    if (File.Exists(CookiePath))
                        File.Delete(CookiePath);
                    Console.WriteLine("Cookie cleared.");
                    return 0;

                case "show":
                    if (!File.Exists(CookiePath))
                    {
                        Console.Error.WriteLine("No cookie saved. Run: msx auth set <cookie>");
                        return 1;
                    }
                    Console.WriteLine(File.ReadAllText(CookiePath));
                    return 0;

                case "path":
                    Console.WriteLine(CookiePath);
                    return 0;

                default:
                    Console.Error.WriteLine("Usage: msx auth set <cookie> | msx auth clear | msx auth show");
                    return 1;
            }
        }

        // ── Client helper ──

        private static MsxClient GetClient()
        {
            if (!File.Exists(CookiePath))
                throw new Exception("No cookie saved. Run: msx auth set <cookie>");
            string cookie = File.ReadAllText(CookiePath).Trim();
            return new MsxClient(cookie);
        }

        // ── WhoAmI ──

        private static async Task<int> HandleWhoAmI()
        {
            var client = GetClient();
            string userId = await client.WhoAmIAsync();
            Console.WriteLine(userId);
            return 0;
        }

        // ── Users ──

        private static async Task<int> HandleSearchUsers(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: msx users <name>");
                return 1;
            }
            var client = GetClient();
            JArray users = await client.SearchUsersAsync(args[1]);
            Console.WriteLine(users.ToString(Formatting.Indented));
            return 0;
        }

        // ── Accounts ──

        private static async Task<int> HandleSearchAccounts(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: msx accounts <search>");
                return 1;
            }
            var client = GetClient();
            JArray accounts = await client.SearchAccountsAsync(args[1]);
            Console.WriteLine(accounts.ToString(Formatting.Indented));
            return 0;
        }

        // ── Opportunities ──

        private static async Task<int> HandleOpportunities(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: msx opps search <account_id> <search>");
                Console.Error.WriteLine("       msx opps mine");
                Console.Error.WriteLine("       msx opps user <user_id>");
                return 1;
            }

            string sub = args[1].ToLowerInvariant();
            var client = GetClient();

            switch (sub)
            {
                case "search":
                    if (args.Length < 4)
                    {
                        Console.Error.WriteLine("Usage: msx opps search <account_id> <search_term>");
                        return 1;
                    }
                    JArray opps = await client.SearchOpportunitiesAsync(args[2], args[3]);
                    Console.WriteLine(opps.ToString(Formatting.Indented));
                    return 0;

                case "mine":
                    string myId = await client.WhoAmIAsync();
                    JArray myOpps = await client.GetAssociatedOpportunitiesAsync(myId);
                    Console.WriteLine(myOpps.ToString(Formatting.Indented));
                    return 0;

                case "user":
                    if (args.Length < 3)
                    {
                        Console.Error.WriteLine("Usage: msx opps user <user_id>");
                        return 1;
                    }
                    JArray userOpps = await client.GetAssociatedOpportunitiesAsync(args[2]);
                    Console.WriteLine(userOpps.ToString(Formatting.Indented));
                    return 0;

                default:
                    Console.Error.WriteLine("Unknown opps subcommand. Use: search, mine, user");
                    return 1;
            }
        }

        // ── Tasks ──

        private static async Task<int> HandleTasks(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: msx tasks mine");
                Console.Error.WriteLine("       msx tasks user <user_id>");
                Console.Error.WriteLine("       msx tasks create <title> <description> <date> [--category <category>] [--account <id>] [--opportunity <id>]");
                return 1;
            }

            string sub = args[1].ToLowerInvariant();
            var client = GetClient();

            switch (sub)
            {
                case "mine":
                    string myId = await client.WhoAmIAsync();
                    JArray myTasks = await client.GetTasksForUserAsync(myId);
                    Console.WriteLine(myTasks.ToString(Formatting.Indented));
                    return 0;

                case "user":
                    if (args.Length < 3)
                    {
                        Console.Error.WriteLine("Usage: msx tasks user <user_id>");
                        return 1;
                    }
                    JArray userTasks = await client.GetTasksForUserAsync(args[2]);
                    Console.WriteLine(userTasks.ToString(Formatting.Indented));
                    return 0;

                case "create":
                    return await HandleTaskCreate(args);

                default:
                    Console.Error.WriteLine("Unknown tasks subcommand. Use: mine, user, create");
                    return 1;
            }
        }

        private static async Task<int> HandleTaskCreate(string[] args)
        {
            // msx tasks create <title> <description> <date> [--category <category>] [--account <id>] [--opportunity <id>]
            if (args.Length < 5)
            {
                Console.Error.WriteLine("Usage: msx tasks create <title> <description> <date> [--category <category>] [--account <id>] [--opportunity <id>]");
                return 1;
            }

            string title = args[2];
            string description = args[3];

            if (!DateTime.TryParse(args[4], out DateTime date))
            {
                Console.Error.WriteLine($"Invalid date: {args[4]}. Use a format like yyyy-MM-dd.");
                return 1;
            }

            // Parse flags
            string? accountId = null;
            string? opportunityId = null;
            TaskCategory? category = null;
            for (int i = 5; i < args.Length - 1; i++)
            {
                if (args[i] == "--account")
                    accountId = args[++i];
                else if (args[i] == "--opportunity")
                    opportunityId = args[++i];
                else if (args[i] == "--category")
                {
                    if (!Enum.TryParse<TaskCategory>(args[++i], ignoreCase: true, out var parsed))
                    {
                        Console.Error.WriteLine($"Invalid category: {args[i]}. Valid values: {string.Join(", ", Enum.GetNames<TaskCategory>())}");
                        return 1;
                    }
                    category = parsed;
                }
            }

            var client = GetClient();
            await client.CreateTaskAsync(title, description, date, category, accountId, opportunityId);
            Console.WriteLine("Task created.");
            return 0;
        }

        // ── Query ──

        private static async Task<int> HandleQuery(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: msx query <odata_query>");
                return 1;
            }

            // Join remaining args in case the query has spaces
            string query = string.Join(" ", args, 1, args.Length - 1);
            var client = GetClient();
            JArray result = await client.RunQueryAsync(query);
            Console.WriteLine(result.ToString(Formatting.Indented));
            return 0;
        }
    }
}