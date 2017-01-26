using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MapGenerator : MonoBehaviour {
	public int width;
	public int height;

	public bool filterSmallRooms;
	public bool filterSmallWalls;
	public int wallThresholdSize = 100;
	public int roomThresholdSize = 50;

	public string seed;
	public bool useRandomSeed;

	[Range(0,100)]	public int randomFillPercent;
	int[,] map;

	void Start () {
		GenerateMap ();
	}

	void Update() {
		if (Input.GetMouseButtonDown (0)) {
			GenerateMap ();
		}
	}

	void GenerateMap () {
		map = new int[width, height];
		RandomFillMap();

		for (int i = 0; i < 5; i++) {
			SmoothMap ();
		}

		ProcessMap ();

		int borderSize = 5;
		int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

		for (int x = 0; x < borderedMap.GetLength(0); x++) {
			for (int y = 0; y < borderedMap.GetLength(1); y++) {
				if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize) {
					borderedMap [x, y] = map [x - borderSize, y - borderSize];
				} else {
					borderedMap [x, y] = 1;
				}
			}
		}

		MeshGenerator meshGen = GetComponent<MeshGenerator> ();
		meshGen.GenerateMesh (borderedMap, 1);
	}

	void ProcessMap(){
		List<List<Coord>> wallRegions = GetRegions (1);

		if (filterSmallWalls) {
			foreach (List<Coord> wallRegion in wallRegions) {
				if (wallRegion.Count < wallThresholdSize) {
					foreach (Coord tile in wallRegion) {
						map [tile.tileX, tile.tileY] = 0;
					}
				}
			}
		}

		List<Room> survivingRooms = new List<Room> ();

		List<List<Coord>> roomRegions = GetRegions (0);
		foreach (List<Coord> roomRegion in roomRegions) {
			if (roomRegion.Count < roomThresholdSize) {
				if (filterSmallRooms) {
					foreach (Coord tile in roomRegion) {
						map [tile.tileX, tile.tileY] = 1;
					}
				}
			} else {
				survivingRooms.Add (new Room (roomRegion, map));
			}
		}
		CoonectClosestRooms (survivingRooms);
	}

	void CoonectClosestRooms(List<Room> allRooms){
		int bestDistance = 0;
		Coord bestTileA = new Coord ();
		Coord bestTileB = new Coord ();
		Room bestRoomA = new Room ();
		Room bestRoomB = new Room ();
		bool possibleConnectionFound = false;

		foreach (Room roomA in allRooms) {
			possibleConnectionFound = false;

			foreach (Room roomB in allRooms) {
				if (roomA == roomB)
					continue;
				
				if (roomA.IsCoonected (roomB)) {
					possibleConnectionFound = false;
					break;
				}
				
				for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++) {
					for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++) {
						Coord tileA = roomA.edgeTiles [tileIndexA];
						Coord tileB = roomB.edgeTiles [tileIndexB];

						int distanceBetweenRooms = (int) (Mathf.Pow (tileA.tileX - tileB.tileX, 2) + Mathf.Pow (tileA.tileY - tileB.tileY, 2));

						if (distanceBetweenRooms < bestDistance || !possibleConnectionFound) {
							bestDistance = distanceBetweenRooms;
							possibleConnectionFound = true;
							bestTileA = tileA;
							bestTileB = tileB;
							bestRoomA = roomA;
							bestRoomB = roomB;
						}
					}
				}
			}

			if (possibleConnectionFound) {
				CreatePassage (bestRoomA, bestRoomB, bestTileA, bestTileB);
			}
		}
	}

	void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB){
		Room.ConnectRooms (roomA, roomB);

		Debug.DrawRay (CoordToWorldPoint (tileA), CoordToWorldPoint (tileB), Color.green, 100);
	}

	Vector3 CoordToWorldPoint(Coord tile){
		return new Vector3 (-width / 2 + .5f + tile.tileX, 2, -height / 2 + .5f + tile.tileY);
	}

	List<List<Coord>> GetRegions(int tileType){
		List<List<Coord>> regions = new List<List<Coord>> ();
		int[,] mapFlags = new int[width, height];

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if ( mapFlags[x, y] == 0 && map [x, y] == tileType ) {
					List<Coord> newRegion = GetRegionTiles(x, y);
					regions.Add (newRegion);

					foreach (Coord tile in newRegion) {
						mapFlags [tile.tileX, tile.tileY] = 1;
					}
				}
			}
		}

		return regions;
	}

	List<Coord> GetRegionTiles(int startX, int startY){
		List<Coord> tiles = new List<Coord> ();
		int[,] mapFlags = new int[width, height];
		int tileType = map [startX, startY];

		Queue<Coord> queue = new Queue<Coord> ();
		queue.Enqueue (new Coord (startX, startY));
		mapFlags [startX, startY] = 1;

		while (queue.Count > 0) {
			Coord tile = queue.Dequeue ();
			tiles.Add (tile);

			for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
				for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
					if (IsInMapRange(x, y) ) {
						if (mapFlags [x, y] == 0 && map [x, y] == tileType) {
							mapFlags [x, y] = 1;
							queue.Enqueue (new Coord (x, y));
						}
					}
				}
			}
		}

		return tiles;
	}

	bool IsInMapRange(int x, int y) {
		return x >= 0 && x < width && y >= 0 && y < height;
	}

	void RandomFillMap() {
		if ( useRandomSeed ) {
			seed = Time.time.ToString();
		}

		System.Random prng = new System.Random(seed.GetHashCode());

		for ( int x = 0; x < width; x++ ) {
			for ( int y = 0; y < height; y++ ) {
				if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
					map [x, y] = 1;
				else
					map[x, y] = (prng.Next(0, 100) < randomFillPercent) ? 1 : 0;
			}
		}
	}

	void SmoothMap() {
		for ( int x = 0; x < width; x++ ) {
			for ( int y = 0; y < height; y++ ) {
				int neighbourWallsCount = GetSurroundingWallCount (x, y);

				if (neighbourWallsCount > 4) {
					map [x, y] = 1;
				} else if (neighbourWallsCount < 4) {
					map [x, y] = 0;
				}
			}
		}
	}

	int GetSurroundingWallCount ( int gridX, int gridY ) {
		int wallCount = 0;

		for ( int x = gridX-1; x <= gridX+1; x++ ) {
			for ( int y = gridY-1; y <= gridY+1; y++ ) {
				if ( IsInMapRange(x, y) ) {
					if (gridX != x || gridY != y) {
						wallCount += map [x, y];
					}
				} else {
					wallCount++;
				}
			}
		}

		return wallCount;
	}

	struct Coord {
		public int tileX;
		public int tileY;

		public Coord (int x, int y){
			tileX = x;
			tileY = y;
		}
	}

	class Room {
		public List<Coord> tiles;
		public List<Coord> edgeTiles;
		public List<Room> connectedRooms;
		public int roomSize;

		public Room(){
		}

		public Room(List<Coord> roomTiles, int[,] map){
			tiles = roomTiles;
			roomSize = tiles.Count;
			connectedRooms = new List<Room>();
			edgeTiles = new List<Coord>();

			foreach (Coord tile in tiles){
				for(int x = tile.tileX - 1 ; x <= tile.tileX+1; x++){
					for(int y = tile.tileY - 1 ; y <= tile.tileY+1; y++){
						if ( x == tile.tileX || y == tile.tileY ) {
							if (map[x,y] == 1) {
								edgeTiles.Add(tile);
							}
						}
					}
				}
			}
		}

		public static void ConnectRooms(Room roomA, Room roomB){
			roomA.connectedRooms.Add (roomB);
			roomB.connectedRooms.Add (roomA);
		}

		public bool IsCoonected(Room otherRoom){
			return connectedRooms.Contains (otherRoom);
		}
	}
}
