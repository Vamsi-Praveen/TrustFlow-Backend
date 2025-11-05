using TrustFlow.Core.Data;
using TrustFlow.Core.Helpers;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Seed
{
    public class DbSeeder
    {
        private readonly ApplicationContext _context;
        private readonly PasswordHelper _passwordHelper;

        public DbSeeder(ApplicationContext context,PasswordHelper passwordHelper)
        {
            _context = context;
            _passwordHelper = passwordHelper;
        }

        public async Task SeedAsync()
        {
            Console.WriteLine("Checking for seed data...");

            await SeedPriorities();
            await SeedSeverities();
            await SeedTypes();
            await SeedAdminUser();

            Console.WriteLine("Seed data check complete.");
        }


        private async Task SeedPriorities()
        {
            if (await _context.IssuePriorities.EstimatedDocumentCountAsync() == 0)
            {
                Console.WriteLine("Seeding Bug Priorities...");
                var priorities = new List<IssuePriority>
                {
                    new IssuePriority { Name = "Critical", Description = "Blocks essential functionality.", Order = 1, IsDefault = false },
                    new IssuePriority { Name = "High", Description = "Significant impact, but not a complete block.", Order = 2, IsDefault = false },
                    new IssuePriority { Name = "Medium", Description = "Standard priority, should be addressed in due course.", Order = 3, IsDefault = true }, // Default
                    new IssuePriority { Name = "Low", Description = "Minor impact, can be addressed later.", Order = 4, IsDefault = false }
                };
                await _context.IssuePriorities.InsertManyAsync(priorities);
                Console.WriteLine($"Seeded {priorities.Count} Bug Priorities.");
            }
        }


        private async Task SeedSeverities()
        {
            if (await _context.IssueSeverities.EstimatedDocumentCountAsync() == 0)
            {
                Console.WriteLine("Seeding Bug Severities...");
                var severities = new List<IssueSeverity>
                {
                    new IssueSeverity { Name = "Blocker", Description = "System completely unusable or core feature broken.", Order = 1, IsDefault = false },
                    new IssueSeverity { Name = "Major", Description = "Major loss of function or critical data error.", Order = 2, IsDefault = false },
                    new IssueSeverity { Name = "Minor", Description = "Minor loss of function or UI defect.", Order = 3, IsDefault = true },
                    new IssueSeverity { Name = "Cosmetic", Description = "Aesthetic issue, no functional impact.", Order = 4, IsDefault = false }
                };
                await _context.IssueSeverities.InsertManyAsync(severities);
                Console.WriteLine($"Seeded {severities.Count} Bug Severities.");
            }
        }


        private async Task SeedTypes()
        {
            if (await _context.IssueTypes.EstimatedDocumentCountAsync() == 0)
            {
                Console.WriteLine("Seeding Bug Types...");
                var types = new List<IssueType>
                {
                    new IssueType { Name = "Bug", Description = "A defect or error in the software.", IsDefault = true },
                    new IssueType { Name = "Feature Request", Description = "A new piece of functionality.", IsDefault = false },
                    new IssueType { Name = "Task", Description = "A unit of work to be done.", IsDefault = false },
                    new IssueType { Name = "Improvement", Description = "Enhancement to existing functionality.", IsDefault = false }
                };
                await _context.IssueTypes.InsertManyAsync(types);
                Console.WriteLine($"Seeded {types.Count} Bug Types.");
            }
        }


        private async Task SeedAdminUser()
        {
            if (await _context.Users.EstimatedDocumentCountAsync() == 0)
            {
                Console.WriteLine("Seeding Admin User...");

                string adminPassword = "AdminPassword123!";
                string hashedPassword = _passwordHelper.HashPassword(adminPassword);

                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@bugtracker.com",
                    PasswordHash = hashedPassword,
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = "Admin",
                    IsActive = true
                };

                await _context.Users.InsertOneAsync(adminUser);
                Console.WriteLine("Seeded Admin User 'admin'.");
            }
        }


    }
}
