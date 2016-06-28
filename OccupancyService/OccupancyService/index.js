function RoomsViewModel() {
    this.rooms = ko.observableArray([]);
    this.connectionStatusText = ko.observable("Connecting...");
    this.connectionStatusClass = ko.observable("label-default");
}

var viewModel = new RoomsViewModel();
ko.applyBindings(viewModel);

function refreshRoomList() {
    $.get("Rooms")
        .done(function (data) {
            viewModel.rooms(data);
        })
        .fail(function () {
            viewModel.connectionStatusText("No response from server");
            viewModel.connectionStatusClass("label-danger");
        });
}

// Initialize room list
refreshRoomList();
window.setInterval(function () {
    // Refresh room list every 1h
    refreshRoomList();
}, 3600000); // 1 * 60 * 60 * 1000 ms

// Set up SignalR-connection
var occupancyHub = $.connection.occupancyHub;
occupancyHub.client.occupancyChanged = function (roomId, isOccupied) {
    for (var i = 0; i < viewModel.rooms().length; i++) {
        var room = viewModel.rooms()[i];
        if (room.Id === roomId) {
            // Update isOccupied
            var updatedRoom = { Id: roomId, Description: room.Description, IsOccupied: isOccupied };
            viewModel.rooms.replace(room, updatedRoom);
        }
    }
};
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
    refreshRoomList(); // Refresh list & status, since we could have missed events
});
$.connection.hub.disconnected(function () {
    viewModel.connectionStatusText("Disconnected");
    viewModel.connectionStatusClass("label-danger");
});
