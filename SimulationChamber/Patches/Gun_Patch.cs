using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimulationChamber.Patches
{
    [HarmonyPatch(typeof(Gun))]
    class Gun_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Attack", typeof(float), typeof(bool), typeof(float), typeof(float), typeof(bool))]
        static bool StopNormalGunAttack(Gun __instance)
        {
            if (__instance is SimulatedGun simulatedGun)
            {
                UnityEngine.Debug.Log("Stopped Normal Gun Attack");
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoAttack", typeof(float), typeof(bool), typeof(float), typeof(float), typeof(bool))]
        static bool StopNormalGunDoAttack(Gun __instance)
        {
            if (__instance is SimulatedGun simulatedGun)
            {
                return false;
            }
            return true;
        }

        //[HarmonyPostfix]
        //[HarmonyPatch("SomeMethod")]
        //static void MyMethodName()
        //{

        //}
    }

    [HarmonyPatch(typeof(ApplyCardStats))]
    class CheckBulletsAfterGettingCards
    {
        [HarmonyPostfix]
        [HarmonyPatch("ApplyStats")]
        private static void SetOnReset(ApplyCardStats __instance)
        {
            var allBullets = Resources.FindObjectsOfTypeAll<ProjectileInit>();
            foreach (var bullet in allBullets)
            {
                var obj = bullet.gameObject;
                if (obj.GetComponent<ProjectileInit>())
                {
                    if (!obj.GetComponent<SimulatedBulletInit>())
                    {
                        obj.AddComponent<SimulatedBulletInit>();
                    }
                }
            }
        }
    }
}