using System.ComponentModel.DataAnnotations.Schema;
using TrialsServerArchive.Models.Objects;

namespace TrialsServerArchive.Models
{
    /// <summary>
    /// Оснастка - используется при испытаниях образцов
    /// </summary>
    public class Tooling
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string CreatedBy { get; set; }

        public DateTime ReconciliationDate { get; set; }
        // Изменяем тип навигационного свойства
        public ICollection<TrialTooling> TrialLinks { get; set; } = [];

        [NotMapped] // Не сохраняется в БД, только для удобства доступа
        public IEnumerable<TrialObject> TrialObjects => TrialLinks.Select(tt => tt.TrialObject);
    }
}
