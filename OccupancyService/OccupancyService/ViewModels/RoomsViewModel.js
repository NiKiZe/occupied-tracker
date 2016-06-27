function RoomsViewModel() {
    this.rooms = ko.observableArray([]);
    this.connectionStatusText = ko.observable("Connecting...");
    this.connectionStatusClass = ko.observable("label-default");
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
              viewModel.connectionStatusText("Connected");
              viewModel.connectionStatusClass("label-success");
          })
          .fail(function () {
              viewModel.connectionStatusText("Could not connect");
              viewModel.connectionStatusClass("label-danger");
          });
      $.connection.hub.connectionSlow(function () {
          viewModel.connectionStatusText("Slow connection");
          viewModel.connectionStatusClass("label-warning");
      });
      $.connection.hub.reconnecting(function () {
          viewModel.connectionStatusText("Reconnecting...");
          viewModel.connectionStatusClass("label-warning");
      });
      $.connection.hub.reconnected(function () {
          viewModel.connectionStatusText("Reconnected");
          viewModel.connectionStatusClass("label-success");
      });
      $.connection.hub.disconnected(function () {
          viewModel.connectionStatusText("Disconnected");
          viewModel.connectionStatusClass("label-danger");
      });
  })
  .fail(function () {
      viewModel.connectionStatusText("No response from server");
      viewModel.connectionStatusClass("label-danger");
  });
