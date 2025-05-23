using Fraktalia.Utility.NativeNoise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace Fraktalia.VoxelGen.World
{
	public class WorldAlgorithm_PerlinNoise_Planet : WorldAlgorithm
	{
		public enum NoiseMode
		{
			Noise1D,
			Noise2D,
			Noise3D
		}

		

		[Header("Fractal Settings")]
		public int Octaves = 4;
		public float Lacunarity = 2;
		public float Gain = 0.5f;

		[Header("Perlin Noise Settings")]
		public float Frequency;
		public float Amplitude;
		public Vector3 Center = new Vector3(-0.5f,-0.5f,-0.5f);
		public float Radius;
		public float Falloff = 10;
		
		public int StartValue = 255;

		[Tooltip("Prevents overhangs caused by 3D noise. Usually planets have this set to true. If false, result looks like Covid-19")]
		public bool Normalized = false;
		WorldAlgorithm_PerlinNoise_Planet_Calculate2D calculate;
		
		public override void Initialize(VoxelGenerator template)
		{
			int width = template.GetBlockWidth(Depth);
			int blocks = width * width * width;

			
			calculate.Width = width;
			calculate.Blocks = blocks;
			calculate.Depth = (byte)Depth;
		}

		public override JobHandle Apply(Vector3 hash, VoxelGenerator targetGenerator, ref JobHandle handle)
		{
			int width = targetGenerator.GetBlockWidth(Depth);
			int blocks = width * width * width;
			calculate.Width = width;
			calculate.Blocks = blocks;
			calculate.Depth = (byte)Depth;

			calculate.VoxelSize = targetGenerator.GetVoxelSize(Depth);
			calculate.voxeldata = worldGenerator.modificationReservoir.GetDataArray(Depth);
			calculate.RootSize = targetGenerator.RootSize;
			calculate.StartValue = StartValue;
			calculate.Center = hash * targetGenerator.RootSize + Center * scale;
			calculate.Frequency = Frequency / scale;
			calculate.Amplitude = Amplitude * scale;
			calculate.Radius = Radius * scale;
			calculate.Falloff = (Falloff * 40) / scale;
			calculate.ApplyFunction = ApplyFunction;
			calculate.PostProcessFunction = PostProcessFunction;
			calculate.Octaves = Octaves;
			calculate.Lacunarity = Lacunarity;
			calculate.Gain = Gain;
			calculate.PermutationTable = worldGenerator.Permutation;
			calculate.Normalized = Normalized ? 1 : 0;
			return calculate.Schedule(calculate.Blocks, 64, handle);

		}
	}

	
	
	[BurstCompile]
	public struct WorldAlgorithm_PerlinNoise_Planet_Calculate2D : IJobParallelFor
	{
		[ReadOnly]
		public PermutationTable_Native PermutationTable;

		public int Octaves;
		public float Lacunarity;
		public float Gain;

		public float   Frequency;
		public float   Amplitude;

		public int StartValue;

		public byte Depth;
		public float VoxelSize;
		public float RootSize;
		public int Width;
		public int Blocks;
		public Vector3 Center;
		public float Radius;
		
		public NativeArray<NativeVoxelModificationData_Inner> voxeldata;		
		public float Falloff;
		public int Normalized;

		internal ApplyMode ApplyFunction;
		internal PostProcess PostProcessFunction;

		public void Execute(int index)
		{

			int x = index % Width;
			int y = (index - x) / Width % Width;
			int z = ((index - x) / Width - y) / Width;

			float Voxelpos_X = x * VoxelSize;
			float Voxelpos_Y = y * VoxelSize;
			float Voxelpos_Z = z * VoxelSize;
			Voxelpos_X += VoxelSize / 2;
			Voxelpos_Y += VoxelSize / 2;
			Voxelpos_Z += VoxelSize / 2;


			float Worldpos_X = Center.x + Voxelpos_X;
			float Worldpos_Y = Center.y + Voxelpos_Y;
			float Worldpos_Z = Center.z + Voxelpos_Z;

			float distsqr = Worldpos_X * Worldpos_X + Worldpos_Y * Worldpos_Y + Worldpos_Z * Worldpos_Z;
			float dist = Mathf.Sqrt(distsqr);

			
			

			if(Normalized == 1)
			{
				float phi = Mathf.Atan2(Worldpos_Y, Worldpos_X);
				float theta = (float)Mathf.Acos(Worldpos_Z / dist);

				Worldpos_X = Radius * Mathf.Sin(theta) * Mathf.Cos(phi);
				Worldpos_Y = Radius * Mathf.Sin(theta) * Mathf.Sin(phi);
				Worldpos_Z = Radius * Mathf.Cos(theta);


			}


			NativeVoxelModificationData_Inner info = voxeldata[index];
			info.Depth = Depth;
			info.X = VoxelGenerator.ConvertLocalToInner(Voxelpos_X, RootSize);
			info.Y = VoxelGenerator.ConvertLocalToInner(Voxelpos_Y, RootSize);
			info.Z = VoxelGenerator.ConvertLocalToInner(Voxelpos_Z, RootSize);

			float height = Radius;

			float frequency = Frequency;
			float amplitude = Amplitude;

			


			for (int i = 0; i < Octaves; i++)
			{
				height += PerlinNoise_Native.Sample3D(Worldpos_X, Worldpos_Y, Worldpos_Z, frequency, amplitude, ref PermutationTable);
				frequency *= Lacunarity;
				amplitude *= Gain;

			}	

			int value;
			if (dist < height)
			{
				value = StartValue;
			}
			else
			{
				float rest = dist - height;


				value = (int)(StartValue - rest * Falloff);
			}
			value = WorldAlgorithm_PostProcesses.CalculateApplyMode(PostProcessFunction, value);
			value = WorldAlgorithm_Modes.CalculateApplyMode(ApplyFunction, info.ID, value);
			info.ID = Mathf.Clamp(value, 0, 255);

			voxeldata[index] = info;

		}
	}

}
