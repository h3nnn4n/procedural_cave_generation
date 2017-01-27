using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MapGenerator : MonoBehaviour {
	public int width;
	public int height;

	public bool filterSmallRooms;
	public bool filterSmallWalls;
	public int wallThresholdSize = 50;
	public int roomThresholdSize = 50;
	public int borderSize = 0;

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
				} else {
					survivingRooms.Add (new Room (roomRegion, map));
				}
			} else {
				survivingRooms.Add (new Room (roomRegion, map));
			}
		}

		survivingRooms.Sort ();
		survivingRooms [0].isMainRoom = true;
		survivingRooms [0].isAccessibleFromMainRoom = true;



		CoonectClosestRooms (survivingRooms);
	}
		
	void CoonectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false){

		List<Room> roomListA = new List<Room> ();
		List<Room> roomListB = new List<Room> ();

		if (forceAccessibilityFromMainRoom) {
			foreach (Room room in allRooms) {
				if (room.isAccessibleFromMainRoom) {
					roomListB.Add (room);
				} else {
					roomListA.Add (room);
				}
			}
		} else {
			roomListA = allRooms;
			roomListB = allRooms;
		}

		int bestDistance = 1000000;
		Coord bestTileA = new Coord ();
		Coord bestTileB = new Coord ();
		Room bestRoomA = new Room ();
		Room bestRoomB = new Room ();
		bool possibleConnectionFound = false;

		foreach (Room roomA in roomListA) {
			if (!forceAccessibilityFromMainRoom) {
				possibleConnectionFound = false;
				if (roomA.connectedRooms.Count > 0) {
					continue;
				}
			}

			foreach (Room roomB in roomListB) {
				if (roomA == roomB || roomA.IsCoonected(roomB))
					continue;
				
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

			if (possibleConnectionFound && !forceAccessibilityFromMainRoom) {
				CreatePassage (bestRoomA, bestRoomB, bestTileA, bestTileB);
			}
		}

		if (possibleConnectionFound && forceAccessibilityFromMainRoom) {
			CreatePassage (bestRoomA, bestRoomB, bestTileA, bestTileB);
			CoonectClosestRooms (allRooms, true);
		}

		if (!forceAccessibilityFromMainRoom) {
			CoonectClosestRooms (allRooms, true);
		}
	}

	void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB){
		Room.ConnectRooms (roomA, roomB);

//		Debug.DrawLine (CoordToWorldPoint (tileA), CoordToWorldPoint (tileB), Color.green, 100);

		List<Coord> line = getLine (tileA, tileB);
		foreach (Coord c in line) {
			DrawCircle (c, 2);
		}
	}

	void DrawCircle (Coord c, int r){
		for (int x = -r; x <= r; x++) {
			for (int y = -r; y <= r; y++) {
				if (x*x + y*y <= r * r) {
					int realX = c.tileX + x;
					int realY = c.tileY + y;

					if (IsInMapRange (realX, realY)) {
						map [realX, realY] = 0;
					}
				}
			}
		}
	}
		
	List<Coord> getLine(Coord from, Coord to ){
		List<Coord> line = new List<Coord> ();

		int x = from.tileX;
		int y = from.tileY;

		int dx = to.tileX - from.tileX;
		int dy = to.tileY - from.tileY;

		bool inverted = false;
		int step = Math.Sign (dx);
		int gradientStep = Math.Sign (dy);

		int longest = Mathf.Abs (dx);
		int shortest = Mathf.Abs (dy);

		if (longest < shortest) {
			inverted = true;
			longest = Mathf.Abs (dy);
			shortest = Mathf.Abs (dx);

			step = Math.Sign (dy);
			gradientStep = Math.Sign (dx);
		}

		int gradientAccumulation = longest / 2;

		for (int i = 0; i< longest ; i++){
			line.Add(new Coord(x, y));
			if ( inverted ) {
				y += step;
			} else {
				x += step;
			}

			gradientAccumulation += shortest;
			if ( gradientAccumulation >= longest ) {
				if ( inverted ) {
					x += gradientStep;
				} else {
					y += gradientStep;
				}
				gradientAccumulation -= longest;
			}
		}

		return line;
	}

	Vector3 CoordToWorldPoint(Coord tile){
		return new Vector3 (-width / 2 + .5f + tile.tileX, 2, -height / 2 + .5f + tile.tileY - 10);
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

	class Room : IComparable<Room> {
		public List<Coord> tiles;
		public List<Coord> edgeTiles;
		public List<Room> connectedRooms;
		public int roomSize;
		public bool isAccessibleFromMainRoom;
		public bool isMainRoom;

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

		public  void SetAccessibleFromMainRoom(){
			if (!isAccessibleFromMainRoom) {
				isAccessibleFromMainRoom = true;

				foreach (Room coonectedRoom in connectedRooms) {
					coonectedRoom.SetAccessibleFromMainRoom ();
				}
			}
		}

		public static void ConnectRooms(Room roomA, Room roomB){
			if (roomA.isAccessibleFromMainRoom) {
				roomB.SetAccessibleFromMainRoom ();
			} else if (roomB.isAccessibleFromMainRoom) {
				roomA.SetAccessibleFromMainRoom ();
			}

			roomA.connectedRooms.Add (roomB);
			roomB.connectedRooms.Add (roomA);
		}

		public bool IsCoonected(Room otherRoom){
			return connectedRooms.Contains (otherRoom);
		}

		public int CompareTo (Room otherRoom) {
			return otherRoom.roomSize.CompareTo (roomSize);
		}
	}
}
