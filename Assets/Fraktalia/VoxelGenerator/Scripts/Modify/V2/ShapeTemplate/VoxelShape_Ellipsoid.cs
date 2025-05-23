using Fraktalia.Core.Collections;
using Fraktalia.Core.Math;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.VoxelGen.Modify
{

	[System.Serializable]
	public class VoxelShape_Ellipsoid : VoxelShape_Base
	{
		public Vector3 Rotation;


		public int InitialID = 255;
		public Vector3 Bounds = new Vector3(10,10,10);
			
		private Vector3 offset;
		private int innervoxelsize;
		public override void DrawEditorPreview(VoxelModifier_V2 modifier, bool isSafe, Vector3 worldPosition, Vector3 normal)
		{
			Vector3 scaledBounds = Bounds * modifier.ShapeScale;

			if (!isSafe)
			{
				Gizmos.color = Color.red;
			}

			Gizmos.color = new Color32(255, 255, 255, 200);
			Quaternion rot = calculateRotation(Rotation, modifier);

			Vector3 rotated = MathUtilities.RotateBoundary(scaledBounds, rot);
			Gizmos.matrix = Matrix4x4.identity;
			Gizmos.DrawWireCube(worldPosition, rotated);
			Gizmos.matrix = Matrix4x4.TRS(worldPosition, rot, scaledBounds);

			Gizmos.color = new Color32(150, 150, 255, 200);
			Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
			
		}

		protected override void calculateTemplateData(VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			Vector3 scaledBounds = Bounds * modifier.ShapeScale;

			innervoxelsize = target.GetInnerVoxelSize(modifier.Depth);
			float voxelsize = target.GetVoxelSize(modifier.Depth);

			Quaternion rot = calculateRotation(Rotation, modifier);

			Vector3 rotated = MathUtilities.RotateBoundary(scaledBounds, rot);
			int rowsX = (int)((rotated.x / voxelsize)) + boundaryExtension;
			rowsX += rowsX % 2;
			int rowsY = (int)((rotated.y / voxelsize)) + boundaryExtension;
			rowsY += rowsY % 2;
			int rowsZ = (int)((rotated.z / voxelsize)) + boundaryExtension;
			rowsZ += rowsZ % 2;

			offset = new Vector3(rowsX, rowsY, rowsZ) * voxelsize * 0.5f;
			float maddition = 0;
			if (modifier.MarchingCubesOffset)
			{
				maddition = voxelsize * 0.5f;
			}
			Vector3 calculationOffset = (offset - Vector3.one * voxelsize * 0.5f + Vector3.one * maddition) + displacement;

			EllipsoidCalculationJob job = new EllipsoidCalculationJob();
			job.Rotation = Quaternion.Inverse(rot);
			job.BoxSize = scaledBounds / 2;		
			job.Offset = calculationOffset;
			job.rows = new Vector3Int(rowsX, rowsY, rowsZ);
			job.innervoxelsize = innervoxelsize;
			job.voxelsize = voxelsize;
			job.initialID = InitialID;
			job.depth = (byte)modifier.Depth;
			job.template = ModifierTemplateData;

			int totalvoxels = rowsX * rowsY * rowsZ;
			if (ModifierTemplateData.Length != totalvoxels)
			{
				ModifierTemplateData.Resize(totalvoxels, NativeArrayOptions.UninitializedMemory);
			}


			job.Schedule(totalvoxels, totalvoxels).Complete();

		}

		public override void SetGeneratorDirty(VoxelModifier_V2 modifier, VoxelGenerator target, Vector3 worldPosition)
		{
			Vector3 scaledBounds = Bounds * modifier.ShapeScale;

			Vector3 rotated = MathUtilities.RotateBoundary(scaledBounds, Quaternion.Euler(Rotation));
			float voxelsize = target.GetVoxelSize(modifier.Depth);

			target.SetRegionsDirty(target.transform.worldToLocalMatrix.MultiplyPoint3x4(worldPosition), rotated + Vector3.one * voxelsize * boundaryExtension, rotated + Vector3.one * voxelsize * boundaryExtension, modifier.TargetDimension);
		}

		public override Vector3 GetOffset(VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			return -(offset);
		}

		public override int GetVoxelModificationCount(VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			Vector3 scaledBounds = Bounds * modifier.ShapeScale;

			float voxelsize = target.GetVoxelSize(modifier.Depth);
			Vector3 rotated = MathUtilities.RotateBoundary(scaledBounds, Quaternion.Euler(Rotation));

			int rowsX = (int)((rotated.x / voxelsize) + boundaryExtension);
			int rowsY = (int)((rotated.y / voxelsize) + boundaryExtension);
			int rowsZ = (int)((rotated.z / voxelsize) + boundaryExtension);
			return rowsX * rowsY * rowsZ;
		}

		public override bool RequiresRecalculation(VoxelModifier_V2 modifier, VoxelGenerator target)
		{
			Vector3 scaledBounds = Bounds * modifier.ShapeScale;

			float sum = scaledBounds.sqrMagnitude + Rotation.sqrMagnitude + (ApplyObjectRotation ? 0 : 1) + InitialID + boundaryExtension;

			if (!sum.Equals(checksum))
			{
				checksum = sum;
				return true;
			}

			return false;
		}
	}

	[BurstCompile]
	public struct EllipsoidCalculationJob : IJobParallelFor
	{
		public Quaternion Rotation;
		public Vector3 BoxSize;
		public Vector3 Offset;
	
		public Vector3Int rows;
		public int innervoxelsize;
		public float voxelsize;
		public int initialID;
		public byte depth;

		[NativeDisableContainerSafetyRestriction]
		public FNativeList<NativeVoxelModificationData_Inner> template;

		public void Execute(int index)
		{
			NativeVoxelModificationData_Inner result = template[index];
			Vector3Int position = MathUtilities.Convert1DTo3D(index, rows.x, rows.y, rows.z);
			result.X = position.x * innervoxelsize;
			result.Y = position.y * innervoxelsize;
			result.Z = position.z * innervoxelsize;
			result.Depth = depth;
			Vector3 p = new Vector3(position.x * voxelsize, position.y * voxelsize, position.z * voxelsize) - Offset;
			p = Rotation * p;

			float metavalue = EvaluateCellValue(p.x, p.y, p.z);
			int ID = (int)(initialID - initialID * (metavalue));
			result.ID = Mathf.Clamp(ID, 0, 255);

			template[index] = result;
		}

		public float EvaluateCellValue(float Pos_X, float Pos_Y, float Pos_Z)
		{
			float Dist_X = Mathf.Abs(Pos_X);
			float Dist_Y = Mathf.Abs(Pos_Y);
			float Dist_Z = Mathf.Abs(Pos_Z);

			float Factor_X = (Dist_X * Dist_X) / (BoxSize.x * BoxSize.x);
			float Factor_Y = (Dist_Y * Dist_Y) / (BoxSize.y * BoxSize.y);
			float Factor_Z = (Dist_Z * Dist_Z) / (BoxSize.z * BoxSize.z);
	
			return (Factor_X + Factor_Y + Factor_Z);
		}
	}
}
