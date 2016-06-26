function RoomsViewModel() {
    this.rooms = ko.observableArray([]);
    this.connectionStatus = ko.observable("Connecting...");
}

var viewModel = new RoomsViewModel();
ko.applyBindings(viewModel);

// Set up SignalR-connection
var occupancyHub = $.connection.occupancyHub;
occupancyHub.client.occupancyChanged = function (roomId, isOccupied) {
    for (var i = 0; i < viewModel.rooms().length; i++) {
        var room = viewModel.rooms()[i];
        if (room.Id === roomId) {
            // Update isOccupied
            var updatedRoom = {Id: roomId, Description: room.Description, IsOccupied: isOccupied};
            viewModel.rooms.replace(room, updatedRoom);
        }
    }
};

$.get("Rooms")
  .done(function (data) {
      viewModel.rooms(data);

      // Start the connection.
      $.connection.hub.start()
          .done(function () {
              viewModel.connectionStatus("Connected");
          })
          .fail(function () {
              viewModel.connectionStatus("Could not connect");
          });
      $.connection.hub.connectionSlow(function () {
          viewModel.connectionStatus("Slow connection");
      });
      $.connection.hub.reconnecting(function () {
          viewModel.connectionStatus("Reconnecting...");
      });
      $.connection.hub.reconnected(function () {
          viewModel.connectionStatus("Reconnected");
      });
      $.connection.hub.disconnected(function () {
          viewModel.connectionStatus("Disconnected");
      });
  })
  .fail(function () {
      viewModel.connectionStatus("No response from server");
  });
