using System.ComponentModel.DataAnnotations;

namespace Divibot.Database.Entities {

    public class EntityAttackUser {

        [Key]
        public ulong UserId { get; set; }

        public string Class { get; set; }

        public int Score { get; set; }

    }

}
