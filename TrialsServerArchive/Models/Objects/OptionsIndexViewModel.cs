namespace TrialsServerArchive.Models.Objects
{
    public class OptionsIndexViewModel
    {
        public List<SampleType> SampleTypes { get; set; } = new();
        public List<FurnaceProgram> FurnacePrograms { get; set; } = new();
        public List<StoragePlace> StoragePlaces { get; set; } = new();
    }
}