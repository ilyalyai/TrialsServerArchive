using TrialsServerArchive.Models.Objects;

namespace TrialsServerArchive.Models.Objects
{
    public class Sample : BaseObject
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ManufactureDate { get; set; }
    }
}
