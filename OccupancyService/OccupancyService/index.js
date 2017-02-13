function RoomsViewModel() {
    this.rooms = ko.observableArray([]);
    this.connectionStatusText = ko.observable("Connecting...");
    this.connectionStatusClass = ko.observable("label-default");
    this.reconnectButtonVisible = ko.observable(false);
    this.reconnect = () => {
        refreshRoomList();
        connectToSignalR();
    }
}
function RoomViewModel(id, description, isOccupied, lastUpdate) {
    var self = this;
    self.Id = ko.observable(id);
    self.Description = ko.observable(description);
    self.IsOccupied = ko.observable(isOccupied);
    self.LastUpdate = ko.observable(lastUpdate);

    self.IsAvailable = ko.computed(() => {
        return !self.IsOccupied();
    });
    self.IsUnavailable = ko.computed(() => {
        return self.IsOccupied();
    });
    self.IsUnknown = ko.computed(() => {
        return false;
    });
}

var viewModel = new RoomsViewModel();

function refreshRoomList() {
    $.get("Rooms")
        .done((data) => {
            viewModel.rooms([]);
            data.forEach((room) => {
                var roomViewModel = new RoomViewModel(room.Id, room.Description, room.IsOccupied, room.LastUpdate);
                viewModel.rooms.push(roomViewModel);
            });
        })
        .fail(() => {
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
        .done(() => {
            viewModel.connectionStatusText("Connected");
            viewModel.connectionStatusClass("label-success");
            viewModel.reconnectButtonVisible(false);
        })
        .fail(() => {
            viewModel.connectionStatusText("Could not connect");
            viewModel.connectionStatusClass("label-danger");
            viewModel.reconnectButtonVisible(true);
        });
}

// Set up SignalR callbacks
$.connection.hub.connectionSlow(() => {
    viewModel.connectionStatusText("Slow connection");
    viewModel.connectionStatusClass("label-warning");
    viewModel.reconnectButtonVisible(false);
});
$.connection.hub.reconnecting(() => {
    viewModel.connectionStatusText("Reconnecting...");
    viewModel.connectionStatusClass("label-warning");
    viewModel.reconnectButtonVisible(false);
});
$.connection.hub.reconnected(() => {
    viewModel.connectionStatusText("Reconnected");
    viewModel.connectionStatusClass("label-success");
    viewModel.reconnectButtonVisible(false);
    refreshRoomList(); // Refresh list & status, since we could have missed events
});
$.connection.hub.disconnected(() => {
    if (viewModel.connectionStatusText() !== "Could not connect") {
        viewModel.connectionStatusText("Disconnected");
        viewModel.connectionStatusClass("label-danger");
        viewModel.reconnectButtonVisible(true);
    }
});
$.connection.roomsHub.client.roomsChanged = (changeType, newRooms) => {
    newRooms.forEach((newRoom) => {
        var newRoomViewModel = new RoomViewModel(newRoom.Id, newRoom.Description, newRoom.IsOccupied, newRoom.LastUpdate);
        for (var i = 0; i < viewModel.rooms().length; i++) {
            var oldRoomViewModel = viewModel.rooms()[i];
            if (changeType === "updated" && oldRoomViewModel.Id() === newRoomViewModel.Id()) {
                // Update room
                viewModel.rooms.splice(i, 1);
                viewModel.rooms.splice(i, 0, newRoomViewModel);
                //viewModel.rooms.replace(oldRoomViewModel, newRoomViewModel);
                return;
            } else if (changeType === "deleted" && oldRoomViewModel.Id() === newRoomViewModel.Id()) {
                // Delete room
                viewModel.rooms.splice(i, 1);
                return;
            } else if (changeType === "new" && oldRoomViewModel.Id() > newRoomViewModel.Id()) {
                // Add new room in middle of array
                viewModel.rooms.splice(i, 0, newRoomViewModel);
                return;
            }
        }

        // Handle new room at the end
        if (changeType === "new") {
            // Add new room at end of array
            viewModel.rooms.push(newRoomViewModel);
            return;
        }
    });
};

// Reconnect automatically on window focus, if disconnected
$(window).focus(() => {
    if (viewModel.reconnectButtonVisible()) {
        viewModel.reconnect();
    }
});

// Workaround to reconnect automatically if page is suspended for more than 5 seconds (mobile)
var lastFired = new Date().getTime();
setInterval(() => {
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

