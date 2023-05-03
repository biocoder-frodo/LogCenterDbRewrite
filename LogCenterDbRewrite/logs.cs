namespace Sqlite.Synology.LogCenter
{
    //cat /var/packages/LogCenter/target/service/schema/accinfo.sql
    //cat /var/packages/LogCenter/target/service/schema/loginfo2.sql
    //cat /var/packages/LogCenter/target/service/schema/synosyslog.sql

    enum logs
    {
        id,
        host,
        ip,
        fac,
        prio,
        llevel,
        tag,
        utcsec,
        r_utcsec,
        tzoffset,
        ldate,
        ltime,
        prog,
        msg,
    }
}