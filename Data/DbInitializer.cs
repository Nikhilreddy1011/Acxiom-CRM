using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roles = { "Admin", "Manager", "SalesExecutive" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Users
        var usersToSeed = new[]
        {
            new { Email = "admin@acxiom.com", Name = "System Administrator", Role = "Admin", Pass = "Admin@12345" },
            new { Email = "manager@acxiom.com", Name = "Sarah Jenkins", Role = "Manager", Pass = "Manager@12345" },
            new { Email = "sales1@acxiom.com", Name = "Alex Rivera", Role = "SalesExecutive", Pass = "Sales@12345" },
            new { Email = "sales2@acxiom.com", Name = "Emily Chen", Role = "SalesExecutive", Pass = "Sales@12345" }
        };

        var userDict = new Dictionary<string, ApplicationUser>();

        foreach (var item in usersToSeed)
        {
            var user = await userManager.FindByEmailAsync(item.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = item.Email,
                    Email = item.Email,
                    FullName = item.Name,
                    Role = item.Role,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var createRes = await userManager.CreateAsync(user, item.Pass);
                if (createRes.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, item.Role);
                }
            }
            userDict[item.Email] = user;
        }

        var adminUser = userDict["admin@acxiom.com"];
        var managerUser = userDict["manager@acxiom.com"];
        var sales1 = userDict["sales1@acxiom.com"];
        var sales2 = userDict["sales2@acxiom.com"];

        // 3. Seed Customers (10+)
        if (!await context.Customers.AnyAsync())
        {
            var customers = new List<Customer>
            {
                new() { CustomerCode = "CUST-1001", CustomerName = "Global Horizon Corp", Email = "contact@globalhorizon.com", Phone = "+1-555-0101", CompanyName = "Global Horizon Corp", Address = "742 Evergreen Terrace", City = "Springfield", State = "IL", Status = "Active", AssignedTo = sales1.Id, CreatedBy = adminUser.Id },
                new() { CustomerCode = "CUST-1002", CustomerName = "Apex Financial Group", Email = "info@apexfinancial.com", Phone = "+1-555-0102", CompanyName = "Apex Financial Group", Address = "120 Wall Street", City = "New York", State = "NY", Status = "Active", AssignedTo = sales2.Id, CreatedBy = adminUser.Id },
                new() { CustomerCode = "CUST-1003", CustomerName = "Quantum Logistics", Email = "ops@quantumlog.com", Phone = "+1-555-0103", CompanyName = "Quantum Logistics", Address = "450 Ocean Drive", City = "Miami", State = "FL", Status = "Active", AssignedTo = sales1.Id, CreatedBy = adminUser.Id },
                new() { CustomerCode = "CUST-1004", CustomerName = "Nexus Health Systems", Email = "billing@nexushealth.org", Phone = "+1-555-0104", CompanyName = "Nexus Health Systems", Address = "89 Medical Center Way", City = "Boston", State = "MA", Status = "Active", AssignedTo = sales2.Id, CreatedBy = managerUser.Id },
                new() { CustomerCode = "CUST-1005", CustomerName = "Vanguard Digital Media", Email = "hello@vanguardmedia.io", Phone = "+1-555-0105", CompanyName = "Vanguard Digital Media", Address = "300 Silicon Alley", City = "San Francisco", State = "CA", Status = "Active", AssignedTo = sales1.Id, CreatedBy = managerUser.Id },
                new() { CustomerCode = "CUST-1006", CustomerName = "Pinnacle Renewable Energy", Email = "procure@pinnaclepower.com", Phone = "+1-555-0106", CompanyName = "Pinnacle Renewable Energy", Address = "150 Windmill Lane", City = "Austin", State = "TX", Status = "Active", AssignedTo = sales2.Id, CreatedBy = adminUser.Id },
                new() { CustomerCode = "CUST-1007", CustomerName = "Crestview Real Estate", Email = "deals@crestviewre.com", Phone = "+1-555-0107", CompanyName = "Crestview Real Estate", Address = "600 Peachtree St", City = "Atlanta", State = "GA", Status = "Active", AssignedTo = sales1.Id, CreatedBy = sales1.Id },
                new() { CustomerCode = "CUST-1008", CustomerName = "Summit Manufacturing Ltd", Email = "supply@summitmfg.com", Phone = "+1-555-0108", CompanyName = "Summit Manufacturing Ltd", Address = "1200 Industrial Parkway", City = "Detroit", State = "MI", Status = "Active", AssignedTo = sales2.Id, CreatedBy = sales2.Id },
                new() { CustomerCode = "CUST-1009", CustomerName = "Starlight Aerospace", Email = "contracts@starlightaero.com", Phone = "+1-555-0109", CompanyName = "Starlight Aerospace", Address = "900 Orbital Way", City = "Seattle", State = "WA", Status = "Active", AssignedTo = sales1.Id, CreatedBy = adminUser.Id },
                new() { CustomerCode = "CUST-1010", CustomerName = "Beacon CyberSecurity", Email = "soc@beaconcyber.com", Phone = "+1-555-0110", CompanyName = "Beacon CyberSecurity", Address = "250 Security Blvd", City = "Reston", State = "VA", Status = "Active", AssignedTo = sales2.Id, CreatedBy = managerUser.Id },
                new() { CustomerCode = "CUST-1011", CustomerName = "Zenith Retail Partners", Email = "info@zenithretail.com", Phone = "+1-555-0111", CompanyName = "Zenith Retail Partners", Address = "410 Retail Hub", City = "Chicago", State = "IL", Status = "Inactive", AssignedTo = sales1.Id, CreatedBy = adminUser.Id }
            };

            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }

        // 4. Seed Leads (10+)
        if (!await context.Leads.AnyAsync())
        {
            var leads = new List<Lead>
            {
                new() { LeadCode = "LEAD-2001", LeadName = "Marcus Brody", Email = "mbrody@archeotech.io", Phone = "+1-555-0201", CompanyName = "ArcheoTech", Source = "Website", Status = "New", Priority = "High", ExpectedValue = 45000, AssignedTo = sales1.Id },
                new() { LeadCode = "LEAD-2002", LeadName = "Elena Rostova", Email = "elena.r@synapsesolutions.net", Phone = "+1-555-0202", CompanyName = "Synapse Solutions", Source = "Referral", Status = "Contacted", Priority = "Medium", ExpectedValue = 28000, AssignedTo = sales2.Id },
                new() { LeadCode = "LEAD-2003", LeadName = "David Kim", Email = "dkim@aurorabiotech.com", Phone = "+1-555-0203", CompanyName = "Aurora BioTech", Source = "Partner", Status = "Qualified", Priority = "High", ExpectedValue = 75000, AssignedTo = sales1.Id },
                new() { LeadCode = "LEAD-2004", LeadName = "Samantha Wright", Email = "swright@urbanarchitects.com", Phone = "+1-555-0204", CompanyName = "Urban Architects", Source = "Cold Call", Status = "Contacted", Priority = "Low", ExpectedValue = 15000, AssignedTo = sales2.Id },
                new() { LeadCode = "LEAD-2005", LeadName = "Liam Gallagher", Email = "liam@oasiscloud.co", Phone = "+1-555-0205", CompanyName = "Oasis Cloud Services", Source = "Advertisement", Status = "Qualified", Priority = "High", ExpectedValue = 92000, AssignedTo = sales1.Id },
                new() { LeadCode = "LEAD-2006", LeadName = "Priya Sharma", Email = "psharma@solarexglobal.com", Phone = "+1-555-0206", CompanyName = "Solarex Global", Source = "Website", Status = "New", Priority = "Medium", ExpectedValue = 35000, AssignedTo = sales2.Id },
                new() { LeadCode = "LEAD-2007", LeadName = "Carlos Mendez", Email = "cmendez@caribbeandist.com", Phone = "+1-555-0207", CompanyName = "Caribbean Distribution", Source = "Referral", Status = "Unqualified", Priority = "Low", ExpectedValue = 8000, AssignedTo = sales1.Id },
                new() { LeadCode = "LEAD-2008", LeadName = "Hannah Abbott", Email = "habbott@botanicaltech.co", Phone = "+1-555-0208", CompanyName = "Botanical Tech", Source = "Website", Status = "Converted", Priority = "High", ExpectedValue = 60000, AssignedTo = sales2.Id },
                new() { LeadCode = "LEAD-2009", LeadName = "Jordan Belfort", Email = "jb@strattonoak.com", Phone = "+1-555-0209", CompanyName = "Stratton Advisory", Source = "Cold Call", Status = "Lost", Priority = "Low", ExpectedValue = 12000, AssignedTo = sales1.Id },
                new() { LeadCode = "LEAD-2010", LeadName = "Tariq Mansoor", Email = "tmansoor@gulflogistics.ae", Phone = "+1-555-0210", CompanyName = "Gulf Logistics Hub", Source = "Partner", Status = "Qualified", Priority = "High", ExpectedValue = 110000, AssignedTo = sales2.Id },
                new() { LeadCode = "LEAD-2011", LeadName = "Chloe Bennett", Email = "cbennett@shielddynamics.com", Phone = "+1-555-0211", CompanyName = "Shield Dynamics", Source = "Website", Status = "New", Priority = "Medium", ExpectedValue = 40000, AssignedTo = sales1.Id }
            };

            await context.Leads.AddRangeAsync(leads);
            await context.SaveChangesAsync();
        }

        // 5. Seed Opportunities (5+)
        if (!await context.Opportunities.AnyAsync())
        {
            var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
            var cust2 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1002");
            var cust3 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1003");
            var cust4 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1004");
            var cust6 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1006");

            var opportunities = new List<Opportunity>
            {
                new() { OpportunityName = "Enterprise Cloud Migration", CustomerId = cust1?.CustomerId, Amount = 120000, Stage = "Negotiation", Probability = 80, ExpectedCloseDate = DateTime.UtcNow.AddDays(25), Status = "Open", AssignedTo = sales1.Id },
                new() { OpportunityName = "Annual Fintech Security Audit", CustomerId = cust2?.CustomerId, Amount = 85000, Stage = "Proposal", Probability = 60, ExpectedCloseDate = DateTime.UtcNow.AddDays(40), Status = "Open", AssignedTo = sales2.Id },
                new() { OpportunityName = "Fleet Tracking IoT Integration", CustomerId = cust3?.CustomerId, Amount = 65000, Stage = "Qualification", Probability = 30, ExpectedCloseDate = DateTime.UtcNow.AddDays(60), Status = "Open", AssignedTo = sales1.Id },
                new() { OpportunityName = "Healthcare Compliance Platform", CustomerId = cust4?.CustomerId, Amount = 150000, Stage = "Won", Probability = 100, ExpectedCloseDate = DateTime.UtcNow.AddDays(-5), Status = "Won", AssignedTo = sales2.Id },
                new() { OpportunityName = "Solar Microgrid Software License", CustomerId = cust6?.CustomerId, Amount = 95000, Stage = "Negotiation", Probability = 75, ExpectedCloseDate = DateTime.UtcNow.AddDays(15), Status = "Open", AssignedTo = sales2.Id },
                new() { OpportunityName = "Legacy CRM Replacement Deal", CustomerId = cust1?.CustomerId, Amount = 40000, Stage = "Lost", Probability = 0, ExpectedCloseDate = DateTime.UtcNow.AddDays(-20), Status = "Lost", AssignedTo = sales1.Id }
            };

            await context.Opportunities.AddRangeAsync(opportunities);
            await context.SaveChangesAsync();
        }

        // 6. Seed FollowUps (5+)
        if (!await context.FollowUps.AnyAsync())
        {
            var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
            var cust2 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1002");
            var lead3 = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-2003");
            var lead5 = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-2005");

            var followUps = new List<FollowUp>
            {
                new() { CustomerId = cust1?.CustomerId, FollowUpDate = DateTime.UtcNow.AddDays(2), FollowUpType = "Meeting", Subject = "Contract Clause Review Meeting", Remarks = "Review SLA and payment terms with executive team", Status = "Planned", AssignedTo = sales1.Id },
                new() { CustomerId = cust2?.CustomerId, FollowUpDate = DateTime.UtcNow.AddDays(4), FollowUpType = "Demo", Subject = "Security Audit Suite Demo", Remarks = "Present live demonstration to CISO", Status = "Planned", AssignedTo = sales2.Id },
                new() { LeadId = lead3?.LeadId, FollowUpDate = DateTime.UtcNow.AddDays(1), FollowUpType = "Call", Subject = "Discovery Call: Requirements Scope", Remarks = "Confirm budget authorization and decision committee", Status = "Planned", AssignedTo = sales1.Id },
                new() { LeadId = lead5?.LeadId, FollowUpDate = DateTime.UtcNow.AddDays(3), FollowUpType = "Email", Subject = "Architecture Whitepaper & Case Studies", Remarks = "Send technical documentation for multi-cloud migration", Status = "Planned", AssignedTo = sales1.Id },
                new() { CustomerId = cust1?.CustomerId, FollowUpDate = DateTime.UtcNow.AddDays(-2), FollowUpType = "Call", Subject = "Initial scoping discussion", Remarks = "Client confirmed budget is approved for Q4", Status = "Completed", AssignedTo = sales1.Id },
                new() { LeadId = lead3?.LeadId, FollowUpDate = DateTime.UtcNow.AddDays(-4), FollowUpType = "Call", Subject = "First touchpoint follow-up", Remarks = "Client was out of office, scheduled callback", Status = "Missed", AssignedTo = sales2.Id }
            };

            await context.FollowUps.AddRangeAsync(followUps);
            await context.SaveChangesAsync();
        }

        // 7. Seed Activities
        if (!await context.Activities.AnyAsync())
        {
            var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
            var cust2 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1002");
            var lead1 = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-2001");

            var activities = new List<Activity>
            {
                new() { ActivityType = "Call", Subject = "Introductory Discovery Call", Description = "Discussed business goals, pain points with existing tooling, and project timeline.", ActivityDate = DateTime.UtcNow.AddDays(-7), CustomerId = cust1?.CustomerId, AssignedTo = sales1.Id, Status = "Completed" },
                new() { ActivityType = "Meeting", Subject = "Solution Architecture Presentation", Description = "Delivered 45-minute presentation to IT Director and VP of Operations.", ActivityDate = DateTime.UtcNow.AddDays(-3), CustomerId = cust1?.CustomerId, AssignedTo = sales1.Id, Status = "Completed" },
                new() { ActivityType = "Email", Subject = "Pricing Proposal Sent", Description = "Sent tiered pricing proposal with 3-year term discounts.", ActivityDate = DateTime.UtcNow.AddDays(-1), CustomerId = cust2?.CustomerId, AssignedTo = sales2.Id, Status = "Completed" },
                new() { ActivityType = "Task", Subject = "Review Competitor Benchmark Report", Description = "Prepared comparison sheet against legacy vendor for client negotiation.", ActivityDate = DateTime.UtcNow.AddDays(-2), LeadId = lead1?.LeadId, AssignedTo = sales1.Id, Status = "Completed" }
            };

            await context.Activities.AddRangeAsync(activities);
            await context.SaveChangesAsync();
        }

        // 8. Seed Audit Logs
        if (!await context.AuditLogs.AnyAsync())
        {
            var auditLogs = new List<AuditLog>
            {
                new() { UserId = adminUser.Id, UserName = adminUser.Email, Action = "System Initialized", EntityName = "System", RecordId = "SYSTEM", CreatedDate = DateTime.UtcNow.AddDays(-10), IpAddress = "127.0.0.1" },
                new() { UserId = adminUser.Id, UserName = adminUser.Email, Action = "User Created", EntityName = "ApplicationUser", RecordId = sales1.Id, NewValue = $"Created user {sales1.Email} with role SalesExecutive", CreatedDate = DateTime.UtcNow.AddDays(-9), IpAddress = "127.0.0.1" },
                new() { UserId = adminUser.Id, UserName = adminUser.Email, Action = "Customer Created", EntityName = "Customer", RecordId = "CUST-1001", NewValue = "Global Horizon Corp", CreatedDate = DateTime.UtcNow.AddDays(-8), IpAddress = "127.0.0.1" },
                new() { UserId = sales1.Id, UserName = sales1.Email, Action = "Opportunity Created", EntityName = "Opportunity", RecordId = "Enterprise Cloud Migration", NewValue = "Amount: $120,000 | Stage: Qualification", CreatedDate = DateTime.UtcNow.AddDays(-6), IpAddress = "192.168.1.10" },
                new() { UserId = sales1.Id, UserName = sales1.Email, Action = "Opportunity Updated", EntityName = "Opportunity", RecordId = "Enterprise Cloud Migration", OldValue = "Stage: Qualification", NewValue = "Stage: Negotiation", CreatedDate = DateTime.UtcNow.AddDays(-2), IpAddress = "192.168.1.10" }
            };

            await context.AuditLogs.AddRangeAsync(auditLogs);
            await context.SaveChangesAsync();
        }
    }
}
