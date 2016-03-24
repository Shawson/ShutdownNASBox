Shutdown NAS Box
================

This was a real simple utility I quickly knocked up in 2011.  It's purpose was to automatically 
shut down my Netgear ReadyNAS Duo when my main PC shut down.  Using Firebug I inspected the http calls
made when issuing a shut down command using the management portal for the NAS box and this app just 
mimics that- then I set a shut down event on my PC to fire this app on shut down of my PC.

Usage
-----

    ShutdownNASBox -h <nas_box_hostname> -u <username> -p <password>
    
