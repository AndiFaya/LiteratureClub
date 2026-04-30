using BookSwap.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context     = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger      = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // ── Roles ──────────────────────────────────────────────────
                foreach (var role in new[] { "Admin", "Student" })
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        var r = await roleManager.CreateAsync(new IdentityRole(role));
                        logger.LogInformation("Role '{Role}' created: {Result}", role, r.Succeeded);
                    }
                }

                // ── Campuses ───────────────────────────────────────────────
                if (!await context.Campuses.AnyAsync())
                {
                    context.Campuses.AddRange(
                        new Campus { Name = "Howard College",      University = "UKZN",                   City = "Durban",             IsActive = true },
                        new Campus { Name = "Westville",           University = "UKZN",                   City = "Durban",             IsActive = true },
                        new Campus { Name = "Pietermaritzburg",    University = "UKZN",                   City = "Pietermaritzburg",   IsActive = true },
                        new Campus { Name = "Edgewood",            University = "UKZN",                   City = "Pinetown",           IsActive = true },
                        new Campus { Name = "Medical School",      University = "UKZN",                   City = "Durban",             IsActive = true },
                        new Campus { Name = "Durban Campus",       University = "DUT",                    City = "Durban",             IsActive = true },
                        new Campus { Name = "Pretoria Main",       University = "University of Pretoria", City = "Pretoria",           IsActive = true },
                        new Campus { Name = "Stellenbosch Main",   University = "Stellenbosch University",City = "Stellenbosch",       IsActive = true },
                        new Campus { Name = "Upper Campus",        University = "UCT",                    City = "Cape Town",          IsActive = true },
                        new Campus { Name = "East Campus",         University = "Wits",                   City = "Johannesburg",       IsActive = true }
                    );
                    await context.SaveChangesAsync();
                    logger.LogInformation("Campuses seeded.");
                }

                // ── Textbook categories ────────────────────────────────────
                if (!await context.TextbookCategories.AnyAsync())
                {
                    context.TextbookCategories.AddRange(
                        new TextbookCategory { Name = "Mathematics",           IsActive = true },
                        new TextbookCategory { Name = "Computer Science",      IsActive = true },
                        new TextbookCategory { Name = "Engineering",           IsActive = true },
                        new TextbookCategory { Name = "Natural Sciences",      IsActive = true },
                        new TextbookCategory { Name = "Economics & Finance",   IsActive = true },
                        new TextbookCategory { Name = "Law",                   IsActive = true },
                        new TextbookCategory { Name = "Medicine & Health",     IsActive = true },
                        new TextbookCategory { Name = "Humanities",            IsActive = true },
                        new TextbookCategory { Name = "Education",             IsActive = true },
                        new TextbookCategory { Name = "Business & Management", IsActive = true },
                        new TextbookCategory { Name = "Social Sciences",       IsActive = true },
                        new TextbookCategory { Name = "Other",                 IsActive = true }
                    );
                    await context.SaveChangesAsync();
                    logger.LogInformation("Categories seeded.");
                }

                // ── Course codes ───────────────────────────────────────────
                if (!await context.CourseCodes.AnyAsync())
                {
                    var campuses = await context.Campuses.ToListAsync();

                    foreach (var campus in campuses)
                    {
                        context.CourseCodes.AddRange(
                            new CourseCode { Code = "MATH101", CourseName = "Calculus I",                   CampusId = campus.Id, IsActive = true },
                            new CourseCode { Code = "MATH201", CourseName = "Calculus II",                  CampusId = campus.Id, IsActive = true },
                            new CourseCode { Code = "MATH301", CourseName = "Linear Algebra",               CampusId = campus.Id, IsActive = true },
                            new CourseCode { Code = "COMP101", CourseName = "Introduction to Programming",  CampusId = campus.Id, IsActive = true },
                            new CourseCode { Code = "COMP201", CourseName = "Data Structures & Algorithms", CampusId = campus.Id, IsActive = true },
                            new CourseCode { Code = "COMP301", CourseName = "Database Systems",             CampusId = campus.Id, IsActive = true },
                            new CourseCode { Code = "ECON101", CourseName = "Microeconomics",               CampusId = campus.Id, IsActive = true },
                            new CourseCode { Code = "PHYS101", CourseName = "Physics I",                    CampusId = campus.Id, IsActive = true },
                            new CourseCode { Code = "CHEM101", CourseName = "Chemistry I",                  CampusId = campus.Id, IsActive = true },
                            new CourseCode { Code = "STAT101", CourseName = "Statistics I",                 CampusId = campus.Id, IsActive = true },
                            new CourseCode { Code = "LAW101",  CourseName = "Introduction to Law",          CampusId = campus.Id, IsActive = true },
                            new CourseCode { Code = "ACCT101", CourseName = "Financial Accounting",         CampusId = campus.Id, IsActive = true }
                        );
                    }
                    await context.SaveChangesAsync();
                    logger.LogInformation("Course codes seeded.");
                }

                // ── Pickup points ──────────────────────────────────────────
                if (!await context.PickupPoints.AnyAsync())
                {
                    var campuses = await context.Campuses.ToListAsync();
                    foreach (var campus in campuses)
                    {
                        context.PickupPoints.AddRange(
                            new PickupPoint { CampusId = campus.Id, Name = "Main Library Entrance",   IsActive = true },
                            new PickupPoint { CampusId = campus.Id, Name = "Student Union Building",  IsActive = true }
                        );
                    }
                    await context.SaveChangesAsync();
                    logger.LogInformation("Pickup points seeded.");
                }

                // ── Admin account ──────────────────────────────────────────
                const string adminEmail = "admin@bookswap.co.za";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var firstCampus = await context.Campuses.FirstAsync();
                    var admin = new ApplicationUser
                    {
                        FirstName       = "BookSwap",
                        LastName        = "Admin",
                        DisplayUsername = "admin",
                        StudentNumber   = "ADMIN0001",
                        UserName        = adminEmail,
                        Email           = adminEmail,
                        EmailConfirmed  = true,
                        City            = "Durban",
                        CampusId        = firstCampus.Id,
                        IsActive        = true
                    };
                    var result = await userManager.CreateAsync(admin, "Admin@BookSwap1!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                        logger.LogInformation("Admin account created.");
                    }
                    else
                    {
                        logger.LogWarning("Admin creation failed: {Errors}",
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Seeding failed.");
                throw;
            }
        }
    }
}
