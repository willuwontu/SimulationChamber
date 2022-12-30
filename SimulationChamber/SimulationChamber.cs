using BepInEx;
using HarmonyLib;
using UnityEngine;
using SimulationChamber.Patches;
using System.Collections.Generic;
using System;
using Photon.Pun;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Linq;
using UnboundLib.GameModes;

namespace SimulationChamber
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.rounds.willuwontu.gunchargepatch", BepInDependency.DependencyFlags.SoftDependency)]
    // Declares our mod to Bepin
    [BepInPlugin(ModId, ModName, Version)]
    // The game our mod is associated with
    [BepInProcess("Rounds.exe")]
    internal class SimulationChamber : BaseUnityPlugin
    {
        private const string ModId = "com.willuwontu.rounds.Id";
        private const string ModName = "SimulationChamber";
        public const string Version = "0.0.0"; // What version are we on (major.minor.patch)?

        public static SimulationChamber instance { get; private set; }

        void Awake()
        {
            instance = this;

            // Use this to call any harmony patch files your mod may have
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
            //harmony.Patch(MonoBehaviour_Patch.TargetMethod(), postfix: new HarmonyMethod(typeof(MonoBehaviour_Patch), nameof(MonoBehaviour_Patch.DoTStopped)));

            var _ = SimulatedWeaponObject;
        }
        void Start()
        {
            foreach (GameObject obj in Resources.LoadAll<GameObject>(""))
            {
                if (obj.GetComponent<ProjectileInit>())
                {
                    if (!obj.GetComponent<SimulatedBulletInit>())
                    {
                        obj.AddComponent<SimulatedBulletInit>();
                    }
                }
            }

            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStart);
        }

        private IEnumerator OnGameStart(IGameModeHandler gm)
        {
            foreach (var simWeapon in simulationWeapons.Values)
            {
                if (simWeapon)
                {
                    try
                    {
                        PhotonNetwork.Destroy(simWeapon.gameObject);
                    }
                    catch
                    {

                    }
                }
                simulationWeapons = new Dictionary<string, SimulatedGun>();
            }

            yield break;
        }

        internal Dictionary<string, SimulatedGun> simulationWeapons = new Dictionary<string, SimulatedGun>();

        private GameObject simulatedWeaponObject = null;

        internal GameObject SimulatedWeaponObject
        {
            get
            {
                if (simulatedWeaponObject == null)
                {
                    simulatedWeaponObject = new GameObject("A_SimulatedWeapon");
                    DontDestroyOnLoad(simulatedWeaponObject);
                    simulatedWeaponObject.SetActive(false);

                    var view = simulatedWeaponObject.AddComponent<PhotonView>();
                    view.Synchronization = ViewSynchronization.UnreliableOnChange;
                    view.OwnershipTransfer = OwnershipOption.Takeover;
                    view.observableSearch = PhotonView.ObservableSearch.AutoFindAll;

                    var simGun = simulatedWeaponObject.AddComponent<SimulatedGun>();
                    var defaultGun = Resources.Load<GameObject>("Player").GetComponent<Holding>().holdable.GetComponent<Gun>();
                    simGun.CopyGunStatsExceptActions(defaultGun);
                    //simGun.CopyAttackAction(defaultGun);
                    //simGun.ShootPojectileAction = (Action<GameObject>)defaultGun.ShootPojectileAction.Clone();

                    PhotonNetwork.PrefabPool.RegisterPrefab(simulatedWeaponObject.name, SimulatedWeaponObject);
                }

                return simulatedWeaponObject;
            }
        }

        public SimulatedGun SpawnSimulationGun()
        {
            string id = (Guid.NewGuid()).ToString();
            var gunObj = PhotonNetwork.Instantiate(SimulatedWeaponObject.name, Vector3.zero, Quaternion.identity, 0, new object[] { id });
            gunObj.SetActive(true);
            DontDestroyOnLoad(gunObj);
            simulationWeapons[id] = gunObj.GetComponent<SimulatedGun>();

            return gunObj.GetComponent<SimulatedGun>();
        }

        public SimulatedGun GetSimulationWeapon(string weaponID)
        {
            if (simulationWeapons.TryGetValue(weaponID, out SimulatedGun gun))
            {
                return gun;
            }

            return null;
        }
    }

    public static class SimulationController
    {
        public static ReadOnlyCollection<SimulatedGun> AvailableWeapons => new ReadOnlyCollection<SimulatedGun>(SimulationChamber.instance.simulationWeapons.Values.ToList());

        public static SimulatedGun CreateNewSimulationWeapon()
        {
            return SimulationChamber.instance.SpawnSimulationGun();
        }
    }

    internal class TestSimulation : MonoBehaviour
    {
        Player player;
        Gun gun;

        public List<SimulatedGun> savedGuns = new List<SimulatedGun>();

        public void Start()
        {
            this.player = this.GetComponentInParent<Player>();

            this.gun = this.player.data.weaponHandler.gun;
            this.gun.ShootPojectileAction += this.OnShootProjectileAction;

            savedGuns.Add(SimulationController.CreateNewSimulationWeapon());
            savedGuns.Add(SimulationController.CreateNewSimulationWeapon());
        }

        public void OnShootProjectileAction(GameObject obj)
        {
            MoveTransform move = obj.GetComponentInChildren<MoveTransform>();

            SimulatedGun xGun = SimulationController.AvailableWeapons[0];
                
            xGun.CopyGunStatsExceptActions(this.gun);
            xGun.CopyAttackAction(this.gun);
            xGun.ShootPojectileAction = (Action<GameObject>)this.gun.ShootPojectileAction.Clone();
            xGun.ShootPojectileAction -= this.OnShootProjectileAction;
            xGun.numberOfProjectiles = 1;
            xGun.bursts = 0;
            xGun.spread = 0f;
            xGun.evenSpread = 0f;

            SimulatedGun yGun = SimulationController.AvailableWeapons[1];
            yGun.CopyGunStatsExceptActions(this.gun);
            yGun.CopyAttackAction(this.gun);
            yGun.ShootPojectileAction = (Action<GameObject>)this.gun.ShootPojectileAction.Clone();
            yGun.ShootPojectileAction -= this.OnShootProjectileAction;
            yGun.numberOfProjectiles = 1;
            yGun.bursts = 0;
            yGun.spread = 0f;
            yGun.evenSpread = 0f;
            yGun.gravity *= -1f;

            if (!(player.data.view.IsMine || PhotonNetwork.OfflineMode))
            {
                return;
            }

            Vector3 spawnPos = new Vector3(obj.transform.position.x * -1f, obj.transform.position.y, obj.transform.position.z);
            Vector3 shootDir = new Vector3(player.data.input.aimDirection.x * -1f, player.data.input.aimDirection.y, 0);

            xGun.SimulatedAttack(this.player.playerID, new Vector3(obj.transform.position.x * -1f, obj.transform.position.y, 0), new Vector3(player.data.input.aimDirection.x * -1f, player.data.input.aimDirection.y, 0), 1f, 1);
            yGun.SimulatedAttack(this.player.playerID, new Vector3(obj.transform.position.x, obj.transform.position.y * -1f, 0), new Vector3(player.data.input.aimDirection.x, player.data.input.aimDirection.y * -1f, 0), 1f, 1);
            yGun.SimulatedAttack(this.player.playerID, new Vector3(obj.transform.position.x * -1f, obj.transform.position.y * -1f, 0), new Vector3(player.data.input.aimDirection.x * -1f, player.data.input.aimDirection.y * -1f, 0), 1f, 1);
        }

        public void OnDestroy()
        {
            gun.ShootPojectileAction -= OnShootProjectileAction;
        }
    }

    internal class SimulatedBulletInit : MonoBehaviour
    {
        [PunRPC]
        public void RPCA_InitSimulatedBullet(string simID, int playerID, int projNum, float damageM, float randomSeed)
        {
            SimulationChamber.instance.GetSimulationWeapon(simID).FakeInit(this.gameObject, playerID, projNum, damageM, randomSeed);
        }
    }
}
