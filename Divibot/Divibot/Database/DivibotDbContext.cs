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

        // Dependency Injection
        private DiscordClient _client;

        // Constructor
        public DivibotDbContext(DbContextOptions options, DiscordClient client) : base(options) {
            _client = client;
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
            _client.Logger.LogInformation(LOG_EVENT_ID, $"Running Divibot {version}");
            await SaveChangesAsync();
        }

    }

}
