using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace Cubizer
{
	namespace Model
	{
		using VOXMaterial = System.Int32;

		public enum VOXCruncherMode
		{
			Stupid,
			Culled,
			Greedy,
		}

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

		public class VOXCruncher
		{
			public byte begin_x;
			public byte begin_y;
			public byte begin_z;

			public byte end_x;
			public byte end_y;
			public byte end_z;

			public VOXMaterial material;
			public VOXVisiableFaces faces;

			public VOXCruncher(byte begin_x, byte end_x, byte begin_y, byte end_y, byte begin_z, byte end_z, VOXMaterial _material)
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

			public VOXCruncher(byte begin_x, byte end_x, byte begin_y, byte end_y, byte begin_z, byte end_z, VOXVisiableFaces _faces, VOXMaterial _material)
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

		public interface VOXCruncherStrategy
		{
			VOXModel CalcVoxelCruncher(VoxFileChunkChild chunk, Color32[] palette);
		}

		public class VOXCruncherStupid : VOXCruncherStrategy
		{
			public VOXModel CalcVoxelCruncher(VoxFileChunkChild chunk, Color32[] palette)
			{
				var crunchers = new VOXCruncher[chunk.xyzi.voxels.Length / 4];

				for (int i = 0, n = 0; i < chunk.xyzi.voxels.Length; i += 4, n++)
				{
					var x = chunk.xyzi.voxels[i];
					var y = chunk.xyzi.voxels[i + 1];
					var z = chunk.xyzi.voxels[i + 2];
					var c = chunk.xyzi.voxels[i + 3];

					crunchers[n] = new VOXCruncher(x, x, y, y, z, z, c);
				}

				return new VOXModel(crunchers);
			}
		}

		public class VOXCruncherCulled : VOXCruncherStrategy
		{
			public static bool GetVisiableFaces(VOXHashMap map, VOXHashMapNode<System.Byte> it, Color32[] palette, out VOXVisiableFaces faces)
			{
				VOXMaterial[] instanceID = new VOXMaterial[6] { VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue };

				var x = it.x;
				var y = it.y;
				var z = it.z;

				if (x >= 1) map.Get((byte)(x - 1), y, z, ref instanceID[0]);
				if (y >= 1) map.Get(x, (byte)(y - 1), z, ref instanceID[2]);
				if (z >= 1) map.Get(x, y, (byte)(z - 1), ref instanceID[4]);
				if (x <= map.bound.x) map.Get((byte)(x + 1), y, z, ref instanceID[1]);
				if (y <= map.bound.y) map.Get(x, (byte)(y + 1), z, ref instanceID[3]);
				if (z <= map.bound.z) map.Get(x, y, (byte)(z + 1), ref instanceID[5]);

				var alpha = palette[it.element].a;
				if (alpha < 255)
				{
					bool f1 = (instanceID[0] == VOXMaterial.MaxValue) ? true : palette[instanceID[0]].a != alpha ? true : false;
					bool f2 = (instanceID[1] == VOXMaterial.MaxValue) ? true : palette[instanceID[1]].a != alpha ? true : false;
					bool f3 = (instanceID[2] == VOXMaterial.MaxValue) ? true : palette[instanceID[2]].a != alpha ? true : false;
					bool f4 = (instanceID[3] == VOXMaterial.MaxValue) ? true : palette[instanceID[3]].a != alpha ? true : false;
					bool f5 = (instanceID[4] == VOXMaterial.MaxValue) ? true : palette[instanceID[4]].a != alpha ? true : false;
					bool f6 = (instanceID[5] == VOXMaterial.MaxValue) ? true : palette[instanceID[5]].a != alpha ? true : false;

					faces.left = f1;
					faces.right = f2;
					faces.bottom = f3;
					faces.top = f4;
					faces.front = f5;
					faces.back = f6;
				}
				else
				{
					bool f1 = (instanceID[0] == VOXMaterial.MaxValue) ? true : palette[instanceID[0]].a < 255 ? true : false;
					bool f2 = (instanceID[1] == VOXMaterial.MaxValue) ? true : palette[instanceID[1]].a < 255 ? true : false;
					bool f3 = (instanceID[2] == VOXMaterial.MaxValue) ? true : palette[instanceID[2]].a < 255 ? true : false;
					bool f4 = (instanceID[3] == VOXMaterial.MaxValue) ? true : palette[instanceID[3]].a < 255 ? true : false;
					bool f5 = (instanceID[4] == VOXMaterial.MaxValue) ? true : palette[instanceID[4]].a < 255 ? true : false;
					bool f6 = (instanceID[5] == VOXMaterial.MaxValue) ? true : palette[instanceID[5]].a < 255 ? true : false;

					faces.left = f1;
					faces.right = f2;
					faces.bottom = f3;
					faces.top = f4;
					faces.front = f5;
					faces.back = f6;
				}

				return faces.left | faces.right | faces.bottom | faces.top | faces.front | faces.back;
			}

			public VOXModel CalcVoxelCruncher(VoxFileChunkChild chunk, Color32[] palette)
			{
				var map = new VOXHashMap(new Vector3Int(chunk.size.x, chunk.size.y, chunk.size.z), chunk.xyzi.voxels.Length / 4);

				for (int j = 0; j < chunk.xyzi.voxels.Length; j += 4)
				{
					var x = chunk.xyzi.voxels[j];
					var y = chunk.xyzi.voxels[j + 1];
					var z = chunk.xyzi.voxels[j + 2];
					var c = chunk.xyzi.voxels[j + 3];

					map.Set(x, z, y, c);
				}

				var crunchers = new List<VOXCruncher>();
				foreach (var it in map.GetEnumerator())
				{
					VOXVisiableFaces faces;
					if (!GetVisiableFaces(map, it, palette, out faces))
						continue;

					crunchers.Add(new VOXCruncher(it.x, it.x, it.y, it.y, it.z, it.z, faces, it.element));
				}

				var array = new VOXCruncher[crunchers.Count];

				int numbers = 0;
				foreach (var it in crunchers)
					array[numbers++] = it;

				return new VOXModel(array);
			}
		}

		public class VOXCruncherGreedy : VOXCruncherStrategy
		{
			public static int F(VOXHashMap map, int i, int j, int k)
			{
				Debug.Assert(i >= 0 && i < map.bound.x);
				Debug.Assert(j >= 0 && i < map.bound.y);
				Debug.Assert(k >= 0 && i < map.bound.z);

				VOXMaterial material = 0;
				map.Get((byte)i, (byte)j, (byte)k, ref material);
				return material;
			}

			public VOXModel CalcVoxelCruncher(VoxFileChunkChild chunk, Color32[] palette)
			{
				var map = new VOXMaterial[chunk.size.x, chunk.size.z, chunk.size.y];

				for (int i = 0; i < chunk.size.x; ++i)
				{
					for (int j = 0; j < chunk.size.y; ++j)
						for (int k = 0; k < chunk.size.z; ++k)
							map[i, k, j] = VOXMaterial.MaxValue;
				}

				for (int j = 0; j < chunk.xyzi.voxels.Length; j += 4)
				{
					var x = chunk.xyzi.voxels[j];
					var y = chunk.xyzi.voxels[j + 1];
					var z = chunk.xyzi.voxels[j + 2];
					var c = chunk.xyzi.voxels[j + 3];

					map[x, z, y] = c;
				}

				var crunchers = new List<VOXCruncher>();
				var dims = new int[] { chunk.size.x, chunk.size.z, chunk.size.y };

				var alloc = System.Math.Max(dims[0], System.Math.Max(dims[1], dims[2]));
				var mask = new int[alloc * alloc];

				for (var d = 0; d < 3; ++d)
				{
					var u = (d + 1) % 3;
					var v = (d + 2) % 3;

					var x = new int[3] { 0, 0, 0 };
					var q = new int[3] { 0, 0, 0 };

					q[d] = 1;

					var faces = new VOXVisiableFaces(false, false, false, false, false, false);

					for (x[d] = -1; x[d] < dims[d];)
					{
						var n = 0;

						for (x[v] = 0; x[v] < dims[v]; ++x[v])
						{
							for (x[u] = 0; x[u] < dims[u]; ++x[u])
							{
								var a = x[d] >= 0 ? map[x[0], x[1], x[2]] : VOXMaterial.MaxValue;
								var b = x[d] < dims[d] - 1 ? map[x[0] + q[0], x[1] + q[1], x[2] + q[2]] : VOXMaterial.MaxValue;
								if (a != b)
								{
									if (a == VOXMaterial.MaxValue)
										mask[n++] = b;
									else if (b == VOXMaterial.MaxValue)
										mask[n++] = -a;
									else
										mask[n++] = -b;
								}
								else
								{
									mask[n++] = 0;
								}
							}
						}

						++x[d];

						n = 0;

						for (var j = 0; j < dims[v]; ++j)
						{
							for (var i = 0; i < dims[u];)
							{
								var c = mask[n];
								if (c == 0)
								{
									++i; ++n;
									continue;
								}

								var w = 1;
								var h = 1;
								var k = 0;

								for (; c == mask[n + w] && (i + w) < dims[u]; ++w) { }

								var done = false;
								for (; (j + h) < dims[v]; ++h)
								{
									for (k = 0; k < w; ++k)
									{
										if (c != mask[n + k + h * dims[u]])
										{
											done = true;
											break;
										}
									}

									if (done)
										break;
								}

								x[u] = i; x[v] = j;

								var du = new int[3] { 0, 0, 0 };
								var dv = new int[3] { 0, 0, 0 };

								du[u] = w;
								dv[v] = h;

								var v1 = new Vector3(x[0], x[1], x[2]);
								var v2 = new Vector3(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2]);

								v2.x = System.Math.Max(v2.x - 1, 0);
								v2.y = System.Math.Max(v2.y - 1, 0);
								v2.z = System.Math.Max(v2.z - 1, 0);

								if (c > 0)
								{
									faces.front = d == 2;
									faces.back = false;
									faces.left = d == 0;
									faces.right = false;
									faces.top = false;
									faces.bottom = d == 1;
								}
								else
								{
									c = -c;
									faces.front = false;
									faces.back = d == 2;
									faces.left = false;
									faces.right = d == 0;
									faces.top = d == 1;
									faces.bottom = false;
								}

								crunchers.Add(new VOXCruncher((byte)v1.x, (byte)(v2.x), (byte)(v1.y), (byte)(v2.y), (byte)(v1.z), (byte)(v2.z), faces, c));

								for (var l = 0; l < h; ++l)
								{
									for (k = 0; k < w; ++k)
										mask[n + k + l * dims[u]] = 0;
								}

								i += w; n += w;
							}
						}
					}
				}

				var array = new VOXCruncher[crunchers.Count];

				int numbers = 0;
				foreach (var it in crunchers)
					array[numbers++] = it;

				return new VOXModel(array);
			}
		}

		public class VOXPolygonCruncher
		{
			public static VOXModel CalcVoxelCruncher(VoxFileChunkChild chunk, Color32[] palette, VOXCruncherMode mode)
			{
				switch (mode)
				{
					case VOXCruncherMode.Stupid:
						return new VOXCruncherStupid().CalcVoxelCruncher(chunk, palette);

					case VOXCruncherMode.Culled:
						return new VOXCruncherCulled().CalcVoxelCruncher(chunk, palette);

					case VOXCruncherMode.Greedy:
						return new VOXCruncherGreedy().CalcVoxelCruncher(chunk, palette);

					default:
						return null;
				}
			}
		}
	}
}