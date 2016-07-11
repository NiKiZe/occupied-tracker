using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

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
        public DateTime StartTime { get; set; }
    }
}
