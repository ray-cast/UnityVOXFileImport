using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;

namespace Cubizer
{
	using VOXMaterial = System.Int32;

	namespace Model
	{
		[Serializable]
		public class VOXHashMapNode<_Tx>
			where _Tx : struct
		{
			public _Tx x;
			public _Tx y;
			public _Tx z;
			public VOXMaterial element;

			public VOXHashMapNode()
			{
				element = int.MaxValue;
			}

			public VOXHashMapNode(_Tx xx, _Tx yy, _Tx zz, VOXMaterial value)
			{
				x = xx;
				y = yy;
				z = zz;
				element = value;
			}

			public bool is_empty()
			{
				return element == int.MaxValue;
			}
		}

		public class VOXHashMapNodeEnumerable<_Tx> : IEnumerable
			where _Tx : struct
		{
			private VOXHashMapNode<_Tx>[] _array;

			public VOXHashMapNodeEnumerable(VOXHashMapNode<_Tx>[] array)
			{
				_array = array;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return (IEnumerator)GetEnumerator();
			}

			public VOXHashMapNodeEnum<_Tx> GetEnumerator()
			{
				return new VOXHashMapNodeEnum<_Tx>(_array);
			}
		}

		public class VOXHashMapNodeEnum<_Tx> : IEnumerator
			where _Tx : struct

		{
			private int position = -1;
			private VOXHashMapNode<_Tx>[] _array;

			public VOXHashMapNodeEnum(VOXHashMapNode<_Tx>[] list)
			{
				_array = list;
			}

			public bool MoveNext()
			{
				var length = _array.Length;
				for (position++; position < length; position++)
				{
					if (_array[position] == null)
						continue;
					if (_array[position].is_empty())
						continue;
					break;
				}

				return position < _array.Length;
			}

			public void Reset()
			{
				position = -1;
			}

			object IEnumerator.Current
			{
				get
				{
					return Current;
				}
			}

			public VOXHashMapNode<_Tx> Current
			{
				get
				{
					return _array[position];
				}
			}
		}

		[Serializable]
		public class VOXHashMap
		{
			protected int _count;
			protected int _allocSize;
			protected Vector3Int _bound;

			protected VOXHashMapNode<System.Byte>[] _data;

			public int Count { get { return _count; } }
			public Vector3Int bound { get { return _bound; } }

			public VOXHashMap(Vector3Int bound)
			{
				_count = 0;
				_bound = bound;
				_allocSize = 0;
			}

			public VOXHashMap(Vector3Int bound, int count)
			{
				_count = 0;
				_bound = bound;
				_allocSize = 0;
				this.Create(count);
			}

			public VOXHashMap(int bound_x, int bound_y, int bound_z, int count)
			{
				_count = 0;
				_bound = new Vector3Int(bound_x, bound_y, bound_z);
				_allocSize = 0;
				this.Create(count);
			}

			public void Create(int count)
			{
				int usage = 1;
				while (usage < count) usage = usage << 1 | 1;

				_count = 0;
				_allocSize = usage;
				_data = new VOXHashMapNode<System.Byte>[usage + 1];
			}

			public bool Set(System.Byte x, System.Byte y, System.Byte z, VOXMaterial value, bool replace = true)
			{
				if (_allocSize == 0)
					this.Create(0xFF);

				var index = HashInt(x, y, z) & _allocSize;
				var entry = _data[index];

				while (entry != null)
				{
					if (entry.x == x && entry.y == y && entry.z == z)
					{
						if (replace)
						{
							_data[index].element = value;
							return true;
						}

						return false;
					}

					index = (index + 1) & _allocSize;
					entry = _data[index];
				}

				if (value != VOXMaterial.MaxValue)
				{
					_data[index] = new VOXHashMapNode<System.Byte>(x, y, z, value);
					_count++;

					if (_count >= _allocSize)
						this.Grow();

					return true;
				}

				return false;
			}

			public bool Get(System.Byte x, System.Byte y, System.Byte z, ref VOXMaterial instanceID)
			{
				if (_allocSize == 0)
					return false;

				var index = HashInt(x, y, z) & _allocSize;
				var entry = _data[index];

				while (entry != null)
				{
					if (entry.x == x && entry.y == y && entry.z == z)
					{
						instanceID = entry.element;
						return instanceID != VOXMaterial.MaxValue;
					}

					index = (index + 1) & _allocSize;
					entry = _data[index];
				}

				instanceID = VOXMaterial.MaxValue;
				return false;
			}

			public bool Exists(System.Byte x, System.Byte y, System.Byte z)
			{
				VOXMaterial instanceID = VOXMaterial.MaxValue;
				return this.Get(x, y, z, ref instanceID);
			}

			public bool Empty()
			{
				return _count == 0;
			}

			public VOXHashMapNodeEnumerable<System.Byte> GetEnumerator()
			{
				if (_data == null)
					throw new System.ApplicationException("GetEnumerator: Empty data");

				return new VOXHashMapNodeEnumerable<System.Byte>(_data);
			}

			public static bool Save(string path, VOXHashMap map)
			{
				UnityEngine.Debug.Assert(map != null);

				var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
				var serializer = new BinaryFormatter();

				serializer.Serialize(stream, map);
				stream.Close();

				return true;
			}

			public static VOXHashMap Load(string path)
			{
				var serializer = new BinaryFormatter();
				var loadFile = new FileStream(path, FileMode.Open, FileAccess.Read);
				return serializer.Deserialize(loadFile) as VOXHashMap;
			}

			private bool Grow(VOXHashMapNode<System.Byte> data)
			{
				var index = HashInt(data.x, data.y, data.z) & _allocSize;
				var entry = _data[index];

				while (entry != null)
				{
					index = (index + 1) & _allocSize;
					entry = _data[index];
				}

				if (data.element != VOXMaterial.MaxValue)
				{
					_data[index] = data;
					_count++;

					return true;
				}

				return false;
			}

			private void Grow()
			{
				var map = new VOXHashMap(_bound, _allocSize << 1 | 1);

				foreach (var it in GetEnumerator())
					map.Grow(it);

				_count = map._count;
				_allocSize = map._allocSize;
				_data = map._data;
			}

			private static int _hash_int(int key)
			{
				key = ~key + (key << 15);
				key = key ^ (key >> 12);
				key = key + (key << 2);
				key = key ^ (key >> 4);
				key = key * 2057;
				key = key ^ (key >> 16);
				return key;
			}

			public static int HashInt(int x, int y, int z)
			{
				return _hash_int(x) ^ _hash_int(y) ^ _hash_int(z);
			}
		}
	}
}