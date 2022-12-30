# GunChargePatch

A mod that makes use of the built in charge system for the `Gun` class, and makes it viable.

To use, simply set `gun.Charge = true;` in your card's `SetupCard`.

<details>
<summary>Change log</summary>

----
### v0.0.3
- GunChargePatch will now search for modded prefab bullets when a card is picked.

----
### v0.0.2
- Patches an issue with not properly recognizing other bullet types.

----
### v0.0.0
- Added the ability to RPC a bullet charge to bullets.

----
### v0.0.0
- Initial Release

</details>

<details>
<summary>Gun Fields</summary>
### useCharge()
```cs
bool useCharge
```
#### Description
Tells a gun that it's a charged weapon now.

### chargeDamageMultiplier()
```cs
float chargeDamageMultiplier
```
#### Description
The multiplier for a gun's damage based on its charge. Default value of 1.

### chargeSpeedTo()
```cs
float chargeSpeedTo
```
#### Description
The multiplier for a gun's bullet speed based on its charge. Default value of 1.
</details>

<details>
<summary>Extension Methods</summary>

## ProjectileHit

---

### BulletCharge()
```cs
float GetBulletCharge(this ProjectileHit instance)
```
#### Description
Returns the charge of the bullet instance.

#### Parameters

#### Example Usage
```CSHARP
float charge = this.gameObject.GetComponent<ProjectileHit>().GetBulletCharge().charge;
```

---

## Gun

---

### BulletCharge()
```cs
GunAdditionalData GetAdditionalData(this Gun instance)
```
#### Description
Returns the additional data associated with a gun object.

#### Parameters

#### Example Usage
```CSHARP
gun.GetAdditionalData().chargeTime = 1f;
```
</details>

<details>
<summary>Classes</summary>

### GunAdditionalData
```cs
class GunAdditionalData
```
#### Fields
- float chargeTime - default 1. How long it takes to charge a weapon.
- bool useDefaultChargingMethod - default true. Only change if you know what you're doing.

#### Description
Information associated with the `Gun` object for a player.

#### Example Usage
```CSHARP
gun.GetAdditionalData().chargeTime = 1f;
```
</details>