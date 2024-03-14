using UnityEngine;

public interface IProjectileAbility
{
    Vector3 ProjectileDirection { get; set; }
    float ProjectileSpeed { get; set; }
    float ProjectileRange { get; set; }

    // Add any other common functionality here
    void InitializeProjectile(Vector3 direction, float speed, float range);
    void MoveProjectile();
}