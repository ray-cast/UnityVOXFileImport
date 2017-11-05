using UnityEngine;

namespace Cubizer
{
	namespace Model
	{
		public class VoxelModel
		{
			private static Vector3[,] _positions = new Vector3[6, 4]
			{
				{ new Vector3(-1, -1, -1), new Vector3(-1, -1, +1), new Vector3(-1, +1, -1), new Vector3(-1, +1, +1) },
				{ new Vector3(+1, -1, -1), new Vector3(+1, -1, +1), new Vector3(+1, +1, -1), new Vector3(+1, +1, +1) },
				{ new Vector3(-1, +1, -1), new Vector3(-1, +1, +1), new Vector3(+1, +1, -1), new Vector3(+1, +1, +1) },
				{ new Vector3(-1, -1, -1), new Vector3(-1, -1, +1), new Vector3(+1, -1, -1), new Vector3(+1, -1, +1) },
				{ new Vector3(-1, -1, -1), new Vector3(-1, +1, -1), new Vector3(+1, -1, -1), new Vector3(+1, +1, -1) },
				{ new Vector3(-1, -1, +1), new Vector3(-1, +1, +1), new Vector3(+1, -1, +1), new Vector3(+1, +1, +1) }
			};

			private static Vector3[] _normals = new Vector3[6]
			{
				new Vector3(-1, 0, 0),
				new Vector3(+1, 0, 0),
				new Vector3(0, +1, 0),
				new Vector3(0, -1, 0),
				new Vector3(0, 0, -1),
				new Vector3(0, 0, +1)
			};

			private static Vector2[,] _uvs = new Vector2[6, 4]
			{
				{ new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) },
				{ new Vector2(1, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1) },
				{ new Vector2(0, 1), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0) },
				{ new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) },
				{ new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) },
				{ new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 1) }
			};

			private static int[,] _indices = new int[6, 6]
			{
				{ 0, 3, 2, 0, 1, 3 },
				{ 0, 3, 1, 0, 2, 3 },
				{ 0, 3, 2, 0, 1, 3 },
				{ 0, 3, 1, 0, 2, 3 },
				{ 0, 3, 2, 0, 1, 3 },
				{ 0, 3, 1, 0, 2, 3 }
			};

			public VoxelCruncher[] voxels;

			public VoxelModel(VoxelCruncher[] array)
			{
				voxels = array;
			}

			public static void CreateCubeMesh16x16(ref Vector3[] vertices, ref Vector3[] normals, ref Vector2[] uv, ref int[] triangles, ref int index, VOXVisiableFaces faces, Vector3 translate, Vector3 scale, uint palette)
			{
				bool[] visiable = new bool[] { faces.left, faces.right, faces.top, faces.bottom, faces.front, faces.back };

				float s = 1.0f / 16.0f;
				float a = 0 + 1.0f / 32.0f;
				float b = s - 1.0f / 32.0f;

				for (int i = 0; i < 6; i++)
				{
					if (!visiable[i])
						continue;

					for (int j = 0; j < 6; j++, index++)
					{
						int k = _indices[i, j];

						Vector3 v = _positions[i, k] * 0.5f;
						v.x *= scale.x;
						v.y *= scale.y;
						v.z *= scale.z;
						v += translate;

						float du = (palette % 16) * s;
						float dv = (palette / 16) * s;

						Vector2 coord;
						coord.x = du + (_uvs[i, k].x > 0 ? b : a);
						coord.y = dv + (_uvs[i, k].y > 0 ? b : a);

						vertices[index] = v;
						normals[index] = _normals[i];
						uv[index] = coord;
						triangles[index] = index;
					}
				}
			}

			public static void CreateCubeMesh16x16(VoxelCruncher it, ref Vector3[] vertices, ref Vector3[] normals, ref Vector2[] uv, ref int[] triangles, ref int index)
			{
				Vector3 pos;
				pos.x = (it.begin_x + it.end_x + 1) * 0.5f;
				pos.y = (it.begin_y + it.end_y + 1) * 0.5f;
				pos.z = (it.begin_z + it.end_z + 1) * 0.5f;

				Vector3 scale;
				scale.x = (it.end_x + 1 - it.begin_x);
				scale.y = (it.end_y + 1 - it.begin_y);
				scale.z = (it.end_z + 1 - it.begin_z);

				VoxelModel.CreateCubeMesh16x16(ref vertices, ref normals, ref uv, ref triangles, ref index, it.faces, pos, scale, (uint)it.material);
			}
		}
	}
}