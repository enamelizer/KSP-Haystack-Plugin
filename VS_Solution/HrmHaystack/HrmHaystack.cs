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
	///  This is the behaviour object that we hook events on to
	/// </summary>
	[KSPAddon(KSPAddon.Startup.Flight, true)]
	public class HSBehaviour : MonoBehaviour
	{
		// Game object that keeps us running
		public static GameObject gameObjectInstance;

		public static List<HSVesselType> vesselTypesList = new List<HSVesselType>();

		private static Vessel switchToMe;
		private static List<Vessel> hsVesselList;
		private static List<Vessel> filteredVesselList;

		private static List<CelestialBody> celestialBodyList;
		private static List<CelestialBody> filteredBodyList;
		public static bool showCelestialBodies = true;

		// count types
		private static Dictionary<string, int> typeCount;

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

			celestialBodyList = new List<CelestialBody>();
			typeCount = new Dictionary<string, int>();

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

			// count vessel types
			typeCount.Clear();
			foreach (var vessel in hsVesselList)
			{
				var typeString = vessel.vesselType.ToString();

				if (typeCount.ContainsKey(typeString))
					typeCount[typeString]++;
				else
					typeCount.Add(typeString, 1);
			}
		}

		/// <summary>
		/// Every second refresh seems to be enough. Data filtering here and switching to selected vessel too.
		/// </summary>
		public void MainHSActivity()
		{
			// populate the list of bodies if empty
			if (celestialBodyList.Count < 1)
				celestialBodyList = FlightGlobals.fetch.bodies;

			if (IsFlightScene)
			{
				// refresh filter lists
				filteredVesselList = new List<Vessel>(hsVesselList);
				filteredBodyList = new List<CelestialBody>(celestialBodyList);

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
								//filter out type
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

						if (showCelestialBodies == true)
							filteredBodyList = celestialBodyList.FindAll(delegate(CelestialBody cb) { return -1 != cb.bodyName.IndexOf(filterVar, StringComparison.OrdinalIgnoreCase); });
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
			if (IsFlightScene)
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
			if (IsFlightScene)
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
			get
			{
				return (HighLogic.LoadedScene == GameScenes.FLIGHT) && MapView.MapIsEnabled;
			}
		}

		private static bool IsFlightScene
		{
			get
			{
				return HighLogic.LoadedScene == GameScenes.FLIGHT;
			}
		}

		// For the scrollview
		private Vector2 scrollPos = Vector2.zero;
		private Vector2 scrollPos2 = Vector2.zero;

		// Keep track of selections in GUILayouts
		private Vessel tmpVesselSelected;
		private Vessel tmpVesselPreSelected;
		private CelestialBody tmpBodySelected;
		private CelestialBody tmpBodyPreSelected;
		private string typeSelected;

		private void MainWindowConstructor(int windowID)
		{
			GUILayout.BeginVertical();

			#region vessel types - horizontal
			GUILayout.BeginHorizontal();

			// Vessels
			for (int i = 0; i < vesselTypesList.Count(); i++ )
			{
				var typeString = vesselTypesList[i].name;

				if (typeCount.ContainsKey(typeString))
					typeString += String.Format(" ({0})", typeCount[typeString]);

				vesselTypesList[i].visible = GUILayout.Toggle(vesselTypesList[i].visible, new GUIContent(vesselTypesList[i].icon, typeString), HSResources.buttonVesselTypeStyle);
			}

			// Bodies
			showCelestialBodies = GUILayout.Toggle(showCelestialBodies, new GUIContent(HSResources.btnBodies, "Bodies"), HSResources.buttonVesselTypeStyle);

			GUILayout.EndHorizontal();

			#endregion vessel types

			GUILayout.BeginHorizontal();
			GUILayout.Label("Find:");
			filterVar = GUILayout.TextField(filterVar, GUILayout.MinWidth(50.0F), GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal();

			// handle tooltips here so it paints over the find entry
			if (GUI.tooltip != "")
			{
				// get mouse position
				var mousePosition = Event.current.mousePosition;
				var width = GUI.tooltip.Length * 11;
				GUI.Box(new Rect(mousePosition.x - 30, mousePosition.y - 30, width, 25), GUI.tooltip);
			}

			// If there's anything to display, do it in a loop
			if ((filteredVesselList != null && filteredVesselList.Any()) || showCelestialBodies == true)
			{
				#region scroller
				scrollPos = GUILayout.BeginScrollView(scrollPos);
				
				GUILayout.BeginVertical();

				bool btnclicked = false;
				
				foreach (Vessel vessel in filteredVesselList)
				{
					GUILayout.BeginVertical(vessel == tmpVesselSelected ? HSResources.buttonVesselListPressed : GUI.skin.button);
					GUILayout.Label(vessel.vesselName, HSResources.textListHeaderStyle);
					GUILayout.Label(string.Format("{0}. {1}{2}", vessel.vesselType.ToString(), Vessel.GetSituationString(vessel), (FlightGlobals.ActiveVessel == vessel && vessel != null) ? ". Currently active" : ""), HSResources.textSituationStyle);
					GUILayout.EndVertical();

					// First, determine which button was clicked within ScrollView and preselect vessel
					if (Event.current != null && Event.current.type == EventType.Repaint && Input.GetMouseButtonDown(0))
					{
						Rect tmpRect = GUILayoutUtility.GetLastRect();

						if (tmpRect.Contains(Event.current.mousePosition))
						{
							btnclicked = true;
							tmpVesselPreSelected = vessel;
							tmpBodyPreSelected = null;
							typeSelected = "vessel";		// TODO: this should probably be an enum
						}
					}
				}

				// celestial bodies
				if (showCelestialBodies == true)
				{
					foreach (CelestialBody body in filteredBodyList)
					{
						GUILayout.BeginVertical(body == tmpBodySelected ? HSResources.buttonVesselListPressed : GUI.skin.button);
						GUILayout.Label(body.name, HSResources.textListHeaderStyle);
						GUILayout.EndVertical();

						if (Event.current != null && Event.current.type == EventType.Repaint && Input.GetMouseButtonDown(0))
						{
							Rect tmpRect = GUILayoutUtility.GetLastRect();

							if (tmpRect.Contains(Event.current.mousePosition))
							{
								btnclicked = true;
								tmpBodyPreSelected = body;
								tmpVesselPreSelected = null;
								typeSelected = "body";		// TODO: this should probably be an enum
							}
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
						int instanceID = -1;

						if (typeSelected == "vessel") // handle vessel selected
						{
							if (tmpVesselSelected == null || tmpVesselSelected != tmpVesselPreSelected)
							{
								tmpVesselSelected = tmpVesselPreSelected;
								instanceID = tmpVesselSelected.GetInstanceID();
							}

							tmpBodySelected = null;
						}
						else if (typeSelected == "body") // handle body selected
						{
							if (tmpBodySelected == null || tmpBodySelected != tmpBodyPreSelected)
							{
								tmpBodySelected = tmpBodyPreSelected;
								instanceID = tmpBodySelected.GetInstanceID();
							}

							tmpVesselSelected = null;
						}

						// focus the map object
						if (instanceID != -1)
							FocusMapObject(instanceID);
					}
				}
			}
			else
			{
				GUILayout.Label("No match found");
				tmpVesselSelected = null;
				tmpBodySelected = null;
				GUILayout.FlexibleSpace();
			}

			#region bottom buttons - horizontal
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			// Disable buttons for current vessel or nothing selected
			if (IsTargetButtonDisabled())
			{
				GUI.enabled = false;
			}

			// target button
			if (GUILayout.Button(HSResources.btnTarg, HSResources.buttonTargStyle))
			{
				if (typeSelected == "vessel")
				{
					FlightGlobals.fetch.SetVesselTarget(tmpVesselSelected);
					//HSUtils.Log(string.Format("setting target: {0} {1}", tmpVesselSelected.GetInstanceID(), tmpVesselSelected.vesselName));
				}
				else if (typeSelected == "body")
				{
					FlightGlobals.fetch.SetVesselTarget(tmpBodySelected);
					//HSUtils.Log(string.Format("setting target: {0} {1}", tmpBodySelected.GetInstanceID(), tmpBodySelected.name));
				}
			}

			GUI.enabled = true;

			// Disable fly button if we selected a body, have no selection, or selected the current vessel
			if (IsFlyButtonDisabled())
			{
				GUI.enabled = false;
			}

			// fly button
			if (GUILayout.Button(HSResources.btnGoHover, HSResources.buttonGoStyle))
			{
				if (typeSelected == "vessel")
				{
					//HSUtils.Log(string.Format("about to switch to vessel: {0} {1}", tmpVesselSelected.GetInstanceID(), tmpVesselSelected.vesselName));
					// Delayed switch to vessel
					switchToMe = tmpVesselSelected;
				}
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

		private bool IsTargetButtonDisabled()
		{
			bool returnVal = true;

			if (typeSelected == "vessel")
			{
				returnVal = (tmpVesselSelected == null || FlightGlobals.ActiveVessel == tmpVesselSelected);
				//HSUtils.Log(string.Format("IsTargetButtonDisabled: {0} {1} {2} {3}", returnVal, typeSelected, tmpVesselSelected, FlightGlobals.ActiveVessel));
			}
			else if (typeSelected == "body")
			{
				returnVal = (tmpBodySelected == null || FlightGlobals.currentMainBody == tmpBodySelected);
				//HSUtils.Log(string.Format("IsTargetButtonDisabled: {0} {1} {2} {3}", returnVal, typeSelected, tmpBodySelected, FlightGlobals.currentMainBody));
			}

			return returnVal;
		}

		private bool IsFlyButtonDisabled()
		{
			bool returnVal = true;

			if (typeSelected == "vessel")
			{
				returnVal = (tmpVesselSelected == null || FlightGlobals.ActiveVessel == tmpVesselSelected);
				//HSUtils.Log(string.Format("IsFlyButtonDisabled: {0} {1} {2} {3}", typeSelected, returnVal, tmpVesselSelected, FlightGlobals.ActiveVessel));
			}

			return returnVal;
		}

		/// <summary>
		/// Focuses the map object matching the intanceID passed in
		/// if the scene is map mode.
		/// Searches both vessels and bodies.
		/// Assumes the instance ID is valid.
		/// </summary>
		/// <param name="instanceID"></param>
		private void FocusMapObject(int instanceID)
		{
			// focus on the object
			if (IsMapMode == true)
			{
				foreach (var mapObject in MapView.MapCamera.targets)
				{
					if (mapObject.vessel != null && mapObject.vessel.GetInstanceID() == instanceID)
					{
						MapView.MapCamera.SetTarget(mapObject);
						break;
					}
					else if (mapObject.celestialBody != null && mapObject.celestialBody.GetInstanceID() == instanceID)
					{
						MapView.MapCamera.SetTarget(mapObject);
						break;
					}
				}
			}
		}
	}

}
