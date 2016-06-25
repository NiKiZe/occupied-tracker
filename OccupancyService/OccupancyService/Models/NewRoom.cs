using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OccupancyService.Models
{
    /// <summary>
    /// Represents a single room
    /// </summary>
    public class NewRoom
    {
        /// <summary>
        /// Name of room
        /// </summary>
        public string Description { get; set; }

        public Room ToRoom()
        {
            return new Room
            {
                Description = Description
            };
        }
    }
}