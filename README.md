# Simulation Chamber

A mod to make it easy to fire a gun anywhere you need it on the map.

<details>
<summary>Change log</summary>

### v 0.0.2
---
- Mod ID changed to correct format.
- Small Fixes, example made better.

### v 0.0.2
---
- Fixed issues with multiplayer.

### v0.0.0
---
- Initial Release

</details>

<details>
<summary>Simulated Gun</summary>

### SimulatedAttack()
```cs
bool SimulatedAttack(int playerID, Vector3 spawnPos, Vector3 shootAngle, float charge, float damageM, Transform followTransform = null, bool useTransformForward = false)
```
#### Description
Fires the `SimulatedGun`.

#### Parameters
 - *int* `playerID` the playerID of the player that is attacking.
 - *Vector3* `spawnPos` the spawnPosition of the attack.
 - *Vector3* `shootAngle` the angle of the attack.
 - *float* `charge` the charge of the attack. Only matters if `Gun::useCharge` is true and GunChargePatch is installed.
 - *float* `damageM` the damage multiplier of the attack.
 - *Transform* `followTransform` an optional transform to use for the attacks. Useful if the gun has bursts and you want it to move with an object while firing.
 - *bool* `useTransformForward` requires followTransform to be set. Optional parameter to use the `followTransform`'s forward vector for the firing angle.

#### Example Usage
```CSHARP
xGun.SimulatedAttack(this.player.playerID, new Vector3(obj.transform.position.x * -1f, obj.transform.position.y, 0), new Vector3(player.data.input.aimDirection.x * -1f, player.data.input.aimDirection.y, 0), 1f, 1);
```

### CopyGunStatsExceptActions()
```cs
void CopyGunStatsExceptActions(Gun copyFromGun)
```
#### Description
Copies the stats from a gun onto the simulated gun with the exception of actions.

#### Parameters
 - *Gun* `copyFromGun`

#### Example Usage
```CSHARP
yGun.CopyGunStatsExceptActions(this.gun);
```

### CopyShootProjectileAction()
```cs
void CopyShootProjectileAction(Gun copyFromGun)
```
#### Description
Copies the shootPojectileAction from a gun onto the simulated gun as a distinct stack of its delegates.

#### Parameters
 - *Gun* `copyFromGun`

#### Example Usage
```CSHARP
yGun.CopyShootProjectileAction(this.gun);
```

### CopyAttackAction()
```cs
void CopyAttackAction(Gun copyFromGun)
```
#### Description
Copies the attack action from a gun onto the simulated gun as a distinct stack of its delegates.

#### Parameters
 - *Gun* `copyFromGun`

#### Example Usage
```CSHARP
xGun.CopyAttackAction(this.gun);
```
</details>

<details>
<summary>Example Mono</summary>
A mono included in the library for the purposes of doing this.

```cs
/// <summary>
/// A mono for the purposes of explaining how the mod works.
/// </summary>
public class MirrorSimulation : MonoBehaviour
{
    Player player;
    Gun gun;

    // A list of guns created for this mono saved here.
    // Ideally you'll make a pool of guns for your mod to use.
    public SimulatedGun[] savedGuns = new SimulatedGun[2];

    public static GameObject _stopRecursionObj = null;

    public static GameObject StopRecursionObj
    {
        get
        {
            if (_stopRecursionObj == null)
            {
                _stopRecursionObj = new GameObject("A_StopRecursion", typeof(StopRecursion));
                DontDestroyOnLoad(_stopRecursionObj);
            }
            return _stopRecursionObj;
        }
    }

    public static ObjectsToSpawn[] StopRecursionSpawn
    {
        get
        {
            return new ObjectsToSpawn[] { new ObjectsToSpawn() { AddToProjectile = StopRecursionObj } };
        }
    }

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
            // We spawn a new object since this allows us manipulate the gun object's position without messing with the player's gameobjest.
            savedGuns[0] = new GameObject("X-Gun").AddComponent<SimulatedGun>();
        }

        // Checks to see if we have a second saved gun already, if not, we make one.
        if (savedGuns[1] == null)
        {
            savedGuns[1] = new GameObject("Y-Gun").AddComponent<SimulatedGun>();
        }
    }

    public void OnShootProjectileAction(GameObject obj)
    {
        // If the bullet has the StopRecursion component in it somewhere, we don't want to trigger.
        if (obj.GetComponentsInChildren<StopRecursion>().Length > 0)
        {
            return;
        }

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
        xGun.objectsToSpawn = xGun.objectsToSpawn.Concat(StopRecursionSpawn).ToArray();

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
        yGun.objectsToSpawn = yGun.objectsToSpawn.Concat(StopRecursionSpawn).ToArray();

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

        UnityEngine.GameObject.Destroy(savedGuns[0]);
        UnityEngine.GameObject.Destroy(savedGuns[1]);
    }
}
```
</details>