namespace TrialsServerArchive.Models.Objects
{
    public enum JournalType
    {
        RES,
        OB
    }

    public class SampleType
    {
        public int Id { get; set; }
        public required string Name { get; set; } // "К200", "К150" и т.д.

        public string? Description { get; set; } 
    }

    public class SamplePhoto
    {
        public int Id { get; set; }
        public int SampleId { get; set; }
        public virtual BaseObject Sample { get; set; }

        public string FileName { get; set; }  // Оригинальное имя файла
        public string ContentType { get; set; } // MIME-тип (image/jpeg, image/png и т.д.)
        public byte[] Content { get; set; }    // Бинарные данные изображения
        public DateTime UploadDate { get; set; } = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
    }

    public class BaseObject
    {
        public int Id { get; set; }

        /// <summary>
        /// Название серии (вводится пользователем)
        /// </summary>
        public string? SeriesName { get; set; }

        /// <summary>
        /// Наименование образца
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Дата изготовления образца
        /// </summary>
        public DateTime SampleCreationDate { get; set; }

        /// <summary>
        /// Дата измерения (записи)
        /// </summary>
        public DateTime RecordDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Создатель записи
        /// </summary>
        public string CreatedBy { get; set; } = "System";

        /// <summary>
        /// Вес образца
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Размер A (для куба/призмы) или Высота (для цилиндра)
        /// </summary>
        public double DimensionA { get; set; }

        /// <summary>
        /// Размер B (для куба/призмы) или Диаметр (для цилиндра)
        /// </summary>
        public double DimensionB { get; set; }

        /// <summary>
        /// Размер C (только для куба/призмы)
        /// </summary>
        public double? DimensionC { get; set; }

        /// <summary>
        /// Примечания
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Тип журнала (РЕС/ОБ)
        /// </summary>
        public JournalType JournalType { get; set; }

        /// <summary>
        /// Тип образца
        /// </summary>
        public int SampleTypeId { get; set; }
        public virtual SampleType? SampleType { get; set; }

        /// <summary>
        /// Фотографии образца
        /// </summary>
        public virtual ICollection<SamplePhoto> Photos { get; set; } = new List<SamplePhoto>();

        /// <summary>
        /// Возраст образца в сутках
        /// </summary>
        public int Age => GetAge(RecordDate);

        /// <summary>
        /// Сколько времени прошло с даты создания 
        /// </summary>
        /// <param name="till">Дата измерения</param>
        /// <returns>Срок (в сутках)</returns>
        public int GetAge(DateTime till) =>
            till < SampleCreationDate ? 0 : (till - SampleCreationDate).Days;
    }
}