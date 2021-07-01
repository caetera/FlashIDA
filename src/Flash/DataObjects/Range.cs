namespace Flash.DataObjects
{
    /// <summary>
    /// Generic range object, i.e. all values from a start value to an end value
    /// </summary>
    public struct Range
    {
        public double Start;
        public double End;
        public double Width { get => End - Start; }
        public double Center { get => (End + Start) / 2; }

        public Range(double start, double end)
        {
            Start = start;
            End = end;
        }
    }
}
