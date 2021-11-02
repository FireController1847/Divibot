using Divibot.Attack;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Divibot.Database.Entities {

    public class EntityAttackTypeChance {

        // Triple-key generated in DbContext [UserId, AttackCategory, AttackType]

        public ulong UserId { get; set; }

        [Column(TypeName = "VARCHAR(20)")]
        [MaxLength(20)]
        public AttackCategory AttackCategory { get; set; }

        [Column(TypeName = "VARCHAR(20)")]
        [MaxLength(20)]
        public string AttackTypeId { get; set; }

        [Column(TypeName = "TINYINT UNSIGNED")]
        public uint CritChance { get; set; }

        [Column(TypeName = "TINYINT UNSIGNED")]
        public uint Chance { get; set; }

        [Column(TypeName = "TINYINT UNSIGNED")]
        public uint IneffChance { get; set; }

        public override string ToString() {
            return $"({UserId}, {AttackCategory}, {AttackTypeId}) = {CritChance}, {Chance}, {IneffChance}";
        }

    }

}
