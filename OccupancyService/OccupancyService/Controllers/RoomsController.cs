using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Web;
using System.Web.Http;
using Microsoft.AspNet.SignalR;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
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
        [HttpGet]
        public IEnumerable<Room> Get()
        {
            var repository = new RoomRepository();
            return repository.GetAll().Select(x => x.ToRoom());
        }

        /// <summary>
        /// Creates a new room
        /// </summary>
        /// <remarks>
        /// Creates a new room
        /// </remarks>
        /// <param name="room">The room to create</param>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("")]
        [HttpPost]
        public Room Post(Room room, string passcode = null)
        {
            CheckPasscode(passcode);

            var repository = new RoomRepository();
            return repository.Insert(room)?.ToRoom();
        }

        /// <summary>
        /// Delete all rooms
        /// </summary>
        /// <remarks>
        /// Delete all rooms
        /// </remarks>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("")]
        [HttpDelete]
        public void Delete(string passcode = null)
        {
            CheckPasscode(passcode);
            
            var repository = new RoomRepository();
            repository.DeleteTable();
        }

        /// <summary>
        /// Gets a single room
        /// </summary>
        /// <remarks>
        /// Gets a single room
        /// </remarks>
        /// <param name="id">Room id</param>
        /// <returns></returns>
        [Route("{id}")]
        [HttpGet]
        public Room Get(long id)
        {
            var repository = new RoomRepository();
            return repository.Get(id)?.ToRoom();
        }

        /// <summary>
        /// Deletes a single room
        /// </summary>
        /// <remarks>
        /// Deletes a single room
        /// </remarks>
        /// <param name="id">Room id</param>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("{id}")]
        [HttpDelete]
        public void Delete(long id, string passcode = null)
        {
            CheckPasscode(passcode);

            var repository = new RoomRepository();
            repository.Delete(id);
        }

        /// <summary>
        /// Updates the given room
        /// </summary>
        /// <remarks>
        /// Updates the given room
        /// </remarks>
        /// <param name="id">Room id</param>
        /// <param name="room">The new properties for the room</param>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("{id}")]
        [HttpPut]
        public Room Put(long id, Room room, string passcode = null)
        {
            CheckPasscode(passcode);

            var repository = new RoomRepository();
            var roomEntity = repository.Get(id);
            roomEntity.Update(room);
            return repository.Update(roomEntity)?.ToRoom();
        }

        /// <summary>
        /// Gets occupancy history of all rooms
        /// </summary>
        /// <remarks>
        /// Gets occupancy history of all rooms
        /// </remarks>
        /// <returns></returns>
        [Route("Occupancies")]
        [HttpGet]
        public IEnumerable<Occupancy> GetOccupancies()
        {
            var repository = new OccupancyRepository();
            return repository.GetAll().Select(x => x.ToOccupancy());
        }

        /// <summary>
        /// Delete all occupancies
        /// </summary>
        /// <remarks>
        /// Delete all occupancies
        /// </remarks>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("Occupancies")]
        [HttpDelete]
        public void DeleteOccupancies(string passcode = null)
        {
            CheckPasscode(passcode);

            var repository = new OccupancyRepository();
            repository.DeleteTable();
        }

        /// <summary>
        /// Gets occupancy history of a single room
        /// </summary>
        /// <remarks>
        /// Gets occupancy history of a single room
        /// </remarks>
        /// <param name="id">The room id</param>
        /// <returns></returns>
        [Route("{id}/Occupancies")]
        [HttpGet]
        public IEnumerable<Occupancy> GetOccupanciesForRoom(long id)
        {
            var repository = new OccupancyRepository();
            return repository.GetAll(id).Select(x => x.ToOccupancy());
        }

        /// <summary>
        /// Updates the status of a given room
        /// </summary>
        /// <remarks>
        /// Updates the status of a given room
        /// </remarks>
        /// <param name="id">The room id</param>
        /// <param name="isOccupied">Indicates if the room is currently occupied or not</param>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("{id}/Occupancies")]
        [HttpPost]
        public Occupancy PostOccupancy(long id, bool isOccupied, string passcode = null)
        {
            CheckPasscode(passcode);

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
                    StartTime = DateTime.UtcNow
                };
                return repository.Insert(occupancy)?.ToOccupancy();
            }
            else
            {
                // End the last occupancy
                var occupancyEntity = repository.GetLatestOccupancy(id);
                occupancyEntity.EndTime = DateTime.UtcNow;
                return repository.Update(occupancyEntity)?.ToOccupancy();
            }
        }

        /// <summary>
        /// Delete all occupancies in a single room
        /// </summary>
        /// <remarks>
        /// Delete all occupancies in a single room
        /// </remarks>
        /// <param name="id">The room id</param>
        /// <param name="passcode">Passcode for this method</param>
        /// <returns></returns>
        [Route("{id}/Occupancies")]
        [HttpDelete]
        public void DeleteOccupanciesInRoom(long id, string passcode = null)
        {
            CheckPasscode(passcode);

            var repository = new OccupancyRepository();
            repository.DeleteAllInRoom(id);
        }

        private void CheckPasscode(string passcode)
        {
            // Check passcode if there is one
            var correctPasscode = CloudConfigurationManager.GetSetting("ApiPasscode");
            if (string.IsNullOrEmpty(correctPasscode) || passcode == correctPasscode)
            {
                // Passcode is disabled, or it is correct. Continue
            }
            else
            {
                throw new AuthenticationException("Wrong passcode");
            }
        }
    }
}