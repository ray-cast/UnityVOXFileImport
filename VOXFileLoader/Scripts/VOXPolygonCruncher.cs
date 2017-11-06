using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace Cubizer
{
	namespace Model
	{
		using VOXMaterial = System.Int32;

		public struct VOXVisiableFaces
		{
			public bool left;
			public bool right;
			public bool bottom;
			public bool top;
			public bool back;
			public bool front;

			public VOXVisiableFaces(bool _left, bool _right, bool _bottom, bool _top, bool _back, bool _front)
			{
				left = _left;
				right = _right;
				bottom = _bottom;
				top = _top;
				back = _back;
				front = _front;
			}
		}

		public class VoxelCruncher
		{
			public byte begin_x;
			public byte begin_y;
			public byte begin_z;

			public byte end_x;
			public byte end_y;
			public byte end_z;

			public VOXMaterial material;
			public VOXVisiableFaces faces;

			public VoxelCruncher(byte begin_x, byte end_x, byte begin_y, byte end_y, byte begin_z, byte end_z, VOXMaterial _material)
			{
				this.begin_x = begin_x;
				this.begin_y = begin_y;
				this.begin_z = begin_z;
				this.end_x = end_x;
				this.end_y = end_y;
				this.end_z = end_z;

				material = _material;

				faces.left = true;
				faces.right = true;
				faces.top = true;
				faces.bottom = true;
				faces.front = true;
				faces.back = true;
			}

			public VoxelCruncher(byte begin_x, byte end_x, byte begin_y, byte end_y, byte begin_z, byte end_z, VOXVisiableFaces _faces, VOXMaterial _material)
			{
				this.begin_x = begin_x;
				this.begin_y = begin_y;
				this.begin_z = begin_z;

				this.end_x = end_x;
				this.end_y = end_y;
				this.end_z = end_z;

				material = _material;
				faces = _faces;
			}
		}

		public class VOXPolygonCruncher
		{
			public static VoxelModel CalcVoxelCruncher(VOXHashMap map)
			{
				var listX = new List<VoxelCruncher>[map.bound.z, map.bound.y];

				for (int z = 0; z < map.bound.z; z++)
				{
					for (int y = 0; y < map.bound.y; y++)
					{
						for (int x = 0; x < map.bound.x; x++)
						{
							VOXMaterial entity = VOXMaterial.MaxValue;
							VOXMaterial entityLast = VOXMaterial.MaxValue;

							if (!map.Get((byte)x, (byte)y, (byte)z, ref entity))
								continue;

							if (entity == VOXMaterial.MaxValue)
								continue;

							entityLast = entity;

							int x_end = x;

							for (int xlast = x + 1; xlast < map.bound.x; xlast++)
							{
								if (!map.Get((byte)xlast, (byte)y, (byte)z, ref entity))
									break;

								if (entity != entityLast)
									break;

								x_end = xlast;
								entityLast = entity;
							}

							if (listX[z, y] == null)
								listX[z, y] = new List<VoxelCruncher>();

							listX[z, y].Add(new VoxelCruncher((byte)x, (byte)(x_end), (byte)y, (byte)(y), (byte)z, (byte)(z), entityLast));

							x = x_end;
						}
					}
				}

				for (int z = 0; z < map.bound.z; z++)
				{
					for (int y = 0; y < map.bound.y - 1; y++)
					{
						if (listX[z, y] == null || listX[z, y + 1] == null)
							continue;

						foreach (var cur in listX[z, y])
						{
							for (int h = y + 1; h < map.bound.y; h++)
							{
								if (listX[z, h] == null)
									break;

								foreach (var it in listX[z, h])
								{
									if (it.begin_x == cur.begin_x && it.end_x == cur.end_x)
									{
										if (it.material == cur.material)
										{
											listX[z, h].Remove(it);
											cur.end_y++;
											break;
										}
										else
										{
											h = ushort.MaxValue;
											break;
										}
									}

									if (it.begin_x > cur.begin_x)
									{
										h = ushort.MaxValue;
										break;
									}
								}
							}
						}
					}
				}

				for (int y = 0; y < map.bound.y; y++)
				{
					for (int z = 0; z < map.bound.z - 1; z++)
					{
						if (listX[z, y] == null || listX[z + 1, y] == null)
							continue;

						foreach (var cur in listX[z, y])
						{
							for (int d = z + 1; d < map.bound.z; d++)
							{
								if (listX[d, y] == null)
									break;

								foreach (var it in listX[d, y])
								{
									if (it.begin_x == cur.begin_x && it.end_x == cur.end_x && it.begin_y == cur.begin_y && it.end_y == cur.end_y)
									{
										if (it.material == cur.material)
										{
											listX[d, y].Remove(it);
											cur.end_z++;
											break;
										}
										else
										{
											d = ushort.MaxValue;
											break;
										}
									}

									if (it.begin_x > cur.begin_x || it.end_x > cur.end_x ||
										it.begin_y > cur.begin_y || it.end_y > cur.end_y ||
										it.begin_z > cur.begin_z || it.end_z > cur.end_z)
									{
										d = ushort.MaxValue;
										break;
									}
								}
							}
						}
					}
				}

				int numbers = 0;
				for (byte z = 0; z < map.bound.z; z++)
				{
					for (byte y = 0; y < map.bound.y; y++)
					{
						if (listX[z, y] == null)
							continue;

						foreach (var it in listX[z, y])
							++numbers;
					}
				}

				var array = new VoxelCruncher[numbers];

				numbers = 0;
				for (byte z = 0; z < map.bound.z; z++)
				{
					for (byte y = 0; y < map.bound.y; y++)
					{
						if (listX[z, y] == null)
							continue;

						foreach (var it in listX[z, y])
							array[numbers++] = it;
					}
				}

				foreach (var it in array)
				{
					if (it.begin_x == it.end_x &&
						it.begin_y == it.end_y &&
						it.begin_z == it.end_z)
					{
						var x = it.begin_x;
						var y = it.begin_y;
						var z = it.begin_z;

						VOXMaterial[] instanceID = new VOXMaterial[6] { VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue };

						if (x >= 1) map.Get((byte)(x - 1), y, z, ref instanceID[0]);
						if (y >= 1) map.Get(x, (byte)(y - 1), z, ref instanceID[2]);
						if (z >= 1) map.Get(x, y, (byte)(z - 1), ref instanceID[4]);
						if (x <= (map.bound.x - 1)) map.Get((byte)(x + 1), y, z, ref instanceID[1]);
						if (y <= (map.bound.y - 1)) map.Get(x, (byte)(y + 1), z, ref instanceID[3]);
						if (z <= (map.bound.z - 1)) map.Get(x, y, (byte)(z + 1), ref instanceID[5]);

						bool f1 = (instanceID[0] == VOXMaterial.MaxValue) ? true : false;
						bool f2 = (instanceID[1] == VOXMaterial.MaxValue) ? true : false;
						bool f3 = (instanceID[2] == VOXMaterial.MaxValue) ? true : false;
						bool f4 = (instanceID[3] == VOXMaterial.MaxValue) ? true : false;
						bool f5 = (instanceID[4] == VOXMaterial.MaxValue) ? true : false;
						bool f6 = (instanceID[5] == VOXMaterial.MaxValue) ? true : false;

						it.faces.left = f1;
						it.faces.right = f2;
						it.faces.bottom = f3;
						it.faces.top = f4;
						it.faces.front = f5;
						it.faces.back = f6;
					}
					else if (it.begin_y == it.end_y && it.begin_z == it.end_z)
					{
						VOXMaterial[] instanceID = new int[2] { VOXMaterial.MaxValue, VOXMaterial.MaxValue };

						if (it.begin_x >= 1) map.Get((byte)(it.begin_x - 1), it.begin_y, it.begin_z, ref instanceID[0]);
						if (it.end_x <= (map.bound.x - 1)) map.Get((byte)(it.end_x + 1), it.begin_y, it.begin_z, ref instanceID[1]);

						it.faces.left = (instanceID[0] == VOXMaterial.MaxValue) ? true : false;
						it.faces.right = (instanceID[1] == VOXMaterial.MaxValue) ? true : false;
					}
					else if (it.begin_x == it.end_x && it.begin_z == it.end_z)
					{
						VOXMaterial[] instanceID = new int[2] { VOXMaterial.MaxValue, VOXMaterial.MaxValue };

						if (it.begin_y >= 1) map.Get(it.begin_x, (byte)(it.begin_y - 1), it.begin_z, ref instanceID[0]);
						if (it.end_y <= (map.bound.y - 1)) map.Get(it.begin_x, (byte)(it.end_y + 1), it.begin_z, ref instanceID[1]);

						it.faces.bottom = (instanceID[0] == VOXMaterial.MaxValue) ? true : false;
						it.faces.top = (instanceID[1] == VOXMaterial.MaxValue) ? true : false;
					}
					else if (it.begin_x == it.end_x && it.begin_y == it.end_y)
					{
						VOXMaterial[] instanceID = new int[2] { VOXMaterial.MaxValue, VOXMaterial.MaxValue };

						if (it.begin_z >= 1) map.Get(it.begin_x, it.begin_y, (byte)(it.begin_z - 1), ref instanceID[0]);
						if (it.end_z <= (map.bound.z - 1)) map.Get(it.begin_x, it.begin_y, (byte)(it.end_z + 1), ref instanceID[1]);

						it.faces.front = (instanceID[0] == VOXMaterial.MaxValue) ? true : false;
						it.faces.back = (instanceID[1] == VOXMaterial.MaxValue) ? true : false;
					}
				}

				return new VoxelModel(array);
			}

			public static VoxelModel CalcVoxelCruncher(VoxFileChunkChild chunk)
			{
				var map = new VOXHashMap(new Vector3Int(chunk.size.x, chunk.size.y, chunk.size.z), chunk.xyzi.voxels.Length / 4);

				for (int j = 0; j < chunk.xyzi.voxels.Length; j += 4)
				{
					var x = chunk.xyzi.voxels[j];
					var y = chunk.xyzi.voxels[j + 1];
					var z = chunk.xyzi.voxels[j + 2];
					var c = chunk.xyzi.voxels[j + 3];

					map.Set(x, y, z, c);
				}

				return CalcVoxelCruncher(map);
			}
		}
	}
}