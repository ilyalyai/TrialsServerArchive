using System.ComponentModel.DataAnnotations.Schema;

namespace TrialsServerArchive.Models.Objects
{
    public class TrialObject : BaseObject
    {
        public int SampleId { get; set; }

        public DateTime TestingDate { get; set; }

        // Изменяем тип навигационного свойства
        public ICollection<TrialTooling> ToolingLinks { get; set; } = [];

        [NotMapped] // Не сохраняется в БД, только для удобства доступа
        public IEnumerable<Tooling> Toolings => ToolingLinks.Select(tt => tt.Tooling);
    }
}
