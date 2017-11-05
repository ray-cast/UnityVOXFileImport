using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Linq.Expressions;

using UnityEngine;

namespace Cubizer
{
	namespace Model
	{
		public class ObjFileExport
		{
			public static string MeshToString(MeshFilter mf, Vector3 scale)
			{
				Mesh mesh = mf.sharedMesh;

				Dictionary<int, int> dictionary = new Dictionary<int, int>();

				if (mesh.subMeshCount > 1)
				{
					int[] triangles = mesh.GetTriangles(1);

					for (int j = 0; j < triangles.Length; j += 3)
					{
						if (!dictionary.ContainsKey(triangles[j]))
							dictionary.Add(triangles[j], 1);

						if (!dictionary.ContainsKey(triangles[j + 1]))
							dictionary.Add(triangles[j + 1], 1);

						if (!dictionary.ContainsKey(triangles[j + 2]))
							dictionary.Add(triangles[j + 2], 1);
					}
				}

				StringBuilder stringBuilder = new StringBuilder().Append("mtllib design.mtl").Append("\n").Append("g ").Append(mf.name).Append("\n");

				Vector3[] vertices = mesh.vertices;
				foreach (Vector3 v in mesh.vertices)
				{
					stringBuilder.Append(string.Format("v {0} {1} {2}\n", v.x * scale.x, v.y * scale.y, v.z * scale.z));
				}

				stringBuilder.Append("\n");

				foreach (Vector3 n in mesh.normals)
					stringBuilder.Append(string.Format("vn {0} {1} {2}\n", -n.x, -n.y, n.z));

				for (int num = 0; num != mesh.uv.Length; num++)
				{
					Vector2 uv = mesh.uv[num];

					if (dictionary.ContainsKey(num))
						stringBuilder.Append(string.Format("vt {0} {1}\n", mesh.uv[num].x, mesh.uv[num].y));
					else
						stringBuilder.Append(string.Format("vt {0} {1}\n", uv.x, uv.y));
				}

				for (int k = 0; k < mesh.subMeshCount; k++)
				{
					stringBuilder.Append("\n");

					if (k == 0)
						stringBuilder.Append("usemtl ").Append("Material_design").Append("\n");

					if (k == 1)
						stringBuilder.Append("usemtl ").Append("Material_logo").Append("\n");

					int[] triangles2 = mesh.GetTriangles(k);

					for (int l = 0; l < triangles2.Length; l += 3)
						stringBuilder.Append(string.Format("f {0}/{0} {1}/{1} {2}/{2}\n", triangles2[l] + 1, triangles2[l + 2] + 1, triangles2[l + 1] + 1));
				}

				return stringBuilder.ToString();
			}

			public static void WriteToFile(string path, MeshFilter mf, Vector3 scale)
			{
				using (var sw = new StreamWriter(path))
				{
					sw.Write(MeshToString(mf, new Vector3(-1f, 1f, 1f)));
					sw.Close();
				}
			}
		}
	}
}