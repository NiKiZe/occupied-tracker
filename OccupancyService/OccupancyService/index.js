function RoomsViewModel() {
    this.rooms = ko.observableArray([]);
    this.connectionStatusText = ko.observable("Connecting...");
    this.connectionStatusClass = ko.observable("label-default");
    this.reconnectButtonVisible = ko.observable(false);
    this.reconnect = function () {
        refreshRoomList();
        connectToSignalR();
    }
}
var viewModel = new RoomsViewModel();

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
}

// Set up SignalR callbacks
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
$.connection.roomsHub.client.roomsChanged = function (changeType, newRooms) {
    newRooms.forEach(function(newRoom) {
        for (var i = 0; i < viewModel.rooms().length; i++) {
            var room = viewModel.rooms()[i];
            if (changeType === "updated" && room.Id === newRoom.Id) {
                // Update room
                viewModel.rooms.replace(room, newRoom);
                return;
            } else if (changeType === "deleted" && room.Id === newRoom.Id) {
                // Delete room
                viewModel.rooms.splice(i, 1);
                return;
            } else if (changeType === "new" && room.Id > newRoom.Id) {
                // Add new room in middle of array
                viewModel.rooms.splice(i, 0, newRoom);
                return;
            }
        }

        // Handle new room at the end
        if (changeType === "new") {
            // Add new room at end of array
            viewModel.rooms.push(newRoom);
            return;
        }
    });
};

// Reconnect automatically on window focus, if disconnected
$(window).focus(function () {
    if (viewModel.reconnectButtonVisible()) {
        viewModel.reconnect();
    }
});

// Workaround to reconnect automatically if page is suspended for more than 5 seconds (mobile)
var lastFired = new Date().getTime();
setInterval(function () {
    now = new Date().getTime();
    if (now - lastFired > 5000 && viewModel.reconnectButtonVisible()) {//if it's been more than 5 seconds
        viewModel.reconnect();
    }
    lastFired = now;
}, 500);

// Set up and bind ViewModel
ko.applyBindings(viewModel);

// Get Room list and connect to SignalR
refreshRoomList();
connectToSignalR();

