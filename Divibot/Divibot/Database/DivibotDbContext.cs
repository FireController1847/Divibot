using Divibot.Database.Entities;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Divibot.Database {

    public class DivibotDbContext : DbContext {

        // Variables
        private readonly EventId LOG_EVENT_ID = new EventId(1750, "Database");

        // Entities
        public DbSet<EntityVersion> Versions { get; set; }
        public DbSet<EntityAfkUser> AfkUsers { get; set; }
        public DbSet<EntityAttackUser> AttackUsers { get; set; }
        public DbSet<EntityAttackTypeChance> AttackTypeChances { get; set; }
        public DbSet<EntityCustomAttackCategoryChance> CustomAttackCategoryChances { get; set; }
        public DbSet<EntityCustomAttackModifierChance> CustomAttackModifierChances { get; set; }

        // Dependency Injection
        private DiscordClient _client;

        // Constructor
        public DivibotDbContext() {
            // This is for migrations only.
            // To run "Add-Migration", enusre to set the environment variables below via $env:MYVAR="Value"
        }
        public DivibotDbContext(DbContextOptions options, DiscordClient client) : base(options) {
            _client = client;
        }

        // Handle configuration
        protected override void OnConfiguring(DbContextOptionsBuilder builder) {
            string connectionString =
                        $"server=localhost;" +
                        $"database={Environment.GetEnvironmentVariable("DbDatabase")};" +
                        $"user={Environment.GetEnvironmentVariable("DbUser")};" +
                        $"password={Environment.GetEnvironmentVariable("DbPassword")};";

            builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }

        // Handle specialities for model creation
        protected override void OnModelCreating(ModelBuilder builder) {
            // Create multipart keys
            builder.Entity<EntityAttackTypeChance>()
                .HasKey(c => new { c.UserId, c.AttackCategory, c.AttackTypeId });
            builder.Entity<EntityCustomAttackCategoryChance>()
                .HasKey(c => new { c.UserId, c.Category });
            builder.Entity<EntityCustomAttackModifierChance>()
                .HasKey(m => new { m.UserId, m.Modifier });
        }

        // Updates the bot's version number
        public async Task UpdateBotVersionAsync() {
            // Prepare version
            EntityVersion version = await Versions.FirstOrDefaultAsync();
            _client.Logger.LogInformation(LOG_EVENT_ID, "Checking version...");
            if (version == null) {
                version = new EntityVersion() {
                    Id = 0,
                    MajorVersion = 16,
                    MinorVersion = 0,
                    Commands = _client.GetSlashCommands().RegisteredCommands.Sum((pair) => { return pair.Value.Count; }),
                    Launches = 0
                };
                Versions.Add(version);
            } else {
                version.Commands = _client.GetSlashCommands().RegisteredCommands.Sum((pair) => { return pair.Value.Count; });
                version.Launches++;
            }
            _client.Logger.LogInformation(LOG_EVENT_ID, $"Running Divibot Alpha {version}");
            await SaveChangesAsync();
        }

    }

}
