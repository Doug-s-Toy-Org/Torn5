using System;

namespace Torn5
{
    internal class PackHits: IComparable
    {
        public string Name {  get; set; }
		public int Games { get; set; }
        public int Phasor { get; set; }
        public int Chest { get; set; }
        public int FrontLeftShoulder { get; set; }
        public int FrontRightShoulder { get; set; }
        public int Back { get; set; }
        public PackHits(string name)
        {
            Name = name;
            Back = 0;
            Phasor = 0;
            Chest = 0;
            FrontLeftShoulder = 0;
            FrontRightShoulder = 0;
            Back = 0;
        }

        public int TotalHits()
        {
            return Phasor + Chest + Back + FrontLeftShoulder + FrontRightShoulder;
        }

		int IComparable.CompareTo(object obj)
		{
			return string.Compare(Name, ((PackHits)obj).Name);
		}
    }
}
