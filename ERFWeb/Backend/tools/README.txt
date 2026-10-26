ERFWeb - Nightly database backup and health monitor
====================================================

WHAT THIS IS
------------
Two Windows scheduled tasks on the ERFWeb API server:

  ERFWeb Database Backup   every night (default 01:00)
                           - copies the whole ERFWeb PostgreSQL database to a .dump file
                           - checks that the file is complete and readable
                           - optionally copies it to a network share
                           - deletes backups older than 14 days
                           - emails you if anything fails

  ERFWeb Health Check      every 5 minutes
                           - checks the ERFWeb health address (API + database)
                           - emails "ERFWeb is DOWN" / "ERFWeb is back up"
                           - emails "Database backup is MISSING" if no backup in 26 hours

Windows Task Scheduler runs them. They are NOT part of the ERFWeb API and keep working
when the API is stopped or redeployed. They read the database login and the email
settings from the API's appsettings.json, so no password is stored twice.


FILES NEEDED (keep all 5 together in one folder)
-----------------------------------------------
  Install-ErfWebMaintenance.ps1    sets everything up (the one you run)
  Install-ErfWebMaintenance.cmd    optional double-click launcher for the line above
  Backup-ErfWebDatabase.ps1        the nightly backup (run by Windows)
  Test-ErfWebHealth.ps1            the 5-minute check (run by Windows)
  ErfWebMaintenance.Common.ps1     helpers used by the two scripts above

Other files that may sit in the same folder (Publish-ErfWeb, HOW-TO-DEPLOY, ...) are for
deploying the API and are not used here.


IN THIS GUIDE
-------------
Replace everything written like <THIS> with your own value. Nothing below depends on where
you put the files; you always run the commands from inside that folder.

  <TOOLS FOLDER>     the folder where you put the 5 files
  <API FOLDER>       the ERFWeb API folder that holds appsettings.json
                     (on the current server: C:\inetpub\wwwroot\erf_web-app)
  <BACKUP FOLDER>    where the backups go, on a drive that exists, e.g. C:\ERFWeb-Backups
  <PG FOLDER>        the folder that holds pg_dump.exe and pg_restore.exe (Step 3)
  <SHARE>            optional network folder for a second copy, e.g. \\192.168.4.119\share\erfweb
  <EMAIL>            who gets the alert emails


====================================================================================
STEP 1 - Copy the files to the API server
====================================================================================
1. Sign in to the API server (Remote Desktop) with an account that is an administrator.
2. Make a folder anywhere, e.g. C:\Temp\erfweb-maintenance. This is <TOOLS FOLDER>.
3. Copy the 5 files above into it.


====================================================================================
STEP 2 - Open an administrator PowerShell in that folder
====================================================================================
1. Start menu > type "PowerShell" > right-click "Windows PowerShell" > Run as administrator.
2. Go to the folder:

     cd "<TOOLS FOLDER>"

3. Check the 5 files are there:

     dir *.ps1, *.cmd

TIP: if the window title starts with "Select", you clicked inside it and it is paused.
     Press Esc.


====================================================================================
STEP 3 - Find pg_dump.exe (PostgreSQL's backup tool)
====================================================================================
The backup needs pg_dump.exe and pg_restore.exe of the SAME major version as the
database server, or newer (the database is PostgreSQL 18, so 18 or higher).

1. Search for it:

     Get-ChildItem 'C:\Program Files' -Recurse -Filter pg_dump.exe -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName; & $_.FullName --version }

   pgAdmin 4 brings its own copy, e.g.
     C:\Program Files\PostgreSQL\18\pgAdmin 4\runtime\pg_dump.exe
   The folder that holds it (without \pg_dump.exe) is <PG FOLDER>.

2. Nothing found? Install the PostgreSQL "Command Line Tools" (PostgreSQL installer for
   Windows, tick only "Command Line Tools") and search again.

3. Check both tools and their version (must say 18 or higher):

     & "<PG FOLDER>\pg_dump.exe" --version; & "<PG FOLDER>\pg_restore.exe" --version

NOTE: pg_dump has no password of its own and you never type one. The script gives it the
      database password from appsettings.json. If a window asks for a password, close it
      (Ctrl+C).


====================================================================================
STEP 4 - Choose the backup folder
====================================================================================
Pick a folder on a drive that EXISTS on this server, e.g. C:\ERFWeb-Backups.
(The default is D:\ERFWeb-Backups; many servers have no D: drive.)
The installer creates it. This is <BACKUP FOLDER>.


====================================================================================
STEP 5 - (Optional) A network share for a second copy
====================================================================================
Skip this step for backups on this server only; you can add the share later.

1. Use the full network path starting with \\ , NOT a drive letter like X:
   (the task cannot see drive letters you mapped). To see the path behind a drive letter:

     net use X:

   and read "Remote name".

2. The account the task runs as must be allowed to write there:
   - Server on the DOMAIN: ask IT to give this server's computer account write access,
     or use a domain account that has access in Step 6 (-RunAsUser).
   - Server NOT on the domain (local workgroup) but the share is: save your domain login
     for the share under a LOCAL account, and run the task as that local account:
       a. Sign in to the server as that local account and see its name:
            whoami
       b. In a normal (not "Run as administrator") PowerShell of that account:
            cmdkey /add:<SHARE SERVER NAME OR IP> /user:<DOMAIN>\<USER> /pass
          It asks for the DOMAIN password.
       c. Check that it can write:
            New-Item "<SHARE>\test.txt" -ItemType File -Force; Remove-Item "<SHARE>\test.txt"
          No red error = OK.
       d. In Step 6 add:  -RunAsUser "<SERVER NAME>\<LOCAL USER>"


====================================================================================
STEP 6 - Install the two tasks
====================================================================================
In the administrator PowerShell from Step 2 (still in <TOOLS FOLDER>), run ONE line.

Backups on this server only:

  powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-ErfWebMaintenance.ps1 -InstallPath "<API FOLDER>" -BackupDir "<BACKUP FOLDER>" -PgBin "<PG FOLDER>" -AlertTo "<EMAIL>"

With a network share as well:

  powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-ErfWebMaintenance.ps1 -InstallPath "<API FOLDER>" -BackupDir "<BACKUP FOLDER>" -PgBin "<PG FOLDER>" -AlertTo "<EMAIL>" -CopyTo "<SHARE>"

Example (current server, local only):

  powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-ErfWebMaintenance.ps1 -InstallPath "C:\inetpub\wwwroot\erf_web-app" -BackupDir "C:\ERFWeb-Backups" -PgBin "C:\Program Files\PostgreSQL\18\pgAdmin 4\runtime" -AlertTo "jca.mdmpi@marsmandrysdale.com"

More options you can add to the end of the line:
  -RunAsUser "<ACCOUNT>"           run the tasks as this account (asks for its password);
                                   without it they run as SYSTEM
  -BackupTime "02:00"              another time for the nightly backup
  -KeepDays 30                     keep backups longer than 14 days
  -AlertTo "a@x.com","b@x.com"     several alert addresses
  -HealthUrl "<URL>"               another health address (default https://sr.mdmpi.com.ph/erf/health)

Success looks like:
  Registered 'ERFWeb Database Backup' (every night at 01:00) and 'ERFWeb Health Check' (every 5 minutes).

The installer copies the scripts to C:\ProgramData\ERFWeb\tools and the tasks run them
from there, so you may delete <TOOLS FOLDER> afterwards (keep it if you expect to change
settings).

DOUBLE-CLICK INSTEAD (no typing in PowerShell):
  1. Right-click Install-ErfWebMaintenance.cmd > Edit (opens in Notepad).
  2. Change only the lines between "SETTINGS" and "END OF SETTINGS":
       INSTALL_PATH   = <API FOLDER>
       BACKUP_DIR     = <BACKUP FOLDER>
       PG_BIN         = <PG FOLDER>
       ALERT_TO       = <EMAIL>
       COPY_TO        = <SHARE>, or leave empty for backups on this server only
       RUN_AS_USER    = an account to run the tasks as, or leave empty for SYSTEM
     They are filled in with the current server's values as an example.
  3. Save, double-click the .cmd and click Yes.
  It finds Install-ErfWebMaintenance.ps1 in its own folder, wherever that is. Do NOT paste
  its "%~dp0..." line into PowerShell; that only works inside the .cmd file.


====================================================================================
STEP 7 - Run the first backup now
====================================================================================
  Start-ScheduledTask -TaskName 'ERFWeb Database Backup'

It prints nothing; it runs in the background. Wait about a minute.


====================================================================================
STEP 8 - Check the result
====================================================================================
  Get-Content "<BACKUP FOLDER>\backup-log.txt" -Tail 10

OK:
  Backing up erms_db on ... to <BACKUP FOLDER>\erms_db_yyyyMMdd_HHmm.dump.
  Backup OK: erms_db_yyyyMMdd_HHmm.dump (... MB, ... tables with data).
  Copied to <SHARE>.                  (only with -CopyTo)

Not OK: a line "BACKUP FAILED: <reason>" (and an alert email). See TROUBLESHOOTING.


====================================================================================
STEP 9 - (Optional) Check that alert emails arrive
====================================================================================
Forces one harmless failure (a tools folder that does not exist):

  powershell -NoProfile -ExecutionPolicy Bypass -File C:\ProgramData\ERFWeb\tools\Backup-ErfWebDatabase.ps1 -InstallPath "<API FOLDER>" -BackupDir "<BACKUP FOLDER>" -PgBin C:\does-not-exist -AlertTo "<EMAIL>"

You should receive "[ERFWeb] Database backup FAILED". No email = check the Smtp section of
appsettings.json.


====================================================================================
STEP 10 - The next morning
====================================================================================
  Get-Content "<BACKUP FOLDER>\backup-log.txt" -Tail 5

There should be a "Backup OK" line from about 01:00. You can also open Task Scheduler >
Task Scheduler Library > "ERFWeb Database Backup" > History.

From then on it runs by itself. You only hear from it when something is wrong.


CHANGE SETTINGS / REMOVE
------------------------
Change: run Step 6 again with the new options. It replaces both tasks.
Remove: run Step 6 with -Uninstall at the end. Backup files are kept.

  powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-ErfWebMaintenance.ps1 -Uninstall


TROUBLESHOOTING
---------------
"pg_dump.exe not found"
    Step 3: add -PgBin "<PG FOLDER>" and run Step 6 again.

"server version mismatch" / "aborting because of server version mismatch"
    The tools are older than the database server. Install newer Command Line Tools.

"Access is denied" (after "Backup OK")
    The local backup is fine; only the copy to the share failed. See Step 5, or run
    Step 6 without -CopyTo for now. While the copy keeps failing, every night sends a
    FAILED email and old backups are not deleted.

"password authentication failed" / cannot connect
    Check ConnectionStrings:Postgres in <API FOLDER>\appsettings.json.

"Cannot find path ... D:\..."
    The server has no D: drive. Use -BackupDir with a drive that exists (Step 4).

"The argument '%~dp0Install-ErfWebMaintenance.ps1' ... does not exist"
    You pasted the .cmd line into PowerShell. Use the Step 6 line (.\Install-...) instead.

"ERFWeb is DOWN" email
    Open the health address in a browser, then read the newest file in <API FOLDER>\logs.

Health check log (changes only): C:\ProgramData\ERFWeb\health-log.txt
Backup log:                      <BACKUP FOLDER>\backup-log.txt


RESTORE A BACKUP (only on purpose; this replaces data)
------------------------------------------------------
Use pg_restore from <PG FOLDER>. Look first: restore into a NEW, empty database:

  & "<PG FOLDER>\psql.exe" -h <DB HOST> -U <DB USER> -d postgres -c "CREATE DATABASE erms_db_restored"
  & "<PG FOLDER>\pg_restore.exe" -h <DB HOST> -U <DB USER> -d erms_db_restored --no-owner "<BACKUP FOLDER>\erms_db_yyyyMMdd_HHmm.dump"

To put a backup back into erms_db itself:
  1. Stop the API:            Stop-Service ERFWeb.Api
  2. Back up the current state first (Step 7).
  3. & "<PG FOLDER>\pg_restore.exe" -h <DB HOST> -U <DB USER> -d erms_db --clean --if-exists --no-owner "<backup file>"
  4. Start the API:           Start-Service ERFWeb.Api

More detail: "Backups and monitoring" in ERFWeb/README.md.
