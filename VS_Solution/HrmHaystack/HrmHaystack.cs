using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP;

namespace HrmHaystack
{
	/// <summary>
	/// Class to house vessel types along with icons and sort order for the plugin
	/// Used to be a structure
	/// </summary>
	public class HSVesselType
	{
		public string name; // Type name defined by KSP devs
		public byte sort; // Sort order, lowest first
		public Texture2D icon; // Icon texture, loaded from PluginData directory. File must be named 'button_vessel_TYPE.png'
		public bool visible; // Is this type shown in list

		public HSVesselType(string name, byte sort, Texture2D icon, bool visible)
		{
			this.name = name;
			this.sort = sort;
			this.icon = icon;
			this.visible = visible;
		}
	};

	/// <summary>
	/// Basic Part piece that creates the Classes that run in the background at all times
	/// </summary>
	public class HrmHaystack : PartModule
	{
		public override void OnLoad(ConfigNode node)
		{
#if DEBUG
			HSUtils.Log("Module loaded");
#endif
			//this.enabled = true;
			HSSettings.Load();
		}

		public override void OnSave(ConfigNode node)
		{
			HSSettings.Save();
#if DEBUG
			HSUtils.Log("Module saved");
#endif
		}

		public override void OnAwake()
		{
#if DEBUG
			HSUtils.Log("awake");
#endif
			if (HSBehaviour.gameObjectInstance == null)
			{

				HSBehaviour.gameObjectInstance = GameObject.Find("HSBehaviour") ?? new GameObject("HSBehaviour", typeof(HSBehaviour));
#if DEBUG
				HSUtils.Log("game object created");
#endif
			}
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();
		}
    }

	/// <summary>
	///  This is the behaviour object that we hook events on to
	/// </summary>
	public class HSBehaviour : MonoBehaviour
	{
		// Game object that keeps us running
		public static GameObject gameObjectInstance;

		public static List<HSVesselType> vesselTypesList = new List<HSVesselType>();

		private static Vessel switchToMe = null;
		private static List<Vessel> hsVesselList = null;
		private static List<Vessel> filteredVesselList = null;

		// Resizeable window vars
		private bool winHidden = true;
		private static Rect _winRect;
		
		// Search text
		string filterVar = "";

		public HSBehaviour()
		{

		}

		public void Awake()
		{
#if DEBUG
			HSUtils.Log("awake Behaviour, DLL loaded");
#endif

			// Populate list of vessel types and load textures - should happen once
			HSResources.LoadTextures();

			HSResources.PopulateVesselTypes(ref vesselTypesList);
			vesselTypesList.Sort(new HSUtils.SortByWeight());

			HSSettings.Load();
			
			DontDestroyOnLoad(this);
			CancelInvoke();

			InvokeRepeating("MainHSActivity", 5.0F, 5.0F); // Refresh from time to time just in case
			InvokeRepeating("RefreshDataSaveSettings", 0, 30.0F);
		}

		/// <summary>
		/// Refresh list of vessels
		/// </summary>
		private static void RefetchVesselList()
		{
			hsVesselList = (FlightGlobals.fetch == null ? FlightGlobals.Vessels : FlightGlobals.fetch.vessels);
			// hsVesselList = FlightGlobals.fetch.vessels; // ?
		}

		/// <summary>
		/// Every second refresh seems to be enough. Data filtering here and switching to selected vessel too.
		/// </summary>
		public void MainHSActivity()
		{
			if (IsMapMode)
			{
				filteredVesselList = hsVesselList;

				if (hsVesselList != null)
				{
					if (vesselTypesList != null)
					{
						// For each hidden type remove it from the list
						// FIXME: must be optimized
						foreach (HSVesselType currentInvisibleType in vesselTypesList)
						{
							if (currentInvisibleType.visible == false)
							{
								filteredVesselList = filteredVesselList.FindAll(
									delegate(Vessel sr)
									{
										return sr.vesselType.ToString() != currentInvisibleType.name;
									}
								);
							}
						}
					}
					
					// And then filter by the search string
					if (filterVar != null && filterVar != "")
					{
						//filteredVesselList = hsVesselList.FindAll(delegate(Vessel v) { return -1 != v.vesselName.IndexOf(filterVar, StringComparison.OrdinalIgnoreCase); });
						filteredVesselList = filteredVesselList.FindAll(delegate(Vessel v) { return -1 != v.vesselName.IndexOf(filterVar, StringComparison.OrdinalIgnoreCase); });
					}
				}
			}

			// Detect if there's a request to switch vessel
			if (switchToMe != null)
			{
				FlightGlobals.SetActiveVessel(switchToMe);
				switchToMe = null;
				winHidden = true;
				filterVar = "";
				/*
				if (!switchToMe.loaded)
				{
					switchToMe.Load();
				}

				if (!HighLogic.LoadedSceneIsFlight)
				{
					HighLogic.LoadScene(GameScenes.FLIGHT);
					CameraManager.Instance.SetCameraFlight();
				}
				*/
			}
		}

		/// <summary>
		/// Function called every 30 seconds
		/// </summary>
		public void RefreshDataSaveSettings()
		{
			if (IsMapMode)
			{
				RefetchVesselList();

				// FIXME: temporary for testing
				HSSettings.Save();
			}
		}

		/// <summary>
		/// Repaint GUI (only in map view condition inside)
		/// </summary>
		public void OnGUI()
		{
			if (IsMapMode)
			{
				DrawGUI();
			}
		}

		public static Rect WinRect
		{
			get { return _winRect; }
			set { _winRect = value; }
		}
		public void DrawGUI()
		{
			GUI.skin = HighLogic.Skin;

			if (HSResources.winStyle == null)
			{
				HSResources.LoadStyles();
			}

			_winRect.y = (winHidden) ? Screen.height - 1 : Screen.height - _winRect.height;
			_winRect = GUILayout.Window(1823748, _winRect, MainWindowConstructor, string.Format("Haystack {0}", HSSettings.version), HSResources.winStyle, GUILayout.MinWidth(120), GUILayout.Height(300));
			if (GUI.Button(new Rect(_winRect.x + (_winRect.width / 2 - 24), _winRect.y - 9, 48, 10), "", HSResources.buttonFoldStyle))
			{
				winHidden ^= true; // toggle window state
				RefetchVesselList();
				MainHSActivity();
			}
		}

		private static bool IsMapMode
		{
			get { return FlightGlobals.fetch != null && MapView.MapIsEnabled; }
			//get { return ((FlightGlobals.fetch != null) && (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Map)); }
			//get { return CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Map; }
		}

		// For the scrollview
		private Vector2 scrollPos = Vector2.zero;
		private Vector2 scrollPos2 = Vector2.zero;

		// Voodoo magick to make GUI work properly without click-through
		private Vessel tmpVesselSelected = null;
		private Vessel tmpVesselPreSelected = null;

		private void MainWindowConstructor(int windowID)
		{
			GUILayout.BeginVertical();

			#region vessel types - horizontal
			GUILayout.BeginHorizontal();
			for (int iter = 0; iter < vesselTypesList.Count(); iter++ )
			{
				vesselTypesList.ElementAt(iter).visible = GUILayout.Toggle(vesselTypesList.ElementAt(iter).visible, vesselTypesList[iter].icon, HSResources.buttonVesselTypeStyle);
			}
			GUILayout.EndHorizontal();
			#endregion vessel types

			GUILayout.BeginHorizontal();
			GUILayout.Label("Find:");
			filterVar = GUILayout.TextField(filterVar, GUILayout.MinWidth(50.0F), GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal();

			// If there's anything to display, do it in a loop
			if (filteredVesselList != null && filteredVesselList.Any())
			{
				#region scroller
				scrollPos = GUILayout.BeginScrollView(scrollPos);
				
				GUILayout.BeginVertical();

				bool btnclicked = false;
				
				foreach (Vessel v in filteredVesselList)
				{
					GUILayout.BeginVertical(v == tmpVesselSelected ? HSResources.buttonVesselListPressed : GUI.skin.button);
					GUILayout.Label(v.vesselName, HSResources.textListHeaderStyle);
					GUILayout.Label(string.Format("{0}. {1}{2}", v.vesselType.ToString(), Vessel.GetSituationString(v), (FlightGlobals.ActiveVessel == v && v != null) ? ". Currently active" : ""), HSResources.textSituationStyle);
					GUILayout.EndVertical();

					// First, determine which button was clicked within ScrollView and preselect vessel
					if (Event.current != null && Event.current.type == EventType.Repaint && Input.GetMouseButtonDown(0))
					{
						Rect tmpRect = GUILayoutUtility.GetLastRect();

						if (tmpRect.Contains(Event.current.mousePosition))
						{
							btnclicked = true;
							tmpVesselPreSelected = v;
						}
					}
				}
				GUILayout.EndVertical();

				GUILayout.EndScrollView();
				#endregion scroller

				// Now we can calculate scrollview dimensions. And if click was performed within this area, select temporary vessel
				// Important: GetLastRect works properly only during Repaint event
				if (Event.current != null && Event.current.type == EventType.Repaint && Input.GetMouseButtonDown(0))
				{
					Rect scrollerCoords = GUILayoutUtility.GetLastRect();
					if (btnclicked && scrollerCoords.Contains(Event.current.mousePosition))
					{
						tmpVesselSelected = (tmpVesselSelected == null || tmpVesselSelected != tmpVesselPreSelected) ? tmpVesselPreSelected : null; // set to current or reset
					}
				}
			}
			else
			{
				GUILayout.Label("No matching vessel found");
				tmpVesselSelected = null;
				GUILayout.FlexibleSpace();
			}

			#region bottom buttons - horizontal
			GUILayout.BeginHorizontal();

			GUILayout.FlexibleSpace();

			// Disable buttons for current vessel or nothing selected
			if (tmpVesselSelected == null || FlightGlobals.ActiveVessel == tmpVesselSelected)
			{
				GUI.enabled = false;
			}
			if (GUILayout.Button(HSResources.btnTarg, HSResources.buttonTargStyle))
			{
				//MapView.MapCamera.setTarget(MapView.MapCamera.targets[UnityEngine.Random.Range(0, MapView.MapCamera.targets.Count)].transform);
				/*
				MapView.MapCamera.AddTarget(v.transform);
				MapView.MapCamera.setTarget(v.ReferenceTransform);
				MapView.MapCamera.SetDistance(10000.0F);
				MapView.MapCamera.transform.LookAt(v.GetWorldPos3D());
				*/
				//FlightGlobals.ActiveVessel.orbitTargeter.SetTarget(tmpVesselSelected.orbitDriver); // nope
				FlightGlobals.fetch.SetVesselTarget(tmpVesselSelected);
#if DEBUG
				HSUtils.Log(string.Format("setting target: {0} {1}", tmpVesselSelected.GetInstanceID(), tmpVesselSelected.vesselName));
#endif
				}
				if (GUILayout.Button(HSResources.btnGoHover, HSResources.buttonGoStyle))
				{
#if DEBUG
					HSUtils.Log(string.Format("about to switch to vessel: {0}", tmpVesselSelected.GetInstanceID(), tmpVesselSelected.vesselName));
#endif
					// Delayed switch to vessel
					switchToMe = tmpVesselSelected;
				}

				GUI.enabled = true;

			GUILayout.EndHorizontal();
			#endregion bottom buttons

			GUILayout.EndVertical();

			// If user input detected, force data refresh
			if (GUI.changed)
			{
				MainHSActivity();
			}

			GUI.DragWindow();
		}
	}

}
