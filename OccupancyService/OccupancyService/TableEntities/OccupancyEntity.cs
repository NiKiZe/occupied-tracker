using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using OccupancyService.Models;

namespace OccupancyService.TableEntities
{
    public class OccupancyEntity : TableEntity
    {
        static readonly TimeZoneInfo LocalTimeZone = TimeZoneInfo.FindSystemTimeZoneById(CloudConfigurationManager.GetSetting("TimeZone"));

        public OccupancyEntity(long roomId, DateTime startTime)
        {
            PartitionKey = roomId.ToString("d19");
            var rowKey = DateTime.MaxValue.Ticks - startTime.Ticks;
            RowKey = rowKey.ToString("d19");
        }

        public OccupancyEntity() { }

        /// <summary>
        /// The id of this occupancy
        /// </summary>
        public long Id => long.Parse(RowKey);

        /// <summary>
        /// Room id
        /// </summary>
        public long RoomId => long.Parse(PartitionKey);

        /// <summary>
        /// The time the room started to become occupied, in UTC
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                var ticksDiff = long.Parse(RowKey);
                var startTimeTicks = DateTime.MaxValue.Ticks - ticksDiff;
                return new DateTime(startTimeTicks);
            }
        }

        /// <summary>
        /// Converts this occupancy entity to a normal occupancy
        /// </summary>
        /// <returns></returns>
        public Occupancy ToOccupancy()
        {
            var startTimeLocalTime = TimeZoneInfo.ConvertTimeFromUtc(StartTime, LocalTimeZone);
            var startTimeLocalTimeOffset = new DateTimeOffset(startTimeLocalTime, LocalTimeZone.GetUtcOffset(StartTime));
            return new Occupancy
            {
                Id = Id,
                RoomId = RoomId,
                StartTime = startTimeLocalTimeOffset
            };
        }
    }
}
