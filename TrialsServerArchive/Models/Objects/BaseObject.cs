namespace TrialsServerArchive.Models.Objects
{
    public abstract class BaseObject
    {
        /// <summary>
        /// id объекта в базе
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Название серии (вводится пользователем)
        /// </summary>
        public string SeriesName { get; set; }


        /// <summary>
        /// Имя образца
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Дата создания образца
        /// </summary>s
        public DateTime SampleCreationDate { get; set; }

        /// <summary>
        /// Создатель образца
        /// </summary>s
        public string CreatedBy { get; set; } = "System";

        /// <summary>
        /// Сколько времени прошло с даты создания 
        /// </summary>
        /// <param name="till"></param>
        /// <returns>Срок (в сутках)</returns>
        public int GetAge(DateTime till) =>
            till < SampleCreationDate ? 0 : (till - SampleCreationDate).Days;
    }
}
