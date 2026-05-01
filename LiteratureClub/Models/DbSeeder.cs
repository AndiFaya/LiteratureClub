using LiteratureClub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LiteratureClub.Data
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

                        // ── Tshwane University of Technology (TUT) ────────────────────
                        new Campus { Name = "Pretoria Campus", University = "Tshwane University of Technology", City = "Pretoria", IsActive = true },
                        new Campus { Name = "Arcadia Campus", University = "Tshwane University of Technology", City = "Pretoria", IsActive = true },
                        new Campus { Name = "Arts Campus", University = "Tshwane University of Technology", City = "Pretoria", IsActive = true },
                        new Campus { Name = "GaRankuwa Campus", University = "Tshwane University of Technology", City = "GaRankuwa", IsActive = true },
                        new Campus { Name = "Polokwane Campus", University = "Tshwane University of Technology", City = "Polokwane", IsActive = true },
                        new Campus { Name = "Soshanguve North Campus", University = "Tshwane University of Technology", City = "Soshanguve", IsActive = true },
                        new Campus { Name = "Soshanguve South Campus", University = "Tshwane University of Technology", City = "Soshanguve", IsActive = true },
                        new Campus { Name = "eMalahleni Campus", University = "Tshwane University of Technology", City = "eMalahleni", IsActive = true },
                        new Campus { Name = "Mbombela Campus", University = "Tshwane University of Technology", City = "Mbombela", IsActive = true },

                        // ── University of the Witwatersrand (Wits) ────────────────────
                        new Campus { Name = "East Campus Braamfontein", University = "University of the Witwatersrand", City = "Johannesburg", IsActive = true },
                        new Campus { Name = "West Campus Braamfontein", University = "University of the Witwatersrand", City = "Johannesburg", IsActive = true },
                        new Campus { Name = "Education Campus Parktown", University = "University of the Witwatersrand", City = "Johannesburg", IsActive = true },
                        new Campus { Name = "Medical School Campus Parktown", University = "University of the Witwatersrand", City = "Johannesburg", IsActive = true },
                        new Campus { Name = "Wits Business School Parktown", University = "University of the Witwatersrand", City = "Johannesburg", IsActive = true },

                        // ── University of Johannesburg (UJ) ───────────────────────────
                        new Campus { Name = "Auckland Park Kingsway (APK)", University = "University of Johannesburg", City = "Johannesburg", IsActive = true },
                        new Campus { Name = "Auckland Park Bunting Road (APB)", University = "University of Johannesburg", City = "Johannesburg", IsActive = true },
                        new Campus { Name = "Doornfontein (DFC)", University = "University of Johannesburg", City = "Johannesburg", IsActive = true },
                        new Campus { Name = "Soweto Campus", University = "University of Johannesburg", City = "Soweto", IsActive = true },

                        // ── University of Pretoria (UP) ───────────────────────────────
                        new Campus { Name = "Hatfield Campus", University = "University of Pretoria", City = "Pretoria", IsActive = true },
                        new Campus { Name = "Groenkloof Campus", University = "University of Pretoria", City = "Pretoria", IsActive = true },
                        new Campus { Name = "Prinshof Campus", University = "University of Pretoria", City = "Pretoria", IsActive = true },
                        new Campus { Name = "Onderstepoort Campus", University = "University of Pretoria", City = "Pretoria", IsActive = true },
                        new Campus { Name = "Hillcrest Campus", University = "University of Pretoria", City = "Pretoria", IsActive = true },
                        new Campus { Name = "Mamelodi Campus", University = "University of Pretoria", City = "Mamelodi", IsActive = true },
                        new Campus { Name = "Gordon Institute of Business Science", University = "University of Pretoria", City = "Illovo", IsActive = true },

                        // ── STADIO Higher Education ───────────────────────────────────
                        new Campus { Name = "Bellville", University = "STADIO Higher Education", City = "Bellville", IsActive = true },
                        new Campus { Name = "Centurion", University = "STADIO Higher Education", City = "Centurion", IsActive = true },
                        new Campus { Name = "Durbanville", University = "STADIO Higher Education", City = "Durbanville", IsActive = true },
                        new Campus { Name = "Musgrave", University = "STADIO Higher Education", City = "Durban", IsActive = true },
                        new Campus { Name = "Hatfield", University = "STADIO Higher Education", City = "Pretoria", IsActive = true },
                        new Campus { Name = "Randburg", University = "STADIO Higher Education", City = "Randburg", IsActive = true },
                        new Campus { Name = "Waterfall", University = "STADIO Higher Education", City = "Midrand", IsActive = true },

                        // ── Emeris ────────────────────────────────────────────────────
                        new Campus { Name = "Cape Town", University = "Emeris", City = "Cape Town", IsActive = true },
                        new Campus { Name = "Newlands", University = "Emeris", City = "Cape Town", IsActive = true },
                        new Campus { Name = "Durban North", University = "Emeris", City = "Durban", IsActive = true },
                        new Campus { Name = "Westville", University = "Emeris", City = "Durban", IsActive = true },
                        new Campus { Name = "Umhlanga", University = "Emeris", City = "Umhlanga", IsActive = true },
                        new Campus { Name = "Pietermaritzburg", University = "Emeris", City = "Pietermaritzburg", IsActive = true },
                        new Campus { Name = "Nelson Mandela Bay", University = "Emeris", City = "Gqeberha", IsActive = true },
                        new Campus { Name = "Pretoria", University = "Emeris", City = "Pretoria", IsActive = true },
                        new Campus { Name = "Sandton", University = "Emeris", City = "Sandton", IsActive = true },
                        new Campus { Name = "Rosebank", University = "Emeris", City = "Johannesburg", IsActive = true },
                        new Campus { Name = "Ruimsig", University = "Emeris", City = "Roodepoort", IsActive = true }

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
                const string adminEmail = "admin@LiteratureClub.co.za";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var firstCampus = await context.Campuses.FirstAsync();
                    var admin = new ApplicationUser
                    {
                        FirstName       = "LiteratureClub",
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
