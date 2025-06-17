using TrialsServerArchive.Models.Objects;

namespace TrialsServerArchive.Models
{
    public class TrialTooling
    {
        public int TrialObjectId { get; set; }
        public TrialObject TrialObject { get; set; }

        public int ToolingId { get; set; }
        public Tooling Tooling { get; set; }
    }
}