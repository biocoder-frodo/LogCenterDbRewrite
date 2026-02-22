# LogCenterDbRewrite
 Shrink and merge LogCenter database files created with FRITZBox-SynologyLogCenterDaemon, or merge LogCenter database files from other sources.

If you have left FRITZBox-SynologyLogCenterDaemon running for a long time, you may find that the database has grown very large and that some sections of logs are repeated over and over again.
My new printer logged different hostnames to LogCenter, creating multiple databases in the process, so I modified the code for this tool to also just plainly merge several LogCenter database files into one.

This offline tool lets you reprocess the FritzBOX log database so that 
* duplicate entries are removed
* startup events with a timestamp in 2070 are adjusted to the time of recording
* repeated events are trimmed, only the last occurence will appear in the database

## How to use the tool
* Disable the port receiving the syslog packets in LogCenter, this will close the database.
* Download/copy the database 
* Reprocess the file(s)
* The output will be written to the folder of the first input database, with a filename that ends in .DB.processed.DB.
* Copy the reprocessed file onto the existing database file using elevated privileges (sudo)
* Enable the port receiving syslog packets in LogCenter.

```
.\LogCenterDbRewrite.exe --?
Usage : ./LogCenterRewrite options [[database] database2 ....]

Perform a merge of the input databases. Can reprocess FritzBOX Syslog databases with the 'fritz' option.

Options:
        fritz           Adjusts timestamps from 1970 in the log(s) and reprocesses repeating messages.
        dlh             Exports the link speed history of your DSL connection.
        dsl-link-history
>
```
## Merging
Run LogCenterRewrite by giving it at least one input database to work with:
```
.\LogCenterDbRewrite.exe printer\SYNOSYSLOGDB_printer.DB printer_ip\SYNOSYSLOGDB_printer_ip.DB default_host\SYNOSYSLOGDB_default_host.DB
>
```

## Reprocessing
Run LogCenterRewrite by giving it at least one input database to work with:

```
.\LogCenterDbRewrite.exe fritz fritz.box\SYNOSYSLOGDB_fritz.box.DB
>
```
You can also specify the folder where the database file is stored:

```
.\LogCenterDbRewrite.exe fritz fritz.box
>
```


