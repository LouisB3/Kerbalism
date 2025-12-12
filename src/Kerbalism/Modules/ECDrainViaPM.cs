using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace KERBALISM
{
	public class ECDrainViaPM : PartModule, ISpecifics, IKerbalismModule
	{
		[KSPField(isPersistant = true)] public string title = string.Empty;     // GUI name of the status action in the PAW
		[KSPField(isPersistant = true)] public string targetModule = string.Empty;                     // target module to toggle
		[KSPField(isPersistant = true)] public double ec_rate;                  // EC consumption rate per-second (optional)
		[KSPField(isPersistant = true)] public bool running = false;            // start state
		private PartModule targetPM = null;

		public string Status;

		// pseudo-ctor
		public override void OnStart(StartState state)
		{
			// don't break tutorial scenarios
			if (Lib.DisableScenario(this)) return;

			// update RMB ui
			foreach (PartModule pm in part.Modules)
			{
				if (pm.name.Equals(targetModule))
				{
					targetPM = pm;
				}
			}
			if (string.IsNullOrEmpty(title))
				title = this.targetPM.name;

			Fields["Status"].guiName = title;
		}

		public void Update()
		{
			// update ui
			if (!part.IsPAWVisible())
				return;

			Status = running ? "On" : "Off";
			if (this.running != targetPM.enabled)
			{
				targetPM.enabled = !targetPM.enabled;
			}
		}

		// See IKerbalismModule
		public static string BackgroundUpdate(Vessel v,
			ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot,
			PartModule proto_part_module, Part proto_part,
			Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest,
			double elapsed_s)
		{
			ECDrainViaPM ecdrain = proto_part_module as ECDrainViaPM;
			if (ecdrain == null) return string.Empty;

			if (Lib.Proto.GetBool(module_snapshot, "running") && ecdrain.ec_rate > 0)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -ecdrain.ec_rate));
			}

			return ecdrain.title;
		}

		public virtual string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			// if enabled, and there is ec consumption
			if (running && ec_rate > 0)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -ec_rate));
			}

			return title;
		}

		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			return ResourceUpdate(null, resourceChangeRequest);
		}


		// part tooltip
		public override string GetInfo()
		{
			string desc = title;

			return Specs().Info(desc);
		}

		// specifics support
		public Specifics Specs()
		{
			Specifics specs = new Specifics();
			if (ec_rate > double.Epsilon)
			{
				if (Settings.UseSIUnits)
					specs.Add(Local.Deploy_actualCost, Lib.SIRate(ec_rate, Lib.ECResID));
				else
					specs.Add("EC/s", Lib.HumanReadableRate(ec_rate));
			}
			return specs;
		}
	}

} // KERBALISM

