# LogCenterDbRewrite
 Shrink and merge LogCenter database files created with FRITZBox-SynologyLogCenterDaemon

If you have left FRITZBox-SynologyLogCenterDaemon running for a long time, you may find that the database has grown very large and that some sections of logs are repeated over and over again.

This offline tool let's you reprocess the database so that 
* duplicate entries are removed
* startup events with a timestamp in 2070 are adjusted to the time of recording
* repeated events are trimmed, only the last occurence will appear in the database

## How to use the tool
* Disable the port receiving the syslog packets in LogCenter, this will close the database.
* Download/copy the database 
* Reprocess the file(s)
* Copy the reprocessed file onto the existing database file using elevated privileges (sudo)
* Enable the port receiving syslog packets in LogCenter.

## Reprocessing
Run LogCenterRewrite by giving it at least one input database to work with:

```
.\LogCenterDbRewrite.exe --?
Usage : ./LogCenterRewrite [[database] database2 ....] options
Options:
dlh, dsl-link-history           Exports the link speed history of your DSL connection.
```