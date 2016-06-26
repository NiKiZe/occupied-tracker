using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OccupancyService.Models
{
    /// <summary>
    /// Represents a single room
    /// </summary>
    public class RoomInsert
    {
        /// <summary>
        /// Id of room
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Name of room
        /// </summary>
        public string Description { get; set; }

        public Room ToRoom()
        {
            return new Room
            {
                Id = Id,
                Description = Description
            };
        }
    }
}