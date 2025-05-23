using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;
using UnityEngine.Events;
using Fraktalia.Core.FraktaliaAttributes;
using Fraktalia.Core.Collections;
using Fraktalia.Core.Math;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif


namespace Fraktalia.VoxelGen.Visualisation
{
	public enum HideflagsMode
	{
		Normal,
		DontSave,
		Hidden,
		HiddenDontSave
	}

	[ExecuteInEditMode]
	public class BasicNativeVisualHull : MonoBehaviour
	{


		[PropertyKey("General Settings", false)]
		public VoxelGenerator engine;
		public HideflagsMode AppliedHideflags = HideflagsMode.HiddenDontSave;

		[Tooltip("Nothing can modify voxels while it is locked.Hull Generators will not be updated.")]
		public bool Locked = false;
		public bool LockAfterInit = false;
		[Tooltip("Doesn't add a mesh collider to the chunks")]
		public bool NoCollision = false;
		[Tooltip("Collider are now convex which is imprecise but faster")]
		public bool ConvexCollider = false;
		[Tooltip("Doesn't add a renderer to the chunks")]
		public bool Invisible = false;

		[NonSerialized] [HideInInspector]
		public bool NoPostProcess = false;

		private bool isIdle = true;
		public bool IsIdle
		{
			get
			{
				return isIdle;
			}
			set
			{
				if (value != isIdle)
				{
					if (value == true)
					{
						if (engine.DebugMode)
							UnityEngine.Debug.Log("Debug Mode: Hull Completed:" + name);

						if (OnHullGenerationComplete != null)
							OnHullGenerationComplete.Invoke();
					}
					else
					{
						if (engine.DebugMode)
							UnityEngine.Debug.Log("Debug Mode: Hull Started:" + name);

						if (OnHullGenerationStarted != null)
							OnHullGenerationStarted.Invoke();
					}
				}

				isIdle = value;
			}
		}

		
		[Tooltip("If true, hull is closed at the border. Recommended to keep true. Was Shrink in earlier version. ")]
		public bool ClosedHull = true;

		/// <summary>
		/// Parameter which shrinks the boundary so the bounds are always closed. 0.001-1 is used in single setups. 0 is used for multi block setups.
		/// Is now defined by ClosedHull flag as it is easier for the user.
		/// </summary>		
		public int Shrink
		{
			get
			{
				return ClosedHull ? 1 : 0;
			}
		}

		public bool IgnoreDimensions = false;
		public List<int> IgnoreDimensionList = new List<int>();

		[Header("Events:")]
		[Tooltip("Executed when the hull generator has work and starts updating his visual appearance.")]
		public UnityEvent OnHullGenerationStarted;

		[Tooltip("Executed when the hull generator has finished his work and also has no work queued.")]
		public UnityEvent OnHullGenerationComplete;

		[HideInInspector]
		public List<VoxelPiece> VoxelMeshes = new List<VoxelPiece>();

		[NonSerialized]
		public bool IsInitialized = false;
	
		private float CheckSum = -1;
		private int piecesettingsCheckSum;

		protected static VoxelPiece originalVoxelPiece;

		[SerializeField][HideInInspector]
		private int instanceID;

		private void Start()
		{
			engine = GetComponentInParent<VoxelGenerator>();
		}

		private void OnDrawGizmosSelected()
		{
#if UNITY_EDITOR
			if (PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab)
			{
				return;
			}

			if(piecesettingschanged())
			{
				updatepiecesettings();
			}


			OnDuplicateItself();

#endif

			if (engine == null)
			{
				cleanup();
				CleanVisualisation();
			}



		}

		public void InitVisualHull()
		{
			IsInitialized = true;
			isIdle = true;
			Initialize();
			ApplyChecksum();
		}		

		public virtual IEnumerator CalculateHullAsync()
		{	
			yield return null;
		}
		
		public virtual void PrepareWorks()
		{
		}
		
		public virtual void CompleteWorks()
		{
		}

		public virtual void PostProcess()
        {			
		}

		public virtual void RootNodeChanged(NativeVoxelNode node)
		{

		}

		public virtual void NodesChanged(ref UpdateVoxelTree_Job worker)
		{
			
		}

		protected virtual void Initialize()
		{
			
		}
		public virtual void Rebuild()
		{

		}

		public void FinishAllWorks()
		{
			for (int i = 0; i < 1000000; i++)
			{
				NoPostProcess = true;
				if (IsIdle) return;
				PrepareWorks();
				CompleteWorks();
			}
		}

		public void CleanUp()
        {
			IsInitialized = false;
			
			cleanup();
        }

		protected virtual void cleanup()
		{

		}

		public virtual bool IsWorking()
		{
			return false;
		}

		public virtual void CleanVisualisation()
		{
			if (!engine) return;
			if (engine.KeepHulls) return;

			MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
			for (int i = 0; i < filters.Length; i++)
			{
				MeshFilter filter = filters[i];
				if (filter)
				{
					DestroyImmediate(filter.sharedMesh);
				}
				DestroyImmediate(filter.gameObject);
			}
		}

		public virtual void HideMeshes()
		{
			for (int i = 0; i < VoxelMeshes.Count; i++)
			{
				VoxelMeshes[i].gameObject.SetActive(false);			
			}
		}

		public virtual void ShowMeshes()
		{
			for (int i = 0; i < VoxelMeshes.Count; i++)
			{
				VoxelMeshes[i].gameObject.SetActive(true);
			}
		}

		public void ClearMeshes()
		{
			for (int i = 0; i < VoxelMeshes.Count; i++)
			{
				VoxelMeshes[i].meshfilter.sharedMesh.Clear();
			}
		}

		public void ClearVoxelPieces()
		{
			if (!engine) return;
			if (!engine.KeepHulls)
			{
				for (int i = 0; i < VoxelMeshes.Count; i++)
				{
					if (VoxelMeshes[i])
					{
						VoxelMeshes[i].ClearBuffers();

						if (VoxelMeshes[i].meshfilter.sharedMesh)
						{
							DestroyImmediate(VoxelMeshes[i].meshfilter.sharedMesh);
						}
						DestroyImmediate(VoxelMeshes[i].gameObject);						
					}
					
				}

				VoxelMeshes.Clear();
			}
		}

		public virtual void UpdateLOD(int newLOD)
		{

		}
		
		

		/// <summary>
		/// Checksum to evaluate if a public parameter has been changed.
		/// </summary>
		/// <returns></returns>
		protected virtual float GetChecksum()
		{
			return MathUtilities. Bools2Int(Locked, LockAfterInit, NoCollision, ConvexCollider, Invisible);
		}

		protected void ApplyChecksum()
		{
			CheckSum = GetChecksum();
		}


		protected virtual void CreateVoxelPieces(int piececount, Material VoxelMaterial, Material materialShell=null)
		{
			VoxelMeshes = new List<VoxelPiece>(piececount);
			VoxelMeshes.AddRange( GetComponentsInChildren<VoxelPiece>());

			for (int i = VoxelMeshes.Count; i < piececount; i++)
			{
				VoxelMeshes.Add(CreateVoxelPiece("__VOXELPIECE", VoxelMaterial, materialShell));
			}
		}

		protected VoxelPiece CreateVoxelPiece(string Name, Material material, Material materialShell = null)
		{
			VoxelPiece piece = CreateVoxelPieceObject(transform, AppliedHideflags);
#if UNITY_EDITOR
			var flags = GameObjectUtility.GetStaticEditorFlags(engine.gameObject);
			GameObjectUtility.SetStaticEditorFlags(piece.gameObject, flags);
#endif

			if (materialShell)
				piece.meshrenderer.materials = new Material[]{material,materialShell};
			else
				piece.meshrenderer.material = material;
			Mesh visualhull = new Mesh();
			visualhull.MarkDynamic();
			visualhull.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			piece.meshfilter.sharedMesh = visualhull;
			piece.voxelMesh = visualhull;

			if(!NoCollision)
			{
				piece.meshcollider.sharedMesh = visualhull;
				piece.meshcollider.convex = ConvexCollider;
			}
			else
			{
				piece.meshcollider.enabled = false;
			}

			if(Invisible)
			{
				piece.meshrenderer.enabled = false;
			}

			return (piece);
		}

		private void updatepiecesettings()
		{
			VoxelPiece[] pieces = GetComponentsInChildren<VoxelPiece>(true);

			for (int i = 0; i < pieces.Length; i++)
			{
				VoxelPiece piece = pieces[i];
				if (!NoCollision)
				{
					piece.meshcollider.enabled = true;
					piece.meshcollider.convex = ConvexCollider;
				}
				else
				{
					piece.meshcollider.enabled = false;
				}

				switch (AppliedHideflags)
				{
					case HideflagsMode.Normal:
						piece.gameObject.hideFlags = HideFlags.None;
						break;
					case HideflagsMode.DontSave:
						piece.gameObject.hideFlags = HideFlags.DontSave;
						break;
					case HideflagsMode.Hidden:
						piece.gameObject.hideFlags = HideFlags.HideInHierarchy;
						break;
					case HideflagsMode.HiddenDontSave:
						piece.gameObject.hideFlags = HideFlags.HideAndDontSave;
						break;
					default:
						break;
				}	
			}			
		}

		private bool piecesettingschanged()
        {
			int checksum = ((int)AppliedHideflags) + (NoCollision ? 0 : 1) + (ConvexCollider ? 0 : 1);
			if (piecesettingsCheckSum != checksum)
            {
				piecesettingsCheckSum = checksum;
				return true;
            }
			return false; 
        }


		public bool IsValid()
		{
			return (GetChecksum().Equals(CheckSum));
		}

		private void OnDuplicateItself()
		{
#if (UNITY_EDITOR)
			if (!Application.isPlaying)//if in the editor
			{

				//if the instance ID doesnt match then this was copied!
				if (instanceID != gameObject.GetInstanceID())
				{
					UnityEngine.Object orig = EditorUtility.InstanceIDToObject(instanceID);
					if (orig != null)
					{
						OnDuplicate();
					}
					instanceID = gameObject.GetInstanceID();
				}
			}
#endif
		}


		public virtual void OnDuplicate()
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

		private void OnDestroy()
		{
			CleanUp();
		}

        private void OnDisable()
        {
#if UNITY_EDITOR
			if (!Application.isPlaying)
				CleanUp();
#endif
		}

        private void OnEnable()
        {
#if UNITY_EDITOR
			CheckSum = 0;
#endif
		}

		public void SetRegionsDirty(VoxelRegion region)
		{
			if(IgnoreDimensions)
            {
				if (IgnoreDimensionList.Contains(region.DimensionModified)) return;
            }
	
			setRegionsDirty(region);
		}

		protected virtual void setRegionsDirty(VoxelRegion region)
		{

		}

		internal static VoxelPiece CreateVoxelPieceObject(Transform parent, HideflagsMode mode)
        {
			GameObject gamobject = new GameObject("__VOXELPIECE");
			Transform trs = gamobject.transform;
			trs.SetParent(parent);
			trs.transform.localRotation = Quaternion.identity;
			trs.transform.localPosition = Vector3.zero;
			trs.transform.localScale = Vector3.one;
			VoxelPiece piece = gamobject.AddComponent<VoxelPiece>();
			piece.meshfilter = gamobject.AddComponent<MeshFilter>();
			piece.meshcollider = gamobject.AddComponent<MeshCollider>();
			piece.meshrenderer = gamobject.AddComponent<MeshRenderer>();

			gamobject.isStatic = parent.gameObject.isStatic;
			gamobject.layer = parent.gameObject.layer;

			switch (mode)
			{
				case HideflagsMode.Normal:
					gamobject.hideFlags = HideFlags.None;
					break;
				case HideflagsMode.DontSave:
					gamobject.hideFlags = HideFlags.DontSave;
					break;
				case HideflagsMode.Hidden:
					gamobject.hideFlags = HideFlags.HideInHierarchy;
					break;
				case HideflagsMode.HiddenDontSave:
					gamobject.hideFlags = HideFlags.HideAndDontSave;
					break;
				default:
					break;
			}

			return piece;
		}

	}	
}
