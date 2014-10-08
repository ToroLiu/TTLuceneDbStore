using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTLuceneDbStore.Helper
{
    internal static class DateTimeHelper
    {
        private static readonly DateTime _dtBase = new DateTime(1970, 1, 1, 0, 0, 0);

        /// <summary>
        /// Turn DateTime into Long value.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static long ToLongInt(this DateTime dt)
        {
            return (long)dt.Subtract(_dtBase).TotalMilliseconds;
        }
        
    }
}
