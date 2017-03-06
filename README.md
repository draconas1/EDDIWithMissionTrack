# EDDI with Mission Tracker: The Elite Dangerous Data Interface

Current version: 1.1 (EDDI 2.2.0)

For how it works, see the main EDDI page https://github.com/cmdrmcdonald/EliteDangerousDataProvider


This provides a new tab (mission tracker) that can spawn a new window, designed for playing Elite with multiple monitors.  Basically I got tried of trying to mission manage using the in game UI once I ahd more than 1 or 2 missions on the go.

# Functions

## Mission Tracking

This is the main function.  The top grid listens to the various mission events and keeps track, allowing you to see what missions you have accepted at a glace and sort them.  

The grid to the right keeps a track of what cargo is required for Collect X missions and what is in your cargo bay.  It attempts to flag when the required cargo is available at a station you have arrived at, but that functionality is a bit hit and miss.

Missions are added and removed automatically, but can be manually deleted using the relevant button.

### PROBLEM

There is no journal event for "mission has changed", so if your mission is updated halfway through (like a new station) the tracker has no way to know.

## Listing Distant Stations

I don't like taking missions to far away stations - things more than 1500ls from the jump point.  The time spent cruising almost always outweighs the benefit of the mission.

Unforuntately there is no way for an external program like the tracker to know about a mission before you accept it.  So to counter this I have the problematic station finder, this lists ALL stations within a set LY distance that are more than a set distance away from the parent star. 

This list is manually populated as it takes a few seconds and locks up the UI whilst doing so.

For this to work you need to have populated the system and station database from EDDB using the EDDB JSONL system and station data.  You also need to have signed up for the Elite Dangerous Star Map service and configured the EDSM plugin wiht your name and API Key.