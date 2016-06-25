using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OccupancyService.Models
{
    /// <summary>
    /// Represents a single room
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Room id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Name of room
        /// </summary>
        public string Description { get; set; }
    }
}