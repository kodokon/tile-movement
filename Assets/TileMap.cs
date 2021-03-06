﻿using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class TileMap : MonoBehaviour {

	public GameObject selectedUnit;
	public TileType[] tileTypes;
	int[,] tiles;
	int mapSizeX = 10;
	int mapSizeY = 10;
	Node[,] graph;
	// Use this for initialization
	void Start () {
		selectedUnit.GetComponent<Unit>().tileX = (int) selectedUnit.transform.position.x;
		selectedUnit.GetComponent<Unit>().tileY = (int) selectedUnit.transform.position.y;
		selectedUnit.GetComponent<Unit>().map = this;

		GenerateMapData();
		GeneratePathfindingGraph();
		//Spawn visual prefabs
		GenerateMapVisual();

	}

	void GenerateMapData() {
		// Allocate our map tiles
		tiles = new int[mapSizeX,mapSizeY];
		
		int x,y;
		
		// Initialize our map tiles to be grass
		for(x=0; x < mapSizeX; x++) {
			for(y=0; y < mapSizeX; y++) {
				tiles[x,y] = 0;
			}
		}
		
		// Make a big swamp area
		for(x=3; x <= 5; x++) {
			for(y=0; y < 4; y++) {
				tiles[x,y] = 1;
			}
		}
		
		// Let's make a u-shaped mountain range
			tiles[4, 4] = 2;
			tiles[5, 4] = 2;
			tiles[6, 4] = 2;
			tiles[7, 4] = 2;
			tiles[8, 4] = 2;
			
			tiles[4, 5] = 2;
			tiles[4, 6] = 2;
			tiles[8, 5] = 2;
			tiles[8, 6] = 2;
			
	}
	
	public float CostToEnterTile (int sourceX, int sourceY, int targetX, int targetY) {

		TileType tt = 	tileTypes[tiles[targetX,targetY]];
		float cost = tt.movementCost;
		// Adds slightly larger diagonal movement costs
		if (sourceX!=targetX && sourceY!=targetY) {
			cost += 0.001f;
		}
		return cost;
	}

	// 
	void GeneratePathfindingGraph() {
		// Initialize the array
		graph = new Node[mapSizeX,mapSizeY];
		
		// Initialize a Node for each spot in the array
		for(int x=0; x < mapSizeX; x++) {
			for(int y=0; y < mapSizeX; y++) {
				graph[x,y] = new Node();
				graph[x,y].x = x;
				graph[x,y].y = y;
			}
		}
		
		// Now that all the nodes exist, calculate their neighbours
		for(int x=0; x < mapSizeX; x++) {
			for(int y=0; y < mapSizeX; y++) {
				// We have a 4-way connected map
				// This also works with 6-way hexes and 8-way tiles and n-way variable areas (like EU4)
				/*
				if(x > 0)
					graph[x,y].neighbours.Add( graph[x-1, y] );
				if(x < mapSizeX-1)
					graph[x,y].neighbours.Add( graph[x+1, y] );
				if(y > 0)
					graph[x,y].neighbours.Add( graph[x, y-1] );
				if(y < mapSizeY-1)
					graph[x,y].neighbours.Add( graph[x, y+1] );
				*/
				// This for eight-directional movement in squares or octagonal(?) tiles
				if(x > 0) {
					graph[x,y].neighbours.Add( graph[x-1, y] );
					if (y > 0) 
						graph[x,y].neighbours.Add( graph[x-1, y-1] );
					if (y < mapSizeY-1)
						graph[x,y].neighbours.Add( graph[x-1, y+1] );
				}

				if(x < mapSizeX-1) {
					graph[x,y].neighbours.Add( graph[x+1, y] );
					if (y > 0) 
						graph[x,y].neighbours.Add( graph[x+1, y-1] );
					if (y < mapSizeY-1)
						graph[x,y].neighbours.Add( graph[x+1, y+1] );
				}

				if(y > 0)
					graph[x,y].neighbours.Add( graph[x, y-1] );
				if(y < mapSizeY-1)
					graph[x,y].neighbours.Add( graph[x, y+1] );
			}
		}
	}
	
	void GenerateMapVisual() {
		for(int x=0; x < mapSizeX; x++) {
			for(int y=0; y < mapSizeX; y++) {
				TileType tt = tileTypes[ tiles[x,y] ];
				GameObject go = (GameObject)Instantiate( tt.tileVisualPrefab, new Vector3(x, y, 0), Quaternion.identity );
				
				ClickableTile ct = go.GetComponent<ClickableTile>();
				ct.tileX = x;
				ct.tileY = y;
				ct.map = this;
			}
		}
	}
	
	public Vector3 TileCoordToWorldCoord(int x, int y) {
		return new Vector3(x, y, 0);
	}
	
    // This is the A* algorithm in C#.
	public void GeneratePathTo(int x, int y) {
		// Clear out our unit's old path.
		Stopwatch sw = Stopwatch.StartNew();
		selectedUnit.GetComponent<Unit>().currentPath = null;

        Node source = graph[selectedUnit.GetComponent<Unit>().tileX, // Origin of pathfinding
                            selectedUnit.GetComponent<Unit>().tileY];
        Node target = graph[x, y];

        List<Node> closedSet = new List<Node>(); // Set of nodes already evaluated

        List<Node> openSet = new List<Node>(); // Set of nodes set to be evaluated

        openSet.Add(source);

        List<Node> currentPath = new List<Node>();
 
        // Graph of navigated nodes
        Dictionary<Node,Node> cameFrom = new Dictionary<Node, Node>();

        //g_score is the distance from source
        Dictionary<Node,float> gScore = new Dictionary<Node, float>();
        gScore[source] = 0;

        //f_score is distance from target
        Dictionary<Node, float> fScore = new Dictionary<Node, float>();
        fScore[source] = gScore[source] + HeuristicCostEstimate(source,target);
        selectedUnit.GetComponent<Unit>().currentPath = currentPath;

        foreach (Node u in graph)
        {
            if(u != source)
            {
                gScore[u] = Mathf.Infinity;
                fScore[u] = Mathf.Infinity;
            }
			cameFrom[u] = null;
        }

        while (openSet.Count > 0)
        {
            //Current is smallest node with f_value in openSet
            Node current = openSet[0];
            foreach(Node v in openSet)
            {
                if (fScore[v] < fScore[current])
                    current = v;
            }
			
            // If current == target, we are done
            if (current == target)
            {
				Node curr = target;
                while (curr != null)
                {
                    currentPath.Add(curr);
					curr = cameFrom[curr];
                }
				currentPath.Reverse();	
				selectedUnit.GetComponent<Unit>().currentPath = currentPath;
				sw.Stop();			
				UnityEngine.Debug.Log(sw.Elapsed.TotalMilliseconds);
				// All done :D
				return;
            }
            openSet.Remove(current);
            closedSet.Add(current);
            foreach(Node neighbour in current.neighbours)
            {
                if (closedSet.Contains(neighbour)) continue;

                float tentative_gScore = gScore[current] + CostToEnterTile(current.x, current.y, neighbour.x, neighbour.y);

                if ((!openSet.Contains(neighbour)) || (tentative_gScore < gScore[neighbour]))
                {
                    cameFrom[neighbour] = current;
                    gScore[neighbour] = tentative_gScore;
                    fScore[neighbour] = gScore[neighbour] + HeuristicCostEstimate(neighbour, target);
                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }
        // Failure
        return;

	}

    // A really simple heuristic(Manhattan)
    float HeuristicCostEstimate(Node source, Node target)
    {
        return (Mathf.Abs(source.x - target.x) + Mathf.Abs(source.y - target.y));
    }
}
