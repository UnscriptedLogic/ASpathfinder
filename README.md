### A* Pathfinding Project
#### Current capabilities:
1) can calculate full path to end point
2) can work in square mazes (circular mazes not tested)
3) decide how far from each node to check for an obstacle (width accounting)
4) can dynamically change its path when an obstacle appears after initial path calculation

#### Future Updates:
1) "Chunk Scanning" instead of "Full Path" calculation
   - Full Path scan
   - Split that full path into chunks with their own start and end (e.g. 10 nodes per chunk)
   - each chunk only calculates to their end point
   - if a chunk no longer can reach its end point, from the chunk's start point, repeat from point 1
   - Can test if "time interval refresh" or "event-based refresh" is more optimized

2) Size optimization
   - currently the character relies on the nodes calculation distance.
   - would like to let the nodes know if a path is viable but the character cannot fit
   - nodes could run into small holes but the player may not be able to fit

3) 3D Version
   - will account for bridges and underpasses
   - to be examined

This project will greatly compliment 2 other personal upcoming projects
   - Maze generation algorithm (seems okay)
   - Marching cubes algorithm (what the f***)
