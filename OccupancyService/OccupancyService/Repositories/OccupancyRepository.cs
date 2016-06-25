using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OccupancyService.Models;

namespace OccupancyService.Repositories
{
    /// <summary>
    /// Mockup repository
    /// </summary>
    public class OccupancyRepository
    {
        private static List<Occupancy> _occupancies = new List<Occupancy>();

        public IEnumerable<Occupancy> GetAll(List<long> roomIds, DateTime? fromTime = null, DateTime? toTime = null)
        {
            IEnumerable<Occupancy> occupancies = _occupancies;

            if (roomIds != null && roomIds.Count > 0)
            {
                // Filter on given rooms
                occupancies = occupancies.Where(x => roomIds.Contains(x.RoomId));
            }

            if (fromTime.HasValue)
            {
                // Filter on fromTime
                occupancies = occupancies.Where(x => x.StartTime >= fromTime.Value);
            }

            if (toTime.HasValue)
            {
                // Filter on toTime
                occupancies = occupancies.Where(x => x.StartTime <= toTime.Value);
            }

            // Sort on id, because why not
            return occupancies.OrderBy(x => x.Id);
        }

        public Occupancy Get(long id)
        {
            return _occupancies.FirstOrDefault(x => x.Id == id);
        }

        public Occupancy GetLastOccupancy(long roomId)
        {
            return _occupancies.Last(x => x.RoomId == roomId);
        }

        public Occupancy Insert(Occupancy occupancy)
        {
            occupancy.Id = _occupancies.Count > 0 ? _occupancies.Max(x => x.Id) + 1 : 1;
            _occupancies.Add(occupancy);
            return occupancy;
        }
        
        public Occupancy Update(Occupancy occupancy)
        {
            if (_occupancies.All(x => x.Id != occupancy.Id))
            {
                return null;
            }
            _occupancies.RemoveAll(x => x.Id == occupancy.Id);
            _occupancies.Add(occupancy);
            return occupancy;
        }
    }
}