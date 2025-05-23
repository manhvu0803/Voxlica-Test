using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.VoxelGen.World
{
	public class WorldAlgorithm_Gradient : WorldAlgorithm
	{
		public Vector3 Gradient_Min = new Vector3(0,2.25f,0);
		public Vector3 Gradient_Max = new Vector3(0,0,0);
		public Vector3 Gradient_Shift = new Vector3(0,-1.5f,0);
		public float GradientPower = 264f;

		WorldAlgorithm_Gradient_Calculate calculate;
		
		public override void Initialize(VoxelGenerator template)
		{

			int width = template.GetBlockWidth(Depth);
			int blocks = width * width * width;



			calculate.Width = width;
			calculate.Blocks = blocks;
			calculate.Depth = (byte)Depth;
			calculate.VoxelSize = template.GetVoxelSize(Depth);
		}

		public override JobHandle Apply(Vector3 hash, VoxelGenerator targetGenerator, ref JobHandle handle)
		{
			calculate.VoxelSize = targetGenerator.GetVoxelSize(Depth);
			calculate.RootSize = targetGenerator.RootSize;
			calculate.voxeldata = worldGenerator.modificationReservoir.GetDataArray(Depth);
			calculate.PositionOffset = hash * targetGenerator.RootSize;
			calculate.ApplyFunction = ApplyFunction;
			calculate.PostProcessFunction = PostProcessFunction;
			calculate.GradientPower = GradientPower;
			calculate.Gradient_Min = (Gradient_Min + Gradient_Shift) * scale ;
			calculate.Gradient_Max = (Gradient_Max + Gradient_Shift) * scale;
		
			return calculate.Schedule(calculate.Blocks, 64, handle);
	
		}
	}

	
	
	[BurstCompile]
	public struct WorldAlgorithm_Gradient_Calculate : IJobParallelFor
	{
		public Vector3 Gradient_Min;
		public Vector3 Gradient_Max;
		public float GradientPower;


		public byte Depth;
		public float VoxelSize;
		public float RootSize;
		public int Width;
		public int Blocks;
		public Vector3 PositionOffset;
		
		
		public NativeArray<NativeVoxelModificationData_Inner> voxeldata;
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


			float Worldpos_X = PositionOffset.x + Voxelpos_X;
			float Worldpos_Y = PositionOffset.y + Voxelpos_Y;
			float Worldpos_Z = PositionOffset.z + Voxelpos_Z;

			float gradient_X = Mathf.InverseLerp(Gradient_Min.x, Gradient_Max.x, Worldpos_X);
			float gradient_Y = Mathf.InverseLerp(Gradient_Min.y, Gradient_Max.y, Worldpos_Y);
			float gradient_Z = Mathf.InverseLerp(Gradient_Min.z, Gradient_Max.z, Worldpos_Z);

			int value = (int)((gradient_X*0 + gradient_Y + gradient_Z*0) * GradientPower);

			NativeVoxelModificationData_Inner info = voxeldata[index];
			info.Depth = Depth;
			info.X = VoxelGenerator.ConvertLocalToInner(Voxelpos_X, RootSize);
			info.Y = VoxelGenerator.ConvertLocalToInner(Voxelpos_Y, RootSize);
			info.Z = VoxelGenerator.ConvertLocalToInner(Voxelpos_Z, RootSize);

			value = WorldAlgorithm_PostProcesses.CalculateApplyMode(PostProcessFunction, value);
			value = WorldAlgorithm_Modes.CalculateApplyMode(ApplyFunction, info.ID, value);
			info.ID = Mathf.Clamp(value, 0, 255);

			voxeldata[index] = info;

		}
	}
}
