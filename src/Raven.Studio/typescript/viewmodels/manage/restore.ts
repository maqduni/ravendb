import viewModelBase = require("viewmodels/viewModelBase");
import shell = require("viewmodels/shell");
import database = require("models/resources/database");
import getDocumentWithMetadataCommand = require("commands/database/documents/getDocumentWithMetadataCommand");
import appUrl = require("common/appUrl");
import resource = require("models/resources/resource");
import filesystem = require("models/filesystem/filesystem");
import monitorRestoreCommand = require("commands/maintenance/monitorRestoreCommand");
import startDbRestoreCommand = require("commands/maintenance/startRestoreCommand");
import startFsRestoreCommand = require("commands/filesystem/startRestoreCommand");

class resourceRestore {
    defrag = ko.observable<boolean>(false);
    backupLocation = ko.observable<string>("");
    resourceLocation = ko.observable<string>();
    indexesLocation = ko.observable<string>();
    journalsLocation = ko.observable<string>();
    resourceName = ko.observable<string>();
    nameCustomValidityError: KnockoutComputed<string>;

    restoreStatusMessages = ko.observableArray<string>();
    
    keepDown = ko.observable<boolean>(false);

    constructor(private parent: restore, private type: string, private resources: KnockoutObservableArray<resource>) {
        this.nameCustomValidityError = ko.computed(() => {
            var errorMessage = "";
            var newResourceName = this.resourceName();
            var foundDb = resources.first((rs: resource) => newResourceName == rs.name);

            if (!!foundDb && newResourceName.length > 0) {
                errorMessage = (this.type === database.type ? "Database" : "File System") + " name already exist!";
            }

            return errorMessage;
        });
    }

    toggleKeepDown() {
        this.keepDown.toggle();
        if (this.keepDown()) {
            var logsPre = document.getElementById(this.type + "RestoreLogPre");
            logsPre.scrollTop = logsPre.scrollHeight;
        }
    }

    updateRestoreStatus(newRestoreStatus: restoreStatusDto) {
        this.restoreStatusMessages(newRestoreStatus.Messages);
        if (this.keepDown()) {
            var logsPre = document.getElementById(this.type + "RestoreLogPre");
            logsPre.scrollTop = logsPre.scrollHeight;
        }
        this.parent.isBusy(newRestoreStatus.State === "Running");
    }
}

class restore extends viewModelBase {
    private dbRestoreOptions = new resourceRestore(this, database.type, shell.databases);
    private fsRestoreOptions = new resourceRestore(this, filesystem.type, shell.fileSystems);

    disableReplicationDestinations = ko.observable<boolean>(false);
    generateNewDatabaseId = ko.observable<boolean>(false);

    isForbidden = ko.observable<boolean>();
    isBusy = ko.observable<boolean>();
    anotherRestoreInProgres = ko.observable<boolean>(false);

    canActivate(args): any {
        var deferred = $.Deferred();

        this.isForbidden(shell.isGlobalAdmin() === false);
        if (this.isForbidden() === false) {
            this.isBusy(true);
            var db = appUrl.getDatabase();
            var self = this;

            new getDocumentWithMetadataCommand("Raven/Restore/InProgress", db, true).execute()
                .fail(() => deferred.resolve({ redirect: appUrl.forSettings(db) }))
                .done((result) => {
                    if (result) {
                        // looks like another restore is in progress
                        this.anotherRestoreInProgres(true);
                        new monitorRestoreCommand($.Deferred(), s => {
                            self.dbRestoreOptions.updateRestoreStatus.bind(self.dbRestoreOptions)(s);
                            self.fsRestoreOptions.updateRestoreStatus.bind(self.fsRestoreOptions)(s);
                        })
                            .execute()
                            .always(() => {
                                $("#databaseRawLogsContainer").resize();
                                $("#filesystemRawLogsContainer").resize();
                                this.anotherRestoreInProgres(false);
                            });

                    } else {
                        this.isBusy(false);
                    }
                    deferred.resolve({ can: true });
                });
        } else {
            deferred.resolve({ can: true });
        }

        return deferred;
    }

    activate(args) {
        super.activate(args);
        this.updateHelpLink("FT7RV6");
    }

    startDbRestore() {
        this.isBusy(true);
        var self = this;

        var restoreDatabaseDto: databaseRestoreRequestDto = {
            BackupLocation: this.dbRestoreOptions.backupLocation(),
            DatabaseLocation: this.dbRestoreOptions.resourceLocation(),
            DatabaseName: this.dbRestoreOptions.resourceName(),
            DisableReplicationDestinations: this.disableReplicationDestinations(),
            GenerateNewDatabaseId: this.generateNewDatabaseId(),
            IndexesLocation: this.dbRestoreOptions.indexesLocation(),
            JournalsLocation: this.dbRestoreOptions.journalsLocation()
        };

        new startDbRestoreCommand(this.dbRestoreOptions.defrag(), restoreDatabaseDto, self.dbRestoreOptions.updateRestoreStatus.bind(self.dbRestoreOptions))
            .execute()
            .always(() => shell.reloadDatabases());
    }

    startFsRestore() {
        this.isBusy(true);
        var self = this;

        var restoreFilesystemDto: filesystemRestoreRequestDto = {
            BackupLocation: this.fsRestoreOptions.backupLocation(),
            FilesystemLocation: this.fsRestoreOptions.resourceLocation(),
            FilesystemName: this.fsRestoreOptions.resourceName(),
            IndexesLocation: this.fsRestoreOptions.indexesLocation(),
            JournalsLocation: this.fsRestoreOptions.journalsLocation()
        };

        new startFsRestoreCommand(this.fsRestoreOptions.defrag(), restoreFilesystemDto, self.fsRestoreOptions.updateRestoreStatus.bind(self.fsRestoreOptions))
            .execute()
            .always(() => shell.reloadFileSystems());
    }
}

export = restore;  