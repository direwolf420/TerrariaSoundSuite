using System;
using System.Collections.Generic;

namespace TerrariaSoundSuite
{
    internal class ValidStyles
    {
        internal int start;

        /// <summary>
        /// Exclusive index (start + length)
        /// </summary>
        // 0 + 5 means index 0 to 4 is valid
        internal int length;

        /// <summary>
        /// Sorted list
        /// </summary>
        internal List<int> others;

        internal bool Always => start == -1 && length == 0;

        internal int LastValidStyle => Math.Max(start + length - 1, (others != null && length > 0) ? others[others.Count - 1] : -1);

        internal int FirstValidStyle => Math.Min(start, others != null ? others[0] : start);

        internal ValidStyles(int start = 0, int length = 0, List<int> others = null)
        {
            this.start = start;
            this.length = length;
            this.others = others;
            if (this.others != null)
            {
                this.others.Sort();
            }
        }

        internal bool Contains(int style)
        {
            if ((style >= start && style < start + length) ||
                (others != null ? others.BinarySearch(style) > -1 : true)) return true;
            return false;
        }

        public override string ToString()
        {
            return $"{start} to {length - 1}";
        }
    }
}
