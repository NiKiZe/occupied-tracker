using System;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using OccupancyService.Models;

namespace OccupancyService.TableEntities
{
    /// <summary>
    /// Represents a single room
    /// </summary>
    public class RoomEntity : TableEntity
    {
        static readonly TimeZoneInfo LocalTimeZone = TimeZoneInfo.FindSystemTimeZoneById(CloudConfigurationManager.GetSetting("TimeZone"));

        /// <summary>
        /// Creates a new room entity
        /// </summary>
        /// <param name="id"></param>
        public RoomEntity(long id)
        {
            PartitionKey = "Rooms";
            RowKey = id.ToString("d19");
        }

        /// <summary>
        /// Creates a new room entity
        /// </summary>
        public RoomEntity() { }

        /// <summary>
        /// Room id
        /// </summary>
        public long Id => long.Parse(RowKey);

        /// <summary>
        /// Name of room
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Indicates if the room is currently occupied
        /// </summary>
        public bool IsOccupied { get; set; }

        /// <summary>
        /// Indicates when this room was last updated, in UTC
        /// </summary>
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// Updates this room with the information contained in the room update
        /// </summary>
        /// <param name="roomUpdate"></param>
        public void Update(RoomUpdate roomUpdate)
        {
            if (roomUpdate.Description != null)
            {
                Description = roomUpdate.Description;
            }

            if (roomUpdate.IsOccupied.HasValue)
            {
                IsOccupied = roomUpdate.IsOccupied.Value;
            }
        }

        /// <summary>
        /// Converts this room entity to a room
        /// </summary>
        /// <returns></returns>
        public Room ToRoom()
        {
            var lastUpdateLocalTime = TimeZoneInfo.ConvertTimeFromUtc(LastUpdate, LocalTimeZone);
            var lastUpdateLocalTimeOffset = new DateTimeOffset(lastUpdateLocalTime, LocalTimeZone.GetUtcOffset(LastUpdate));
            return new Room
            {
                Id = Id,
                Description = Description,
                IsOccupied = IsOccupied,
                LastUpdate = lastUpdateLocalTimeOffset
            };
        }
    }
}