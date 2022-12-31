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

    /// <summary>
    /// The class to interact with for creating weapons.
    /// </summary>
    public static class SimulationController
    {
        //public static ReadOnlyCollection<SimulatedGun> AvailableWeapons => new ReadOnlyCollection<SimulatedGun>(SimulationChamber.instance.simulationWeapons.Values.ToList());

        /// <summary>
        /// Creates a simulated gun for a player to use.
        /// </summary>
        /// <returns>A Simulated Gun.</returns>
        public static SimulatedGun CreateNewSimulationWeapon()
        {
            return SimulationChamber.instance.SpawnSimulationGun();
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

    /// <summary>
    /// A mono for the purposes of explaining how the mod works.
    /// </summary>
    public class MirrorSimulation : MonoBehaviour
    {
        Player player;
        Gun gun;

        // A list of guns created for this mono saved here.
        // Ideally you'll make a pool of guns for your mod to use.
        public static SimulatedGun[] savedGuns = new SimulatedGun[2];

        public void Start()
        {
            // Get Player
            this.player = this.GetComponentInParent<Player>();
            // Get Gun
            this.gun = this.player.data.weaponHandler.gun;
            // Hook up our action.
            this.gun.ShootPojectileAction += this.OnShootProjectileAction;

            // Checks to see if we have a saved gun already, if not, we make one.
            if (savedGuns[0] == null)
            {
                savedGuns[0] = SimulationController.CreateNewSimulationWeapon();
            }

            // Checks to see if we have a second saved gun already, if not, we make one.
            if (savedGuns[1] == null)
            {
                savedGuns[1] = SimulationController.CreateNewSimulationWeapon();
            }
        }

        public void OnShootProjectileAction(GameObject obj)
        {
            /*************************************************************************
            **************************************************************************
            *** Here's where we sync our guns so that people see the same effect when
            *** the guns are shot.
            **************************************************************************
            *************************************************************************/

            // We get our first gun that we made earlier
            // We're going to be using this as our gun for mirroring across the y-axis
            SimulatedGun xGun = savedGuns[0];  
            
            // We copy over our gun stats, including actions, so that it's pretty much a copy of our gun.
            // Note, the methods for copying actions actually create separate instances of those actions
            xGun.CopyGunStatsExceptActions(this.gun);
            xGun.CopyAttackAction(this.gun);
            xGun.CopyShootProjectileAction(this.gun);

            // Since we created a separate instance of our shootprojectile action, we can safely remove this action
            // to avoid our simulated gun from triggering it as well.
            //
            // If we had simply done `xGun.ShootPojectileAction = this.gun.ShootPojectileAction;` this would have also
            // removed the action from `this.gun.ShootPojectileAction`.
            xGun.ShootPojectileAction -= this.OnShootProjectileAction;

            // We only want to fire 1 bullet per bullet, since we're mirroring our attacks.
            xGun.numberOfProjectiles = 1;
            xGun.bursts = 0;
            xGun.spread = 0f;
            xGun.evenSpread = 0f;

            // Our second gun is used to mirror about the y-axis
            // We use this gun since we want to have different values on our y than our x.
            SimulatedGun yGun = savedGuns[1];

            // Copy actions like before
            yGun.CopyGunStatsExceptActions(this.gun);
            yGun.CopyAttackAction(this.gun);
            yGun.CopyShootProjectileAction(this.gun);
            yGun.ShootPojectileAction -= this.OnShootProjectileAction;

            // We invert gravity this time though, so it looks like our bullets are mirroring each other
            yGun.numberOfProjectiles = 1;
            yGun.bursts = 0;
            yGun.spread = 0f;
            yGun.evenSpread = 0f;
            yGun.gravity *= -1f;

            /*************************************************************************
            **************************************************************************
            *** We check to see if the player who's shooting is that player, otherwise
            *** we'll end up firing a simulation gun for each player in the game.
            **************************************************************************
            *************************************************************************/
            if (!(player.data.view.IsMine || PhotonNetwork.OfflineMode))
            {
                return;
            }

            // Fires our gun that's mirrored across the y-axis, so we invert our x position and shoot angle.
            xGun.SimulatedAttack(this.player.playerID, new Vector3(obj.transform.position.x * -1f, obj.transform.position.y, 0), new Vector3(player.data.input.aimDirection.x * -1f, player.data.input.aimDirection.y, 0), 1f, 1);
            // Fires our gun that's mirrored across the x axis, inverting our y position and shoot angle.
            yGun.SimulatedAttack(this.player.playerID, new Vector3(obj.transform.position.x, obj.transform.position.y * -1f, 0), new Vector3(player.data.input.aimDirection.x, player.data.input.aimDirection.y * -1f, 0), 1f, 1);
            // Fires our gun that's mirrored across the x-axis, inverting our x and y position and shoot angle.
            yGun.SimulatedAttack(this.player.playerID, new Vector3(obj.transform.position.x * -1f, obj.transform.position.y * -1f, 0), new Vector3(player.data.input.aimDirection.x * -1f, player.data.input.aimDirection.y * -1f, 0), 1f, 1);
        }

        public void OnDestroy()
        {
            // Remove our action when the mono is removed
            gun.ShootPojectileAction -= OnShootProjectileAction;
        }
    }
}
