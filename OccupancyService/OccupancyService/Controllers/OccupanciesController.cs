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
    /// All occupancies of all rooms
    /// </summary>
    public class OccupanciesController : ApiController
    {
        /// <summary>
        /// Gets occupancy history of all rooms
        /// </summary>
        /// <remarks>
        /// Gets occupancy history of all rooms
        /// </remarks>
        /// <returns></returns>
        public IEnumerable<Occupancy> Get()
        {
            var repository = new OccupancyRepository();
            return repository.GetAll();
        }

        /// <summary>
        /// Gets occupancy history of a given room
        /// </summary>
        /// <remarks>
        /// Gets occupancy history of a given room
        /// </remarks>
        /// <returns></returns>
        public IEnumerable<Occupancy> Get(int id)
        {
            var repository = new OccupancyRepository();
            return repository.GetAll(new[] {id});
        }

        /// <summary>
        /// Updates the status of a given room
        /// </summary>
        /// <remarks>
        /// Updates the status of a given room
        /// </remarks>
        /// <returns></returns>
        /// <param name="id">The room id</param>
        /// <param name="isOccupied">Indicates if the room is currently occupied or not</param>
        /// <returns></returns>
        public Occupancy Post(int id, bool isOccupied)
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