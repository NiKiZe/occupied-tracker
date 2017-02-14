class RoomsViewModel {
    rooms: KnockoutObservableArray<RoomViewModel>;
    connectionStatusText: KnockoutObservable<string>;
    connectionStatusClass: KnockoutObservable<string>;
    reconnectButtonVisible: KnockoutObservable<boolean>;
    timeNow: KnockoutObservable<any>; // TODO: Fix moment typings

    constructor() {
        this.rooms = ko.observableArray(<RoomViewModel[]>[]);
        this.connectionStatusText = ko.observable("Connecting...");
        this.connectionStatusClass = ko.observable("label-default");
        this.reconnectButtonVisible = ko.observable(false);
        this.timeNow = ko.observable(moment());

        // Update timer every minute
        window.setInterval(() => { this.timeNow(moment()); }, 60000);
    }

    reconnect() {
        refreshRoomList();
        connectToSignalR();
    }
}

interface IRoom {
// ReSharper disable InconsistentNaming
    Id: number;
    Description: string;
    IsOccupied: boolean;
    LastUpdate: string;
// ReSharper restore InconsistentNaming
}

class RoomViewModel {
    id: KnockoutObservable<number>;
    description: KnockoutObservable<string>;
    isOccupied: KnockoutObservable<boolean>;
    lastUpdate: KnockoutObservable<any>; // TODO: Fix moment typings

    isOld: KnockoutComputed<boolean>;
    isAvailable: KnockoutComputed<boolean>;
    isUnavailable: KnockoutComputed<boolean>;

    constructor(id: number, description: string, isOccupied: boolean, lastUpdate: string) {
        this.id = ko.observable(id);
        this.description = ko.observable(description);
        this.isOccupied = ko.observable(isOccupied);
        this.lastUpdate = ko.observable(moment(lastUpdate));

        this.isOld = ko.computed(() => {
            var timeLimit = viewModel.timeNow().clone().subtract(60, 'minutes');
            return this.lastUpdate().isBefore(timeLimit);
        });
        this.isAvailable = ko.computed(() => {
            return !this.isOld() && !this.isOccupied();
        });
        this.isUnavailable = ko.computed(() => {
            return !this.isOld() && this.isOccupied();
        });
    }
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
$.connection.roomsHub.client.roomsChanged = (changeType: string, newRooms: IRoom[]) => {
    newRooms.forEach((newRoom: IRoom) => {
        var newRoomViewModel = new RoomViewModel(newRoom.Id, newRoom.Description, newRoom.IsOccupied, newRoom.LastUpdate);
        for (var i = 0; i < viewModel.rooms().length; i++) {
            var oldRoomViewModel = viewModel.rooms()[i];
            if (changeType === "updated" && oldRoomViewModel.id() === newRoomViewModel.id()) {
                // Update room
                viewModel.rooms.splice(i, 1);
                viewModel.rooms.splice(i, 0, newRoomViewModel);
                return;
            } else if (changeType === "deleted" && oldRoomViewModel.id() === newRoomViewModel.id()) {
                // Delete room
                viewModel.rooms.splice(i, 1);
                return;
            } else if (changeType === "new" && oldRoomViewModel.id() > newRoomViewModel.id()) {
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
    var now = new Date().getTime();
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

