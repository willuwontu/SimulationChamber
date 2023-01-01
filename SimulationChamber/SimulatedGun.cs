using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using Photon.Pun;
using SimulationChamber.Extensions;
using UnityEngine;

namespace SimulationChamber
{
    /// <summary>
    /// The gun used to shoots.
    /// </summary>
    public class SimulatedGun : Gun
    {
        private int simulationID;
        private float usedCooldown
        {
            get
            {
                if (!this.lockGunToDefault)
                {
                    return this.attackSpeed;
                }
                return this.defaultCooldown;
            }
        }

        private void Awake()
        {
            this.simulationID = SimulationChamber.instance.currentCount;
            SimulationChamber.instance.currentCount += 1;
            SimulationChamber.instance.simulationWeapons.Add(this.simulationID, this);

            this.shootPosition = this.transform;
        }

        private void Start()
        {

        }

        private void Update()
        {

        }

        /// <summary>
        /// This is what you should be using to attack with.
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="spawnPos"></param>
        /// <param name="shootAngle"></param>
        /// <param name="charge"></param>
        /// <param name="damageM"></param>
        /// <param name="followTransform"></param>
        /// <param name="useTransformForward"></param>
        /// <returns></returns>
        public bool SimulatedAttack(int playerID, Vector3 spawnPos, Vector3 shootAngle, float charge, float damageM, Transform followTransform = null, bool useTransformForward = false)
        {
            if (shootAngle == Vector3.zero)
            {
                return false;
            }

            DoAttack(playerID, spawnPos, shootAngle, charge, damageM, followTransform, useTransformForward);

            return true;
        }

        [System.ObsoleteAttribute("Does Not Work, use SimulatedGun::SimulatedAttack(int playerID, Vector3 spawnPos, Vector3 shootAngle, float charge, float damageM) instead.", true)]
        public override bool Attack(float charge, bool forceAttack = false, float damageM = 1, float recoilM = 1, bool useAmmo = true)
        {
            UnityEngine.Debug.Log("Stopped Simulated Gun Attack, fuck off with your bullshit reflection to use this method.");
            return false;
        }

        private void DoAttack(int playerID, Vector3 spawnPos, Vector3 shootAngle, float charge, float damageM = 1f, Transform followTransform = null, bool useTransformAngle = false)
        {
            if (this.GetAttackAction() != null)
            {
                this.GetAttackAction()();
            }

            SimulationChamber.instance.StartCoroutine(this.FireBurst(playerID, spawnPos, shootAngle, charge, damageM, followTransform, useTransformAngle));
        }

        Vector3 ForceShootDir
        {
            get
            {
                return (Vector3)typeof(Gun).GetField("forceShootDir", BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField).GetValue(this);
            }
            set
            {
                typeof(Gun).GetField("forceShootDir", BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField).SetValue(this, value);
            }
        }

        Vector3 SpawnPos 
        { 
            get 
            { 
                return (Vector3)typeof(Gun).GetField("spawnPos", BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField).GetValue(this); 
            } 
            set
            {
                typeof(Gun).GetField("spawnPos", BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField).SetValue(this, value);
            }
        }

        private IEnumerator FireBurst(int playerID, Vector3 spawnPos, Vector3 shootAngle, float charge, float damageM = 1f, Transform followTransform = null, bool useTransformAngle = false)
        {
            int currentNumberOfProjectiles = this.lockGunToDefault ? 1 : (this.numberOfProjectiles + Mathf.RoundToInt(this.chargeNumberOfProjectilesTo * charge));

            if (this.timeBetweenBullets == 0f)
            {
                GamefeelManager.GameFeel(base.transform.up * this.shake);
                //this.soundGun.PlayShot(currentNumberOfProjectiles);
            }
            int num;
            for (int ii = 0; ii < Mathf.Clamp(this.bursts, 1, 100); ii = num + 1)
            {
                for (int i = 0; i < this.projectiles.Length; i++)
                {
                    for (int j = 0; j < currentNumberOfProjectiles; j++)
                    {
                        if (this.CheckIsMine(playerID))
                        {
                            this.shootPosition.forward = shootAngle;

                            var spawnLoc = spawnPos;

                            if (followTransform != null)
                            {
                                spawnLoc = followTransform.position;

                                if (useTransformAngle)
                                {
                                    this.shootPosition.forward = followTransform.forward;
                                }
                            }

                            Quaternion shootDir = this.getShootRotation(j, currentNumberOfProjectiles, charge);

                            GameObject gameObject = PhotonNetwork.Instantiate(this.projectiles[i].objectToSpawn.gameObject.name, spawnLoc, shootDir, 0, null);

                            if (Chainloader.PluginInfos.Select(plugin => plugin.Key).Contains("com.rounds.willuwontu.gunchargepatch"))
                            {
                                gameObject.GetComponent<PhotonView>().RPC("RPCA_SetBulletCharge", RpcTarget.All, new object[]
                                {
                                    charge
                                }); 
                            }

                            gameObject.GetComponent<PhotonView>().RPC(nameof(SimulatedBulletInit.RPCA_InitSimulatedBullet), RpcTarget.All, new object[]
                            {
                                simulationID,
                                playerID,
                                currentNumberOfProjectiles,
                                damageM,
                                UnityEngine.Random.Range(0f, 1f)
                            });
                        }
                        if (this.timeBetweenBullets != 0f)
                        {
                            GamefeelManager.GameFeel(base.transform.up * this.shake);
                            //this.soundGun.PlayShot(currentNumberOfProjectiles);
                        }
                    }
                }
                if (this.bursts > 1 && ii + 1 == Mathf.Clamp(this.bursts, 1, 100))
                {
                    //this.soundGun.StopAutoPlayTail();
                }
                if (this.timeBetweenBullets > 0f)
                {
                    yield return new WaitForSeconds(this.timeBetweenBullets);
                }
                num = ii;
            }
            yield break;
        }

        private bool CheckIsMine(int playerID)
        {
            var player = PlayerManager.instance.GetPlayerWithID(playerID);

            bool result = false;
            if (player && player.data)
            {
                result = player.data.view.IsMine;
            }
            return result;
        }

        /// <summary>
        /// This is used to initialize the bullets.
        /// </summary>
        /// <param name="bullet"></param>
        /// <param name="playerID"></param>
        /// <param name="usedNumberOfProjectiles"></param>
        /// <param name="damageM"></param>
        /// <param name="randomSeed"></param>
        public void FakeInit(GameObject bullet, int playerID, int usedNumberOfProjectiles, float damageM, float randomSeed)
        {
            this.spawnedAttack = bullet.GetComponent<SpawnedAttack>();
            if (!this.spawnedAttack)
            {
                this.spawnedAttack = bullet.AddComponent<SpawnedAttack>();
            }
            this.ApplyPlayerStuff(playerID, bullet);
            if (!bullet.GetComponentInChildren<DontChangeMe>())
            {
                this.ApplyProjectileStats(bullet, usedNumberOfProjectiles, damageM, randomSeed, playerID);
            }
            if (this.soundDisableRayHitBulletSound)
            {
                RayHitBulletSound component = bullet.GetComponent<RayHitBulletSound>();
                if (component != null)
                {
                    component.disableImpact = true;
                }
            }
            if (this.GetShootProjectileAction() != null)
            {
                this.GetShootProjectileAction()(bullet);
            }
        }

        private void ApplyPlayerStuff(int playerID, GameObject obj)
        {
            ProjectileHit component = obj.GetComponent<ProjectileHit>();

            var player = PlayerManager.instance.GetPlayerWithID(playerID);

            component.ownWeapon = base.gameObject;
            if (this.player)
            {
                component.ownPlayer = player;
            }
            this.spawnedAttack.spawner = player;
            this.spawnedAttack.attackID = this.attackID;
        }

        private void ApplyProjectileStats(GameObject obj, int numOfProj = 1, float damageM = 1f, float randomSeed = 0f, int playerID = 0)
        {
            ProjectileHit bullet = obj.GetComponent<ProjectileHit>();
            bullet.dealDamageMultiplierr *= this.bulletDamageMultiplier;
            bullet.damage *= this.damage * damageM;
            bullet.percentageDamage = this.percentageDamage;
            bullet.stun = bullet.damage / 150f;
            bullet.force *= this.knockback;
            bullet.movementSlow = this.slow;
            typeof(ProjectileHit).GetField("hasControl", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField).SetValue(bullet, this.CheckIsMine(playerID));
            bullet.projectileColor = this.projectileColor;
            bullet.unblockable = this.unblockable;
            RayCastTrail trail = obj.GetComponent<RayCastTrail>();
            if (this.ignoreWalls)
            {
                trail.mask = trail.ignoreWallsMask;
            }
            if (trail)
            {
                trail.extraSize += this.size;
            }
            var player = PlayerManager.instance.GetPlayerWithID(playerID);
            if (player)
            {
                PlayerSkin playerSkinColors = PlayerSkinBank.GetPlayerSkinColors(player.playerID);
                bullet.team = playerSkinColors;
                obj.GetComponent<RayCastTrail>().teamID = player.playerID;
                SetTeamColor.TeamColorThis(obj, playerSkinColors);
            }
            List<ObjectsToSpawn> list = new List<ObjectsToSpawn>();
            for (int i = 0; i < this.objectsToSpawn.Length; i++)
            {
                list.Add(this.objectsToSpawn[i]);
                if (this.objectsToSpawn[i].AddToProjectile && (!this.objectsToSpawn[i].AddToProjectile.gameObject.GetComponent<StopRecursion>() || !this.isProjectileGun))
                {
                    GameObject gameObject = UnityEngine.GameObject.Instantiate<GameObject>(this.objectsToSpawn[i].AddToProjectile, bullet.transform.position, bullet.transform.rotation, bullet.transform);
                    gameObject.transform.localScale *= 1f * (1f - this.objectsToSpawn[i].scaleFromDamage) + bullet.damage / 55f * this.objectsToSpawn[i].scaleFromDamage;
                    if (this.objectsToSpawn[i].scaleStacks)
                    {
                        gameObject.transform.localScale *= 1f + (float)this.objectsToSpawn[i].stacks * this.objectsToSpawn[i].scaleStackM;
                    }
                    if (this.objectsToSpawn[i].removeScriptsFromProjectileObject)
                    {
                        MonoBehaviour[] componentsInChildren = gameObject.GetComponentsInChildren<MonoBehaviour>();
                        for (int j = 0; j < componentsInChildren.Length; j++)
                        {
                            if (componentsInChildren[j].GetType().ToString() != "SoundImplementation.SoundUnityEventPlayer")
                            {
                                UnityEngine.GameObject.Destroy(componentsInChildren[j]);
                            }
                            global::Debug.Log(componentsInChildren[j].GetType().ToString());
                        }
                    }
                }
            }
            bullet.objectsToSpawn = list.ToArray();
            if (this.reflects > 0)
            {
                RayHitReflect rayHitReflect = obj.gameObject.AddComponent<RayHitReflect>();
                rayHitReflect.reflects = this.reflects;
                rayHitReflect.speedM = this.speedMOnBounce;
                rayHitReflect.dmgM = this.dmgMOnBounce;
            }
            if (!this.forceSpecificShake)
            {
                float num = bullet.damage / 100f * ((1f + this.usedCooldown) / 2f) / ((1f + (float)numOfProj) / 2f) * 2f;
                float num2 = Mathf.Clamp((0.2f + bullet.damage * (((float)this.numberOfProjectiles + 2f) / 2f) / 100f * ((1f + this.usedCooldown) / 2f)) * 1f, 0f, 3f);
                bullet.shake = num * this.shakeM;
                this.shake = num2;
            }
            MoveTransform move = obj.GetComponent<MoveTransform>();
            move.localForce *= this.projectileSpeed;
            typeof(MoveTransform).GetField("simulationSpeed", BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField).SetValue(move, this.projectielSimulatonSpeed);
            move.gravity *= this.gravity;
            move.worldForce *= this.gravity;
            move.drag = this.drag;
            move.drag = Mathf.Clamp(move.drag, 0f, 45f);
            move.velocitySpread = Mathf.Clamp(this.spread * 50f, 0f, 50f);
            move.dragMinSpeed = this.dragMinSpeed;
            move.localForce *= Mathf.Lerp(1f - move.velocitySpread * 0.01f, 1f + move.velocitySpread * 0.01f, randomSeed);
            move.selectedSpread = 0f;
            if (this.damageAfterDistanceMultiplier != 1f)
            {
                obj.AddComponent<ChangeDamageMultiplierAfterDistanceTravelled>().muiltiplier = this.damageAfterDistanceMultiplier;
            }
            if (this.cos > 0f)
            {
                obj.gameObject.AddComponent<Cos>().multiplier = this.cos;
            }
            if (this.destroyBulletAfter != 0f)
            {
                obj.GetComponent<RemoveAfterSeconds>().seconds = this.destroyBulletAfter;
            }
            if (this.spawnedAttack && this.projectileColor != Color.black)
            {
                this.spawnedAttack.SetColor(this.projectileColor);
            }
        }

        private bool useSelfAttackAction = true;
        private bool useOtherGunAttackAction = false;
        private Gun attackActionGun = null;

        /// <summary>
        /// The attack action of a Simulated Gun.
        /// </summary>
        public Action AttackAction
        {
            get
            {
                return (Action)typeof(Gun).GetField("attackAction", BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField).GetValue(this);
            }
            set
            {
                typeof(Gun).GetField("attackAction", BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField).SetValue(this, value);
            }
        }

        private Action GetAttackAction()
        {
            Action attackAction = null;

            if (this.useSelfAttackAction)
            {
                attackAction += this.AttackAction;
            }
            if (useOtherGunAttackAction && attackActionGun)
            {
                attackAction += attackActionGun.AttackAction();
            }

            return attackAction;
        }

        private bool useSelfProjectileAction = true;
        private bool useOtherGunProjectileAction = false;
        private Gun projectileActionGun = null;

        private Action<GameObject> GetShootProjectileAction()
        {
            Action<GameObject> projectileAction = null;

            if (this.useSelfProjectileAction) 
            {
                projectileAction += this.ShootPojectileAction;
            }
            if (useOtherGunProjectileAction && projectileActionGun)
            {
                projectileAction += projectileActionGun.ShootPojectileAction;
            }

            return projectileAction;
        }

        SpawnedAttack spawnedAttack;

        /// <summary>
        /// Clones the <seealso cref="Gun.attackAction"/> of a gun, and adds it as its own set of delegates to this gun.
        /// </summary>
        /// <param name="copyFromGun">The gun to copy the action from.</param>
        public void CopyAttackAction(Gun copyFromGun)
        {
            if ((Action)Traverse.Create(copyFromGun).Field("attackAction").GetValue() == null)
            {
                return;
            }

            this.AttackAction = (Action)(((Action)Traverse.Create(copyFromGun).Field("attackAction").GetValue()).Clone());
        }

        /// <summary>
        /// Clones the <seealso cref="Gun.ShootPojectileAction"/> of a gun, and adds it as its own set of delegates to this gun.
        /// </summary>
        /// <param name="copyFromGun">The gun to copy the action from.</param>
        public void CopyShootProjectileAction(Gun copyFromGun)
        {
            if (copyFromGun.ShootPojectileAction == null)
            {
                return;
            }

            this.ShootPojectileAction = (Action<GameObject>)(copyFromGun.ShootPojectileAction.Clone());
        }

        /// <summary>
        /// Copies the stats of one gun to this one.
        /// </summary>
        /// <param name="copyFromGun">The gun to copy the stats of.</param>
        public void CopyGunStatsExceptActions(Gun copyFromGun)
        {
            this.ammo = copyFromGun.ammo;
            this.ammoReg = copyFromGun.ammoReg;
            this.attackID = copyFromGun.attackID;
            this.attackSpeed = copyFromGun.attackSpeed;
            this.attackSpeedMultiplier = copyFromGun.attackSpeedMultiplier;
            this.bodyRecoil = copyFromGun.bodyRecoil;
            this.bulletDamageMultiplier = copyFromGun.bulletDamageMultiplier;
            this.bulletPortal = copyFromGun.bulletPortal;
            this.bursts = copyFromGun.bursts;
            this.chargeDamageMultiplier = copyFromGun.chargeDamageMultiplier;
            this.chargeEvenSpreadTo = copyFromGun.chargeEvenSpreadTo;
            this.chargeNumberOfProjectilesTo = copyFromGun.chargeNumberOfProjectilesTo;
            this.chargeRecoilTo = copyFromGun.chargeRecoilTo;
            this.chargeSpeedTo = copyFromGun.chargeSpeedTo;
            this.chargeSpreadTo = copyFromGun.chargeSpreadTo;
            this.cos = copyFromGun.cos;
            this.currentCharge = copyFromGun.currentCharge;
            this.damage = copyFromGun.damage;
            this.damageAfterDistanceMultiplier = copyFromGun.damageAfterDistanceMultiplier;
            this.defaultCooldown = copyFromGun.defaultCooldown;
            this.destroyBulletAfter = copyFromGun.destroyBulletAfter;
            this.dmgMOnBounce = copyFromGun.dmgMOnBounce;
            this.dontAllowAutoFire = copyFromGun.dontAllowAutoFire;
            this.drag = copyFromGun.drag;
            this.dragMinSpeed = copyFromGun.dragMinSpeed;
            this.evenSpread = copyFromGun.evenSpread;
            this.explodeNearEnemyDamage = copyFromGun.explodeNearEnemyDamage;
            this.forceSpecificAttackSpeed = copyFromGun.forceSpecificAttackSpeed;
            this.forceSpecificShake = copyFromGun.forceSpecificShake;
            this.gravity = copyFromGun.gravity;
            this.hitMovementMultiplier = copyFromGun.hitMovementMultiplier;
            this.ignoreWalls = copyFromGun.ignoreWalls;
            this.isProjectileGun = copyFromGun.isProjectileGun;
            this.isReloading = copyFromGun.isReloading;
            this.knockback = copyFromGun.knockback;
            this.lockGunToDefault = copyFromGun.lockGunToDefault;
            this.multiplySpread = copyFromGun.multiplySpread;
            this.numberOfProjectiles = copyFromGun.numberOfProjectiles;
            this.objectsToSpawn = copyFromGun.objectsToSpawn.ToArray();
            this.overheatMultiplier = copyFromGun.overheatMultiplier;
            this.percentageDamage = copyFromGun.percentageDamage;
            this.player = copyFromGun.player;
            this.projectielSimulatonSpeed = copyFromGun.projectielSimulatonSpeed;
            this.projectileColor = copyFromGun.projectileColor;
            this.projectiles = copyFromGun.projectiles.ToArray();
            this.projectileSize = copyFromGun.projectileSize;
            this.projectileSpeed = copyFromGun.projectileSpeed;
            this.randomBounces = copyFromGun.randomBounces;
            this.recoil = copyFromGun.recoil;
            this.recoilMuiltiplier = copyFromGun.recoilMuiltiplier;
            this.reflects = copyFromGun.reflects;
            this.reloadTime = copyFromGun.reloadTime;
            this.reloadTimeAdd = copyFromGun.reloadTimeAdd;
            this.shake = copyFromGun.shake;
            this.shakeM = copyFromGun.shakeM;
            this.size = copyFromGun.size;
            this.slow = copyFromGun.slow;
            this.smartBounce = copyFromGun.smartBounce;
            this.soundDisableRayHitBulletSound = copyFromGun.soundDisableRayHitBulletSound;
            this.soundGun = copyFromGun.soundGun;
            this.soundImpactModifier = copyFromGun.soundImpactModifier;
            this.soundShotModifier = copyFromGun.soundShotModifier;
            this.spawnSkelletonSquare = copyFromGun.spawnSkelletonSquare;
            this.speedMOnBounce = copyFromGun.speedMOnBounce;
            this.spread = copyFromGun.spread;
            this.teleport = copyFromGun.teleport;
            this.timeBetweenBullets = copyFromGun.timeBetweenBullets;
            this.timeToReachFullMovementMultiplier = copyFromGun.timeToReachFullMovementMultiplier;
            this.unblockable = copyFromGun.unblockable;
            this.useCharge = copyFromGun.useCharge;
            this.waveMovement = copyFromGun.waveMovement;
        }
    }
}
