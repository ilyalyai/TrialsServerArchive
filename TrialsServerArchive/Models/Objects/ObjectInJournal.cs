namespace TrialsServerArchive.Models.Objects
{
    public class ObjectInJournal : TrialObject
    {
        public DateTime ArchiveDate { get; set; }

        public string ArchivedBy { get; set; }
    }
}
