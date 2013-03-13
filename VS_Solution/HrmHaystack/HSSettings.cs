using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using KSP;
using KSP.IO;

namespace HrmHaystack
{
	public class HSSettings
	{
		public static string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		public static bool minimized;

		public static void Load()
		{
#if DEBUG
			HSUtils.Log("loading settings");
#endif
			PluginConfiguration cfg = PluginConfiguration.CreateForType<HrmHaystack>();
			cfg.load();

			HSBehaviour.WinRect = cfg.GetValue<Rect>("winPos");
			if (HSBehaviour.WinRect == null)
			{
#if DEBUG
				HSUtils.Log("rectangle failed");
#endif
				HSBehaviour.WinRect = new Rect(Screen.width - 320, Screen.height / 2 - 200, 300, 600);
			}
#if DEBUG
			HSUtils.Log(string.Format("rectangle success: {0} {1} {2} {3}", HSBehaviour.WinRect.x, HSBehaviour.WinRect.y, HSBehaviour.WinRect.width, HSBehaviour.WinRect.height));
#endif

			for (ushort iter = 0; iter < HSBehaviour.vesselTypesList.Count(); iter++)
			{
				HSBehaviour.vesselTypesList[iter].visible = cfg.GetValue("type_visible_" + HSBehaviour.vesselTypesList[iter].name, true);
			}
		}

		public static void Save()
		{
#if DEBUG
			HSUtils.Log("saving settings");
#endif
			PluginConfiguration cfg = PluginConfiguration.CreateForType<HrmHaystack>();
			cfg.SetValue("winPos", HSBehaviour.WinRect);

			foreach(HSVesselType type in HSBehaviour.vesselTypesList)
			{
				cfg.SetValue("type_visible_" + type.name, type.visible);
			}

			cfg.save();
		}
		
	}
}
