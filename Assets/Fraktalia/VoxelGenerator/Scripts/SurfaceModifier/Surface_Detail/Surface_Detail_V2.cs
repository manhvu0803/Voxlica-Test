using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
using Fraktalia.Utility;
using Fraktalia.Core.Collections;
using Fraktalia.Core.Math;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Fraktalia.VoxelGen.Visualisation
{
	public class Surface_Detail_V2 : ModularHullDetail
	{
		public enum GenerationMode
		{
			Crystallic,
			IndividualObject,
			Both
		}

		public NativeArray<Vector3> Permutations;

		[Header("Detail Mesh Settings")]
		public GenerationMode Mode;

		public Transform DetailObject;
		public Mesh CrystalMesh;
		public Material CrystalMaterial;
		
		public float Angle_Min = 0;
		public float Angle_Max = 180;
		public Vector3 Angle_UpwardVector = new Vector3(0,0,1);

		public PlacementManifest CrystalPlacement;
		public float CrystalNormalInfluence = 1;
		public PlacementManifest ObjectPlacement;
		public float ObjectNormalInfluence = 1;

		[Header("Placement Settings")]
		[Range(0, 1)]
		public float CrystalProbability;
		[Range(0, 0.2f)][Tooltip("Object Probability is used for object placement")]
		public float ObjectProbability;
		[Range(0, 20)]
		public int Density;

		[Header("Requirement Settings")]
		public int RequirementDimension;
		public int LifeDimension;


		[Tooltip("Data structure for detail placement. Contains rules about how and when to place props.")]
		public DetailPlacement Placement;

		

		
		private Surface_Detail_Calculation[] m_MeshModJobs;
		private JobHandle[] m_JobHandles;

		private bool isInitialized = false;	
		
		private Stack<Transform>[] ObjectPool;

		

		public NativeMesh nativeMesh;


		protected override void Initialize()
		{			
			isInitialized = true;
			m_MeshModJobs = new Surface_Detail_Calculation[HullGenerator.CurrentNumCores];
			m_JobHandles = new JobHandle[HullGenerator.CurrentNumCores];
		
			Permutations = ContainerStaticLibrary.GetPermutationTable("awdwadwa", 10000);

			nativeMesh = ContainerStaticLibrary.GetNativeMesh(CrystalMesh);

			for (int i = 0; i < m_MeshModJobs.Length; i++)
			{
				m_MeshModJobs[i].Init(i);
				m_MeshModJobs[i].Permutations = Permutations;

				m_MeshModJobs[i].mesh_verticeArray = nativeMesh.mesh_verticeArray;
				m_MeshModJobs[i].mesh_triangleArray = nativeMesh.mesh_triangleArray;
				m_MeshModJobs[i].mesh_uvArray = nativeMesh.mesh_uvArray;
				m_MeshModJobs[i].mesh_uv3Array = nativeMesh.mesh_uv3Array;
				m_MeshModJobs[i].mesh_uv4Array = nativeMesh.mesh_uv4Array;
				m_MeshModJobs[i].mesh_uv5Array = nativeMesh.mesh_uv5Array;
				m_MeshModJobs[i].mesh_uv6Array = nativeMesh.mesh_uv6Array;
				m_MeshModJobs[i].mesh_normalArray = nativeMesh.mesh_normalArray;
				m_MeshModJobs[i].mesh_tangentsArray = nativeMesh.mesh_tangentsArray;
				m_MeshModJobs[i].mesh_colorArray = nativeMesh.mesh_colorArray;

				m_MeshModJobs[i].MODE = (int)Mode;
				
				m_MeshModJobs[i].Placement = Placement;

				
			}

			int piececount = HullGenerator.Cell_Subdivision * HullGenerator.Cell_Subdivision * HullGenerator.Cell_Subdivision;

			CreateVoxelPieces(piececount, CrystalMaterial);

			ObjectPool = new Stack<Transform>[piececount];
			for (int i = 0; i < piececount; i++)
			{
				ObjectPool[i] = new Stack<Transform>();
			}		
		}

		public override void PrepareWorks()
		{
			if (!isInitialized) return;

            for (int cores = 0; cores < HullGenerator.activeCores; cores++)
            {
				int cellIndex = HullGenerator.activeCells[cores];
				if (HullGenerator.WorkInformations[cellIndex].CurrentWorktype != ModularUniformVisualHull.WorkType.PostProcessesOnly) continue;
				if (Disabled) continue;

				NativeMeshData data = HullGenerator.nativeMeshData[cellIndex];

				if (HullGenerator.DebugMode)
					Debug.Log("Adding Details/Post Surface Process");


				m_MeshModJobs[cores].surface_verticeArray = data.verticeArray;
				m_MeshModJobs[cores].surface_normalArray = data.normalArray;
				m_MeshModJobs[cores].surface_triangleArray = data.triangleArray_original;

				int Cell_Subdivision = HullGenerator.Cell_Subdivision;
				int Width = HullGenerator.width;
		
				float cellSize = HullGenerator.engine.RootSize / Cell_Subdivision;
				

				m_MeshModJobs[cores].slotIndex = cellIndex;
				m_MeshModJobs[cores].voxelSize = cellSize / (Width);
				m_MeshModJobs[cores].halfSize = (cellSize / (Width)) / 2;
				m_MeshModJobs[cores].cellSize = cellSize;
				m_MeshModJobs[cores].positionoffset = data.positionOffset[0];		
				m_MeshModJobs[cores].Angle_Min = Angle_Min;
				m_MeshModJobs[cores].Angle_Max = Angle_Max;
				m_MeshModJobs[cores].Angle_UpwardVector = Angle_UpwardVector;
				m_MeshModJobs[cores].CrystalManifest = CrystalPlacement;
				m_MeshModJobs[cores].CrystalNormalInfluence = CrystalNormalInfluence;
				m_MeshModJobs[cores].ObjectManifest = ObjectPlacement;
				m_MeshModJobs[cores].ObjectNormalInfluence = ObjectNormalInfluence;
				m_MeshModJobs[cores].CrystalProbability = CrystalProbability;
				m_MeshModJobs[cores].ObjectProbability = ObjectProbability;
				m_MeshModJobs[cores].Density = Density;
				m_MeshModJobs[cores].Placement = Placement;

				if (RequirementDimension >= 0 && RequirementDimension < HullGenerator.engine.Data.Length)
				{
					m_MeshModJobs[cores].requirementData = HullGenerator.engine.Data[RequirementDimension];
					m_MeshModJobs[cores].requirementvalid = 1;
				}
				else m_MeshModJobs[cores].requirementvalid = 0;

				if (LifeDimension >= 0 && LifeDimension < HullGenerator.engine.Data.Length)
				{
					m_MeshModJobs[cores].lifeData = HullGenerator.engine.Data[LifeDimension];
					m_MeshModJobs[cores].lifevalid = 1;
				}
				else m_MeshModJobs[cores].lifevalid = 0;

                if (!m_JobHandles[cores].IsCompleted)
                {
					m_JobHandles[cores].Complete();

				}
				
				m_JobHandles[cores] = m_MeshModJobs[cores].Schedule();		
			}
		}

		public override void CompleteWorks()
		{
			if (!isInitialized) return;

			for (int cores = 0; cores < HullGenerator.activeCores; cores++)
			{
				int cellIndex = HullGenerator.activeCells[cores];
				m_JobHandles[cores].Complete();

				if (HullGenerator.WorkInformations[cellIndex].CurrentWorktype != ModularUniformVisualHull.WorkType.PostProcessesOnly)
				{					
					continue;
				}
			

				int index = cellIndex;

				

				VoxelPiece piece = VoxelMeshes[index];
				piece.Clear();

				
				Surface_Detail_Calculation usedcalculator = m_MeshModJobs[cores];
				if (usedcalculator.verticeArray.Length != 0 && !Disabled)
				{
					piece.SetVertices(usedcalculator.verticeArray);
					piece.SetTriangles(usedcalculator.triangleArray);
					piece.SetNormals(usedcalculator.normalArray);
					piece.SetTangents(usedcalculator.tangentsArray);
					piece.SetUVs(0, usedcalculator.uvArray);
					piece.SetUVs(2, usedcalculator.uv3Array);
					piece.SetUVs(3, usedcalculator.uv4Array);
					piece.SetUVs(4, usedcalculator.uv5Array);
					piece.SetUVs(5, usedcalculator.uv6Array);
					piece.SetColors(usedcalculator.colorArray);
				}

				piece.voxelMesh.RecalculateBounds();
				piece.EnableCollision(!NoCollision);

				if (DetailObject != null)
				{
					if (usedcalculator.objectArray.Length != 0 && !Disabled)
					{
						Transform piececontainter = piece.transform;
						int childcount = piececontainter.childCount;

						for (int i = 0; i < childcount; i++)
						{
							Transform detailObject = piececontainter.GetChild(i);
							ObjectPool[index].Push(detailObject);
						}


						for (int i = 0; i < usedcalculator.objectArray.Length; i++)
						{
							Transform detailObject;
							if (ObjectPool[index].Count > 0)
							{
								detailObject = ObjectPool[index].Pop();
								detailObject.gameObject.SetActive(true);
							}
							else
							{
								detailObject = Instantiate(DetailObject, piececontainter);
							}

							Matrix4x4 matrix = usedcalculator.objectArray[i];


							detailObject.localPosition = matrix.GetColumn(3);
							detailObject.localRotation = matrix.rotation;
							detailObject.localScale = matrix.lossyScale;
						}

						while (ObjectPool[index].Count > 0)
						{
							Transform detailObject = ObjectPool[index].Pop();
							detailObject.gameObject.SetActive(false);
						}
					}
					else
					{
						Transform piececontainter = piece.transform;
						int childcount = piececontainter.childCount;

						for (int i = 0; i < childcount; i++)
						{
							piececontainter.GetChild(i).gameObject.SetActive(false);

						}
					}
				}
			}
		}

		public override bool IsSave()
		{
			if (Mode != GenerationMode.IndividualObject)
			{
				if (CrystalMesh == null)
				{
					ErrorMessage = "No DetailMesh assigned!";
					return false;
				}
				if (CrystalMesh.vertexCount > 1000)
				{
					ErrorMessage = "Vertex Count of detail mesh is greater than 1000. Use individual objects for high poly details.";
					return false;
				}
			}

			if (Mode != GenerationMode.Crystallic)
			{
				if (DetailObject == null)
				{
					ErrorMessage = "No DetailObject assigned!";
					return false;
				}
			}

			return true;
		}

        public override bool IsCompleted()
        {
            for (int i = 0; i < m_JobHandles.Length; i++)
            {
				if (!m_JobHandles[i].IsCompleted) return false;
            }

			return true;
        }


        public override void CleanUp()
		{
			DestroyMeshes();
				
			if (m_MeshModJobs != null)
			{
				for (int i = 0; i < m_MeshModJobs.Length; i++)
				{
					m_JobHandles[i].Complete();					
				}
			}

			m_MeshModJobs = new Surface_Detail_Calculation[0];		
			
			isInitialized = false;
		}

		


		

        internal override ModularUniformVisualHull.WorkType EvaluateWorkType(int dimension)
        {
			if(dimension == RequirementDimension || dimension == LifeDimension)
            {
				return ModularUniformVisualHull.WorkType.PostProcessesOnly;
            }

			return ModularUniformVisualHull.WorkType.Nothing;
        }

        internal override void GetFractionalGeoChecksum(ref ModularUniformVisualHull.FractionalChecksum fractional)
        {
			float sum = 0;
			sum += CrystalPlacement._GetChecksum();
			sum += ObjectPlacement._GetChecksum();		
			sum += (CrystalProbability * 100 + ObjectProbability * 100);
			sum += Density;
			sum += Placement.GetChecksum();
			sum += CrystalNormalInfluence + ObjectNormalInfluence;
			sum += Angle_Min + Angle_Max + Angle_UpwardVector.sqrMagnitude;
			sum += LifeDimension + RequirementDimension;
			sum += MathUtilities.Bools2Int(Disabled, ConvexCollider, NoCollision);
			fractional.postprocessChecksum += sum;
        }


        internal override void OnDuplicate()
		{			
			VoxelPiece[] Pieces = GetComponentsInChildren<VoxelPiece>();
			for (int i = 0; i < Pieces.Length; i++)
			{
				if (Pieces[i].meshfilter.sharedMesh != null)
				{
					Pieces[i].meshfilter.sharedMesh = Instantiate(Pieces[i].meshfilter.sharedMesh);
					Pieces[i].voxelMesh = Pieces[i].meshfilter.sharedMesh;
				}

			}
			VoxelMeshes = new List<VoxelPiece>();
			VoxelMeshes.AddRange(Pieces);
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(Surface_Detail_V2))]
	[CanEditMultipleObjects]
	public class Surface_Detail_V2Editor : Editor
	{



		public override void OnInspectorGUI()
		{
			GUIStyle title = new GUIStyle();
			title.fontStyle = FontStyle.Bold;
			title.fontSize = 14;
			title.richText = true;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 12;
			bold.richText = true;


			EditorStyles.textField.wordWrap = true;



			Surface_Detail_V2 myTarget = (Surface_Detail_V2)target;
			EditorGUILayout.Space();

			DrawDefaultInspector();

			if (!myTarget.IsSave())
			{
				EditorGUILayout.LabelField("<color=red>Errors:</color>", bold);
				EditorGUILayout.TextArea(myTarget.ErrorMessage);
			}


			if (GUI.changed)
			{
				EditorUtility.SetDirty(target);
			}



		}
	}
#endif

}

