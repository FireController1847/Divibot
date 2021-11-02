using Divibot.Attack;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Divibot.Database.Entities {

    public class EntityCustomAttackCategoryChance {

        // Dual-key generated in DbContext [UserId, AttackCategory]

        public ulong UserId { get; set; }

        [Column(TypeName = "VARCHAR(20)")]
        [MaxLength(20)]
        public AttackCategory Category { get; set; }

        [Column(TypeName = "TINYINT UNSIGNED")]
        public uint ChanceMin { get; set; }

        [Column(TypeName = "TINYINT UNSIGNED")]
        public uint ChanceMax { get; set; }

    }

}
