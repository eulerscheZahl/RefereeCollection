# CodinGame Referee Collection
This is an unofficial collection of referee programs for CodinGame multiplayer games, compatible to [brutaltester](https://github.com/dreignier/cg-brutaltester).
The referees are not ported from the code used on the servers, so correctness can't be garanteed. This applied especially for, but is not limited to map generation.

The referees support some basic graphic feedback.
To activate it, pass a folder (or `.` for the current directory) as first command line argument and you will get an image for each turn.
This is not recommended when playing multiple turns, as it is slow and images will be replaced by later runs.

# Getting started
The referees are written in C#. On Windows the sould run out of the box (Windows XP with service pack 3 or above). If not, check for updates of the .NET framework.
On Linux install the mono package with `apt-get install mono` (Ubuntu) to execute the referees.

# Not implemented
## HyperSonic
MOVE doesn't find shortest paths. It works fine, as long as you always go to adjacent cells.
When you use the referee to optimize you bot, you most likely use your own pathfinding anyway.
