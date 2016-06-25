using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.AspNet.SignalR;
using OccupancyService.Models;
using OccupancyService.Repositories;

namespace OccupancyService.Controllers
{
    /// <summary>
    /// All rooms
    /// </summary>
    [RoutePrefix("Rooms")]
    public class RoomsController : ApiController
    {
        /// <summary>
        /// Gets list of all rooms
        /// </summary>
        /// <remarks>
        /// Gets list of all rooms
        /// </remarks>
        /// <returns></returns>
        [Route("")]
        public IEnumerable<Room> Get()
        {
            var repository = new RoomRepository();
            return repository.GetAll();
        }

        /// <summary>
        /// Gets a single room
        /// </summary>
        /// <remarks>
        /// Gets a single room
        /// </remarks>
        /// <returns></returns>
        [Route("{id}")]
        public Room Get(long id)
        {
            var repository = new RoomRepository();
            return repository.Get(id);
        }

        /// <summary>
        /// Creates a new room
        /// </summary>
        /// <remarks>
        /// Creates a new room
        /// </remarks>
        /// <returns></returns>
        [Route("")]
        public Room Post(NewRoom room)
        {
            var repository = new RoomRepository();
            return repository.Insert(room);
        }

        /// <summary>
        /// Gets occupancy history of all rooms
        /// </summary>
        /// <remarks>
        /// Gets occupancy history of all rooms
        /// </remarks>
        /// <param name="roomIds">Limit occupancies to these rooms</param>
        /// <param name="fromTime">Limit occupancies to starttime after this time</param>
        /// <param name="toTime">Limit occupancies to starttime before this time</param>
        /// <returns></returns>
        [Route("Occupancies")]
        public IEnumerable<Occupancy> GetOccupancies(
            [FromUri]List<long> roomIds = null,
            DateTime? fromTime = null,
            DateTime? toTime = null)
        {
            var repository = new OccupancyRepository();
            return repository.GetAll(roomIds: roomIds, fromTime: fromTime, toTime: toTime);
        }

        /// <summary>
        /// Updates the given room
        /// </summary>
        /// <remarks>
        /// Updates the given room
        /// </remarks>
        /// <returns></returns>
        [Route("{id}")]
        public Room Put(long id, Room room)
        {
            var repository = new RoomRepository();
            room.Id = id;
            return repository.Update(room);
        }

        /// <summary>
        /// Gets occupancy history of a single room
        /// </summary>
        /// <remarks>
        /// Gets occupancy history of a single room
        /// </remarks>
        /// <param name="id">The room id</param>
        /// <param name="fromTime">Limit occupancies to starttime after this time</param>
        /// <param name="toTime">Limit occupancies to starttime before this time</param>
        /// <returns></returns>
        [Route("{id}/Occupancies")]
        public IEnumerable<Occupancy> GetOccupancy(
            long id,
            DateTime? fromTime = null,
            DateTime? toTime = null)
        {
            var repository = new OccupancyRepository();
            return repository.GetAll(roomIds: new List<long> {id}, fromTime: fromTime, toTime: toTime);
        }

        /// <summary>
        /// Updates the status of a given room
        /// </summary>
        /// <remarks>
        /// Updates the status of a given room
        /// </remarks>
        /// <param name="id">The room id</param>
        /// <param name="isOccupied">Indicates if the room is currently occupied or not</param>
        /// <returns></returns>
        [Route("{id}/Occupancies")]
        public Occupancy PostOccupancy(long id, bool isOccupied)
        {
            // Send event on SignalR
            var context = GlobalHost.ConnectionManager.GetHubContext<OccupancyHub>();
            context.Clients.All.occupancyChanged(id, isOccupied);

            // Update DB
            var repository = new OccupancyRepository();
            if (isOccupied)
            {
                // Create a new occupancy
                var occupancy = new Occupancy
                {
                    RoomId = id,
                    StartTime = DateTimeOffset.Now
                };
                return repository.Insert(occupancy);
            }
            else
            {
                // End the last occupancy
                var occupancy = repository.GetLastOccupancy(id);
                occupancy.EndTime = DateTimeOffset.Now;
                return repository.Update(occupancy);
            }
        }
    }
}