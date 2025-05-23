using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Fraktalia.Core.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.Utility
{
	public struct VertexEntry
	{
		public int TriangleIndex;
		public int VertexIndex;

		public VertexEntry(int triIndex, int vertIndex)
		{

			TriangleIndex = triIndex;
			VertexIndex = vertIndex;
		}
	}

	public unsafe static class ContainerStaticLibrary
	{
		public static event Action OnBeforeCleanUp;
		public static bool AsyncLock;

		private static Dictionary<string, NativeArray<float>> staticArrays_float = new Dictionary<string, NativeArray<float>>();
		private static Dictionary<string, NativeArray<byte>> staticArrays_byte = new Dictionary<string, NativeArray<byte>>();
		private static Dictionary<string, NativeArray<int>> staticArrays_int = new Dictionary<string, NativeArray<int>>();
		private static Dictionary<string, IntPtr> staticArrays_pointer = new Dictionary<string, IntPtr>();
		private static Dictionary<string, NativeArray<GPUVertex>[]> vertexBank = new Dictionary<string, NativeArray<GPUVertex>[]>();
		private static Dictionary<string, NativeArray<Vector3>> vertexArrays = new Dictionary<string, NativeArray<Vector3>>();
		private static Dictionary<string, NativeArray<Vector3>> normalArrays = new Dictionary<string, NativeArray<Vector3>>();
		private static Dictionary<string, Vector3[]> vertexArrays_managed = new Dictionary<string, Vector3[]>();
		private static Dictionary<string, Vector3[]> normalArrays_managed = new Dictionary<string, Vector3[]>();

		private static Dictionary<string, int[]> risingIntArray = new Dictionary<string, int[]>();
		private static Dictionary<string, NativeArray<int>> risingNativeIntArray = new Dictionary<string, NativeArray<int>>();
		private static Dictionary<string, ComputeBuffer> staticComputeBuffers = new Dictionary<string, ComputeBuffer>();

		private static Dictionary<string, NativeArray<Vector3>> permutationTables = new Dictionary<string, NativeArray<Vector3>>();
		private static Dictionary<string, NativeMesh> nativeMeshes = new Dictionary<string, NativeMesh>();

		private static Dictionary<string, FNativeList<Vector3>> FNativeVector3List = new Dictionary<string, FNativeList<Vector3>>();
		private static Dictionary<string, FNativeList<int>> FNativeIntList = new Dictionary<string, FNativeList<int>>();
		private static Dictionary<string, FNativeList<Vector2>> FNativeVector2List = new Dictionary<string, FNativeList<Vector2>>();
		private static Dictionary<string, FNativeList<Vector4>> FNativeVector4List = new Dictionary<string, FNativeList<Vector4>>();
		private static Dictionary<string, FNativeList<Color>> FNativeColorList = new Dictionary<string, FNativeList<Color>>();

		private static Dictionary<string, FNativeMultiHashMap<Vector3, VertexEntry>> FNativeVector3VertexEntryDict = new Dictionary<string, FNativeMultiHashMap<Vector3, VertexEntry>>();

		public static NativeArray<float> GetArray_float(int count, int bank = 0)
		{
			string key = "" + count + "_" + bank;
			if (staticArrays_float.ContainsKey(key))
			{
				return staticArrays_float[key];
			}

			var array = new NativeArray<float>(count, Allocator.Persistent);
			staticArrays_float[key] = array;
			return array;
		}

		public static NativeArray<float> GetArray_float(int count, string identifier)
		{
			string key = "" + count + "_" + identifier;
			if (staticArrays_float.ContainsKey(key))
			{
				return staticArrays_float[key];
			}

			var array = new NativeArray<float>(count, Allocator.Persistent);
			staticArrays_float[key] = array;
			return array;
		}

		public static NativeArray<byte> GetArray_byte(int count, int bank = 0)
		{
			string key = "" + count + "_" + bank;

			if (staticArrays_byte.ContainsKey(key))
			{
				return staticArrays_byte[key];
			}

			var array = new NativeArray<byte>(count, Allocator.Persistent);
			staticArrays_byte[key] = array;
			return array;
		}

		public static NativeArray<int> GetArray_int(int count, int bank = 0)
		{
			string key = "" + count + "_" + bank;

			if (staticArrays_int.ContainsKey(key))
			{
				return staticArrays_int[key];
			}

			var array = new NativeArray<int>(count, Allocator.Persistent);
			staticArrays_int[key] = array;
			return array;
		}

		public static NativeArray<Vector3> GetVertexArray(int count, int bank = 0)
		{
			string key = "" + count + "_" + bank;
			if (vertexArrays.ContainsKey(key))
			{
				return vertexArrays[key];
			}

			var array = new NativeArray<Vector3>(count, Allocator.Persistent);
			vertexArrays[key] = array;
			return array;
		}

		public static Vector3[] GetVertexArray_Managed(int count, int bank = 0)
		{
			string key = "" + count + "_" + bank;
			if (vertexArrays_managed.ContainsKey(key))
			{
				return vertexArrays_managed[key];
			}

			var array = new Vector3[count];
			vertexArrays_managed[key] = array;
			return array;
		}

		public static NativeArray<Vector3> GetNormalArray(int count, int bank = 0)
		{
			string key = "" + count + "_" + bank;
			if (normalArrays.ContainsKey(key))
			{
				return normalArrays[key];
			}

			var array = new NativeArray<Vector3>(count, Allocator.Persistent);
			normalArrays[key] = array;
			return array;
		}

		public static NativeMesh GetNativeMesh(Mesh normalmesh)
		{
			if(normalmesh == null)
            {
				return GetEmptyNativeMesh("NULL", 0);
            }

			string key = "" + normalmesh.name + "_" + normalmesh.vertexCount + "_" + normalmesh.bounds.size;
			if (nativeMeshes.ContainsKey(key))
			{
				return nativeMeshes[key];
			}

			NativeMesh nMesh = NativeMesh.CreateNativeMesh(normalmesh);
			nativeMeshes[key] = nMesh;
			return nMesh;
		}

		public static NativeMesh GetEmptyNativeMesh(string identifier, int index)
		{
			string key = identifier + index;
	
			if (nativeMeshes.ContainsKey(key))
			{
				return nativeMeshes[key];
			}

			NativeMesh nMesh = NativeMesh.CreateEmptyNativeMesh();
			nativeMeshes[key] = nMesh;
			return nMesh;
		}

		public static Vector3[] GetNormalArray_Managed(int count, int bank = 0)
		{
			string key = "" + count + "_" + bank;
			if (normalArrays_managed.ContainsKey(key))
			{
				return normalArrays_managed[key];
			}

			var array = new Vector3[count];
			normalArrays_managed[key] = array;
			return array;
		}

		public static IntPtr GetPointer<T>(string identifier, T type, int size) where T : struct
		{
			string key = identifier + type.ToString() + size;

			if (staticArrays_pointer.ContainsKey(key))
			{
				return staticArrays_pointer[key];
			}

			IntPtr ptr = (IntPtr)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * size, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);

			staticArrays_pointer[key] = ptr;
			return ptr;
		}
	
		public static NativeArray<GPUVertex> GetVertexBank(string identifier, int size,  int index, int banklenght)
		{
			string key = identifier + size + banklenght;

			NativeArray<GPUVertex>[] arrays;
			if (!vertexBank.TryGetValue(key, out arrays))
			{
				arrays = new NativeArray<GPUVertex>[banklenght];
				vertexBank[key] = arrays;
				for (int i = 0; i < banklenght; i++)
				{
					arrays[i] = new NativeArray<GPUVertex>(size, Allocator.Persistent);
				}
			}

			return arrays[index];
		}

		public static NativeArray<Vector3> GetPermutationTable(string seed, int size)
		{
			if (permutationTables.ContainsKey(seed + "" + size))
			{
				return permutationTables[seed + "" + size];
			}

			var array = new NativeArray<Vector3>(size, Allocator.Persistent);			
			var state = UnityEngine.Random.state;
			UnityEngine.Random.InitState(seed.GetHashCode());

			for (int i = 0; i < 10000; i++)
			{
				float value1 = UnityEngine.Random.Range(0f, 1f);
				float value2 = UnityEngine.Random.Range(0f, 1f);
				float value3 = UnityEngine.Random.Range(0f, 1f);

				array[i] = new Vector3(value1, value2, value3);
			}
			UnityEngine.Random.state = state;

			permutationTables[seed + "" + size] = array;
			return array;
		}


		public static ComputeBuffer GetComputeBuffer(string identifier,  int size, int elementSize, ComputeBufferType type = ComputeBufferType.Default)
		{
			string key = identifier + size + "x" + elementSize+ "x" + type;

			ComputeBuffer buffer;
			if (!staticComputeBuffers.TryGetValue(key, out buffer))
			{
				buffer = new ComputeBuffer(size, elementSize, type);
				buffer.IsValid();
				staticComputeBuffers[key] = buffer;	
			}

			return buffer;
		}

		public static int[] GetRisingIntArray(string identifier, int size)
		{
			string key = identifier + size;

			int[] array;
			if (!risingIntArray.TryGetValue(key, out array))
			{
				array = new int[size];
				for (int i = 0; i < size; i++)
				{
					array[i] = i;
				}
				risingIntArray[key] = array;			
			}

			return array;
		}

		public static NativeArray<int> GetRisingNativeIntArray(string identifier, int size)
		{
			string key = identifier + size;

			NativeArray<int> array;
			if (!risingNativeIntArray.TryGetValue(key, out array))
			{
				array = new NativeArray<int>(size, Allocator.Persistent);
				for (int i = 0; i < size; i++)
				{
					array[i] = i;
				}
				risingNativeIntArray[key] = array;
			}

			return array;
		}

		public static FNativeList<Vector4> GetVector4FNativeList(string identifier, int bank)
		{
			string key = identifier + bank;
			FNativeList<Vector4> array;
			if (!FNativeVector4List.TryGetValue(key, out array))
			{
				array = new FNativeList<Vector4>(bank, Allocator.Persistent);
				FNativeVector4List[key] = array;
			}
			return array;
		}
		public static FNativeList<Vector3> GetVector3FNativeList(string identifier, int bank)
		{
			string key = identifier + bank;
			FNativeList<Vector3> array;
			if (!FNativeVector3List.TryGetValue(key, out array))
			{
				array = new FNativeList<Vector3>(bank, Allocator.Persistent);
				FNativeVector3List[key] = array;
			}
			return array;
		}

		public static NativeArray<Vector3> GetVector3FNativeArray(int count, string identifier, int bank)
		{
			string key = "count" + count + "_" + identifier + bank;
			if (vertexArrays.ContainsKey(key))
			{
				return vertexArrays[key];
			}

			var array = new NativeArray<Vector3>(count, Allocator.Persistent);
			vertexArrays[key] = array;
			return array;
		}


		public static FNativeList<Vector2> GetVector2FNativeList(string identifier, int bank)
		{
			string key = identifier + bank;
			FNativeList<Vector2> array;
			if (!FNativeVector2List.TryGetValue(key, out array))
			{
				array = new FNativeList<Vector2>(bank, Allocator.Persistent);
				FNativeVector2List[key] = array;
			}
			return array;
		}

		public static FNativeList<Color> GetColorNativeList(string identifier, int bank)
		{
			string key = identifier + bank;
			FNativeList<Color> array;
			if (!FNativeColorList.TryGetValue(key, out array))
			{
				array = new FNativeList<Color>(bank, Allocator.Persistent);
				FNativeColorList[key] = array;
			}
			return array;
		}


		public static FNativeList<int> GetIntFNativeList(string identifier, int bank)
		{
			string key = identifier + bank;
			FNativeList<int> array;
			if (!FNativeIntList.TryGetValue(key, out array))
			{
				array = new FNativeList<int>(bank, Allocator.Persistent);
				FNativeIntList[key] = array;
			}
			return array;
		}

		public static FNativeMultiHashMap<Vector3, VertexEntry> GetFNativeVector3VertexEntryDict(string identifier, int bank)
		{
			string key = identifier + bank;
			FNativeMultiHashMap<Vector3, VertexEntry> array;
			if (!FNativeVector3VertexEntryDict.TryGetValue(key, out array))
			{
				array = new FNativeMultiHashMap<Vector3, VertexEntry>(bank, Allocator.Persistent);
				FNativeVector3VertexEntryDict[key] = array;
			}
			return array;
		}


		public static void CleanUp()
		{
			OnBeforeCleanUp?.Invoke();

			if (staticArrays_float != null)
			{
				foreach (var item in staticArrays_float)
				{
					item.Value.Dispose();
				}
				staticArrays_float.Clear();
			}

			if (staticArrays_int != null)
			{
				foreach (var item in staticArrays_int)
				{
					item.Value.Dispose();
				}
				staticArrays_int.Clear();
			}

			if (staticArrays_byte != null)
			{
				foreach (var item in staticArrays_byte)
				{
					item.Value.Dispose();
				}
				staticArrays_byte.Clear();
			}

			if (staticArrays_pointer != null)
			{
				foreach (var item in staticArrays_pointer)
				{
					UnsafeUtility.Free(item.Value.ToPointer(), Allocator.Persistent);
				}
				staticArrays_pointer.Clear();
			}

			if(vertexBank != null)
			{
				foreach (var item in vertexBank)
				{
					var arrays = item.Value;
					for (int i = 0; i < arrays.Length; i++)
					{
						arrays[i].Dispose();
					}
				
				}
				vertexBank.Clear();
			}

			risingIntArray.Clear();

			foreach (var item in staticComputeBuffers)
			{
				item.Value.Dispose();
			}
			staticComputeBuffers.Clear();


			if(vertexArrays != null)
			{
				foreach (var item in vertexArrays)
				{
					var arrays = item.Value;
					arrays.Dispose();
				}
				vertexArrays.Clear();
			}

			if (normalArrays != null)
			{
				foreach (var item in normalArrays)
				{
					var arrays = item.Value;
					arrays.Dispose();
				}
				normalArrays.Clear();
			}

			if(permutationTables != null)
            {
				foreach (var item in permutationTables)
				{
					var arrays = item.Value;
					arrays.Dispose();
				}
				permutationTables.Clear();
			}

			if (risingNativeIntArray != null)
			{
				foreach (var item in risingNativeIntArray)
				{
					var arrays = item.Value;
					arrays.Dispose();
				}
				risingNativeIntArray.Clear();
			}

			if (nativeMeshes != null)
			{
				foreach (var item in nativeMeshes)
				{
					var arrays = item.Value;
					arrays.Dispose();
				}
				nativeMeshes.Clear();
			}

			if (FNativeVector4List != null)
			{
				foreach (var item in FNativeVector4List)
				{
					var arrays = item.Value;
					arrays.Dispose();
				}
				FNativeVector4List.Clear();
			}

			if (FNativeVector3List != null)
            {
				foreach (var item in FNativeVector3List)
				{
					var arrays = item.Value;
					arrays.Dispose();
				}
				FNativeVector3List.Clear();
			}

			if (FNativeVector2List != null)
			{
				foreach (var item in FNativeVector2List)
				{
					var arrays = item.Value;
					arrays.Dispose();
				}
				FNativeVector2List.Clear();
			}

			if (FNativeIntList != null)
			{
				foreach (var item in FNativeIntList)
				{
					var arrays = item.Value;
					arrays.Dispose();
				}
				FNativeIntList.Clear();
			}

			if (FNativeColorList != null)
			{
				foreach (var item in FNativeColorList)
				{
					var arrays = item.Value;
					arrays.Dispose();
				}
				FNativeColorList.Clear();
			}

			if (FNativeVector3VertexEntryDict != null)
			{
				foreach (var item in FNativeVector3VertexEntryDict)
				{
					var arrays = item.Value;
					arrays.Dispose();
				}
				FNativeVector3VertexEntryDict.Clear();
			}
		}

	}

   

#if UNITY_EDITOR

    // ensure class initializer is called whenever scripts recompile
    [InitializeOnLoad]
	public static class ContainerStaticLibrary_JobCleaner
	{
		// register an event handler when the class is initialized
		static ContainerStaticLibrary_JobCleaner()
		{
			EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
			EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
			EditorSceneManager.sceneClosing += EditorSceneManager_sceneClosing;
		}

		private static void EditorSceneManager_sceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
		{

			ContainerStaticLibrary.CleanUp();
		}


		private static void EditorSceneManager_sceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
		{
			ContainerStaticLibrary.CleanUp();
		}

		private static void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
		{
			if(obj == PlayModeStateChange.ExitingEditMode || obj == PlayModeStateChange.ExitingPlayMode)
			ContainerStaticLibrary.CleanUp();
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			ContainerStaticLibrary.CleanUp();
		}

		private static void CleanNativeArray(PlayModeStateChange state)
		{
			ContainerStaticLibrary.CleanUp();
		}



	}

#endif
}
