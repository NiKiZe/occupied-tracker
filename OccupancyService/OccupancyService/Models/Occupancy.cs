using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OccupancyService.Models
{
    /// <summary>
    /// Represents a single occupancy
    /// </summary>
    public class Occupancy
    {
        /// <summary>
        /// The occupancy id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The room id
        /// </summary>
        public long RoomId { get; set; }

        /// <summary>
        /// The time the room started to become occupied
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// The time when the room became unoccupied again
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }
    }
}
