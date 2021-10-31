using System.ComponentModel.DataAnnotations;

namespace Divibot.Database.Entities {

    public class EntityAfkUser {

        [Key]
        public ulong UserId { get; set; }

        public string Message { get; set; }

    }

}
