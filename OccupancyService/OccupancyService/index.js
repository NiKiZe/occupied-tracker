function refreshRoomList() {
    $.get("Rooms")
        .done(function (data) {
            viewModel.rooms(data);
        })
        .fail(function () {
            viewModel.connectionStatusText("Could not refresh room list");
            viewModel.connectionStatusClass("label-danger");
            viewModel.reconnectButtonVisible(true);
        });
}

function connectToSignalR() {
    viewModel.connectionStatusText("Connecting...");
    viewModel.connectionStatusClass("label-default");
    viewModel.reconnectButtonVisible(false);

    $.connection.hub.start()
        .done(function () {
            viewModel.connectionStatusText("Connected");
            viewModel.connectionStatusClass("label-success");
            viewModel.reconnectButtonVisible(false);
        })
        .fail(function () {
            viewModel.connectionStatusText("Could not connect");
            viewModel.connectionStatusClass("label-danger");
            viewModel.reconnectButtonVisible(true);
        });
    $.connection.hub.connectionSlow(function () {
        viewModel.connectionStatusText("Slow connection");
        viewModel.connectionStatusClass("label-warning");
        viewModel.reconnectButtonVisible(false);
    });
    $.connection.hub.reconnecting(function () {
        viewModel.connectionStatusText("Reconnecting...");
        viewModel.connectionStatusClass("label-warning");
        viewModel.reconnectButtonVisible(false);
    });
    $.connection.hub.reconnected(function () {
        viewModel.connectionStatusText("Reconnected");
        viewModel.connectionStatusClass("label-success");
        viewModel.reconnectButtonVisible(false);
        refreshRoomList(); // Refresh list & status, since we could have missed events
    });
    $.connection.hub.disconnected(function () {
        if (viewModel.connectionStatusText() !== "Could not connect") {
            viewModel.connectionStatusText("Disconnected");
            viewModel.connectionStatusClass("label-danger");
            viewModel.reconnectButtonVisible(true);
        }
    });
}

function connect() {
    refreshRoomList();
    connectToSignalR();
}

function RoomsViewModel() {
    this.rooms = ko.observableArray([]);
    this.connectionStatusText = ko.observable("Connecting...");
    this.connectionStatusClass = ko.observable("label-default");
    this.reconnectButtonVisible = ko.observable(false);
    this.reconnect = function() {
        connect();
    }
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
            var updatedRoom = { Id: roomId, Description: room.Description, IsOccupied: isOccupied };
            viewModel.rooms.replace(room, updatedRoom);
        }
    }
};

// Connect to server
connect();
window.setInterval(function () {
    // Refresh room list every 1h
    refreshRoomList();
}, 3600000); // 1 * 60 * 60 * 1000 ms

