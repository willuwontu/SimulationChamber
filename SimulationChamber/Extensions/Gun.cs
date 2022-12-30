using System;
using System.Reflection;
using UnityEngine;

namespace SimulationChamber.Extensions
{
    internal static class GunExtension
    {
        public static Quaternion getShootRotation(this Gun gun, int bulletID, int numOfProj, float charge)
        {
            return (Quaternion)typeof(Gun).InvokeMember("getShootRotation", BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, gun, new object[] { bulletID, numOfProj, charge });
        }

        public static Action AttackAction(this Gun gun)
        {
            return (Action)typeof(Gun).GetField("attackAction", BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField).GetValue(gun);
        }
    }
}
