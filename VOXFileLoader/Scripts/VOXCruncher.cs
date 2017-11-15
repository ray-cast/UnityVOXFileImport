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
			public struct Vector3
			{
				public int x;
				public int y;
				public int z;
			}

			public Vector3 begin;
			public Vector3 end;

			public VOXMaterial material;
			public VOXVisiableFaces faces;

			public VOXCruncher(Vector3 begin, Vector3 end, VOXMaterial _material)
			{
				this.begin = begin;
				this.end = end;

				material = _material;

				faces.left = true;
				faces.right = true;
				faces.top = true;
				faces.bottom = true;
				faces.front = true;
				faces.back = true;
			}

			public VOXCruncher(int begin_x, int end_x, int begin_y, int end_y, int begin_z, int end_z, VOXMaterial _material)
			{
				begin.x = begin_x;
				begin.y = begin_y;
				begin.z = begin_z;

				end.x = end_x;
				end.y = end_y;
				end.z = end_z;

				material = _material;

				faces.left = true;
				faces.right = true;
				faces.top = true;
				faces.bottom = true;
				faces.front = true;
				faces.back = true;
			}

			public VOXCruncher(int begin_x, int end_x, int begin_y, int end_y, int begin_z, int end_z, VOXVisiableFaces _faces, VOXMaterial _material)
			{
				begin.x = begin_x;
				begin.y = begin_y;
				begin.z = begin_z;

				end.x = end_x;
				end.y = end_y;
				end.z = end_z;

				material = _material;
				faces = _faces;
			}
		}

		public interface IVOXCruncherStrategy
		{
			VOXModel CalcVoxelCruncher(VoxData chunk, Color32[] palette);
		}

		public class VOXCruncherStupid : IVOXCruncherStrategy
		{
			public VOXModel CalcVoxelCruncher(VoxData chunk, Color32[] palette)
			{
				var crunchers = new VOXCruncher[chunk.count];
				var faces = new VOXVisiableFaces(true, true, true, true, true, true);

				int n = 0;

				for (int i = 0; i < chunk.x; ++i)
				{
					for (int j = 0; j < chunk.y; ++j)
					{
						for (int k = 0; k < chunk.z; ++k)
						{
							var m = chunk.voxels[i, j, k];
							if (m != int.MaxValue)
								crunchers[n++] = new VOXCruncher(i, i, j, j, k, k, faces, m);
						}
					}
				}

				return new VOXModel(crunchers);
			}
		}

		public class VOXCruncherCulled : IVOXCruncherStrategy
		{
			public static bool GetVisiableFaces(VOXMaterial[,,] map, Vector3Int bound, int x, int y, int z, VOXMaterial material, Color32[] palette, out VOXVisiableFaces faces)
			{
				VOXMaterial[] instanceID = new VOXMaterial[6] { VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue, VOXMaterial.MaxValue };

				if (x >= 1) instanceID[0] = map[(byte)(x - 1), y, z];
				if (y >= 1) instanceID[2] = map[x, (byte)(y - 1), z];
				if (z >= 1) instanceID[4] = map[x, y, (byte)(z - 1)];
				if (x <= bound.x) instanceID[1] = map[(byte)(x + 1), y, z];
				if (y <= bound.y) instanceID[3] = map[x, (byte)(y + 1), z];
				if (z <= bound.z) instanceID[5] = map[x, y, (byte)(z + 1)];

				var alpha = palette[material].a;
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

			public VOXModel CalcVoxelCruncher(VoxData chunk, Color32[] palette)
			{
				var crunchers = new List<VOXCruncher>();
				var bound = new Vector3Int(chunk.x, chunk.y, chunk.z);

				for (int i = 0; i < chunk.x; ++i)
				{
					for (int j = 0; j < chunk.y; ++j)
					{
						for (int k = 0; k < chunk.z; ++k)
						{
							var c = chunk.voxels[i, j, k];
							if (c != int.MaxValue)
							{
								VOXVisiableFaces faces;
								if (!GetVisiableFaces(chunk.voxels, bound, i, j, k, c, palette, out faces))
									continue;

								crunchers.Add(new VOXCruncher((byte)i, (byte)i, (byte)j, (byte)j, (byte)k, (byte)k, faces, c));
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

		public class VOXCruncherGreedy : IVOXCruncherStrategy
		{
			public VOXModel CalcVoxelCruncher(VoxData chunk, Color32[] palette)
			{
				var crunchers = new List<VOXCruncher>();
				var dims = new int[] { chunk.x, chunk.y, chunk.z };

				var alloc = System.Math.Max(dims[0], System.Math.Max(dims[1], dims[2]));
				var mask = new int[alloc * alloc];
				var map = chunk.voxels;

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
									mask[n++] = VOXMaterial.MaxValue;
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
								if (c == VOXMaterial.MaxValue)
								{
									++i; ++n;
									continue;
								}

								var w = 1;
								var h = 1;
								var k = 0;

								for (; (i + w) < dims[u] && c == mask[n + w]; ++w) { }

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
										mask[n + k + l * dims[u]] = VOXMaterial.MaxValue;
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
			public static VOXModel CalcVoxelCruncher(VoxData chunk, Color32[] palette, VOXCruncherMode mode)
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