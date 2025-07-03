using System.ComponentModel.DataAnnotations.Schema;

namespace TrialsServerArchive.Models.Objects
{
    public class TrialObject : BaseObject
    {
        public int SampleId { get; set; }

        public DateTime TestingDate { get; set; }

        // Режим испытаний
        public string TestMode { get; set; } = "Стандарт";

        // Результаты испытаний
        public double? WeightAfterTest { get; set; } // Вес после испытаний, г
        public double? DimensionAAfterTest { get; set; } // a, мм
        public double? DimensionBAfterTest { get; set; } // b, мм
        public double? DimensionCAfterTest { get; set; } // h, мм
        public double? DensityAfterTest { get; set; } // Плотность, кг/м³

        [NotMapped]
        public double? DensityLoss =>
            Density - DensityAfterTest;

        public double? BreakingLoad { get; set; } // Разрушающая нагрузка, кН
        public double? WetCoefficient { get; set; } // МК (влажностный коэффициент)

        [NotMapped]
        public double? StrengthWithCoefficient =>
            BreakingLoad.HasValue && WetCoefficient.HasValue
                ? BreakingLoad.Value * WetCoefficient.Value
                : null; // Прочность с учетом МК, МПа

        public double? TestingTemperature { get; set; } // Температура воздуха, °C
        public double? TestingHumidity { get; set; } // Влажность воздуха, %

        // Документация
        public string MU { get; set; } // МУ
        public string MUStar { get; set; } // МУ*
        public string PP { get; set; } // ПП

        // Внешние ключи для вариантов выбора
        public int? FurnaceProgramId { get; set; }
        public int? StoragePlaceId { get; set; }

        [ForeignKey("FurnaceProgramId")]
        public virtual FurnaceProgram? FurnaceProgram { get; set; }

        [ForeignKey("StoragePlaceId")]
        public virtual StoragePlace? StoragePlace { get; set; }

        // Исполнители
        public string TestedBy { get; set; } // Испытания провел

        // Примечания
        public string Comment { get; set; }

        // Связь с оснасткой
        public virtual ICollection<TrialTooling> ToolingLinks { get; set; } = [];

        [NotMapped]
        public IEnumerable<Tooling> Toolings => ToolingLinks.Select(tt => tt.Tooling);

        // Возраст образца на момент испытания
        [NotMapped]
        public int? TestingAge =>
            SampleCreationDate != null
                ? (int)(TestingDate - SampleCreationDate).TotalDays
                : null;

        [NotMapped]
        public int? DaysToNextAge { get; set; }

    }

    // Модель для печей/программ
    public class FurnaceProgram
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    // Модель для мест хранения
    public class StoragePlace
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }
}