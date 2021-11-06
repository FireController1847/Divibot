using System;

namespace Divibot.Database.Entities {

    public class EntityYeetedUser {

        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong UserId { get; set; }

        public DateTime ExpirationDate { get; set; }

    }

}
