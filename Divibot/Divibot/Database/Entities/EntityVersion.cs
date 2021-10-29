using System.ComponentModel.DataAnnotations;

namespace Divibot.Database.Entities {

    public class EntityVersion {

        [Key]
        public int Id { get; set; }

        public int MajorVersion { get; set; }

        public int MinorVersion { get; set; }

        public int Commands { get; set; }

        public int Launches { get; set; }

        public override string ToString() {
            return $"v{MajorVersion}.{MinorVersion}.{Commands}.{Launches}";
        }

    }

}
