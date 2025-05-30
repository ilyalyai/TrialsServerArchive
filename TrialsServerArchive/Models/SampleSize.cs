namespace TrialsServerArchive.Models
{
    public abstract class SampleSize
    {
        public override abstract string ToString();

        public float Weight { get; set; }

        public float Density { get; private set; }
    }
}
