using System;

namespace OccupancyService.Models
{
    /// <summary>
    /// Represents a single room
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Id of room
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Name of room
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Indicates if the room is currently occupied
        /// </summary>
        public bool IsOccupied { get; set; }

        /// <summary>
        /// Indicates when this room was last updated
        /// </summary>
        public DateTime LastUpdate { get; set; }
    }
}