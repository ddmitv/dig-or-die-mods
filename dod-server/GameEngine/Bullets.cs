namespace GameEngine;

public sealed class CBulletDesc {
    public float m_radius;
    public float m_dispertionAngleRad;
    public float m_speedStart;
    public float m_speedEnd;
    public Color24 m_light;
    public bool m_goThroughEnnemies;
    public bool m_hasTrail;
    public bool m_hasSmoke;
    public bool m_pierceArmor;
    public float m_explosionRadius;
    public bool m_isPhysical;
    public bool m_inflame;
    public float m_grenadeYSpeed;
    public float m_lavaQuantity;
    public bool m_explosionSetFire;
    public int m_explosionMaxBlockHp = -1;
    public float m_criticsRate = 0.05f;

    public CBulletDesc(float radius, float dispersionAngleRad, float speedStart, float speedEnd, uint light) {
        m_radius = radius;
        m_dispertionAngleRad = dispersionAngleRad;
        m_speedStart = speedStart;
        m_speedEnd = speedEnd;
        m_light = Color24.FromNumber(light);
    }
}

public class GBullets {
    public static readonly CBulletDesc plasma = new(0.25f, 0f, 30f, 20f, 11358926U);

    public static readonly CBulletDesc shotgun = new(0.25f, 0.7f, 35f, 15f, 11358926U);

    public static readonly CBulletDesc snipe = new(0.25f, 0f, 50f, 40f, 11358926U) {
        m_criticsRate = 0.25f
    };

    public static readonly CBulletDesc laser = new(0.15f, 0.05f, 35f, 35f, 16733782U) {
        m_goThroughEnnemies = true
    };

    public static readonly CBulletDesc laserDrone = new(0.15f, 0f, 35f, 35f, 16733782U) {
        m_goThroughEnnemies = true
    };

    public static readonly CBulletDesc rocket = new(0.3f, 0.1f, 20f, 30f, 13619151U) {
        m_hasSmoke = true,
        m_explosionRadius = 4f,
        m_isPhysical = true,
        m_explosionMaxBlockHp = 0
    };

    public static readonly CBulletDesc megasnipe = new(0.35f, 0f, 50f, 40f, 11358926U) {
        m_goThroughEnnemies = true
    };

    public static readonly CBulletDesc zf0bullet = new(0.15f, 0.1f, 30f, 20f, 13619151U) {
        m_hasTrail = true,
        m_pierceArmor = true,
        m_isPhysical = true
    };

    public static readonly CBulletDesc laserGatling = new(0.15f, 0.15f, 35f, 35f, 16733782U) {
        m_goThroughEnnemies = true
    };

    public static readonly CBulletDesc grenade = new(0.3f, 0f, 30f, 10f, 0U) {
        m_grenadeYSpeed = -40f,
        m_explosionRadius = 4f,
        m_isPhysical = true,
        m_explosionMaxBlockHp = 350
    };

    public static readonly CBulletDesc grenadeUltimate = new(0.3f, 0f, 30f, 10f, 0U) {
        m_grenadeYSpeed = -40f,
        m_explosionRadius = 4f,
        m_isPhysical = true
    };

    public static readonly CBulletDesc particlesShotgun = new(0.35f, 0.7f, 35f, 15f, 8126431U);

    public static readonly CBulletDesc particlesSnipTurret = new(0.45f, 0f, 30f, 20f, 8126431U);

    public static readonly CBulletDesc particlesSnip = new(0.45f, 0f, 30f, 20f, 8126431U) {
        m_goThroughEnnemies = true
    };

    public static readonly CBulletDesc flamethrower = new(0.5f, 0.1f, 25f, 15f, 16640885U) {
        m_isPhysical = true,
        m_goThroughEnnemies = true,
        m_pierceArmor = true,
        m_inflame = true
    };

    public static readonly CBulletDesc defenses = new(0.2f, 0.1f, 30f, 20f, 0U);

    public static readonly CBulletDesc firefly = new(0.1f, 0.1f, 8f, 6f, 14151934U);

    public static readonly CBulletDesc dweller = new(0.1f, 0.1f, 10f, 8f, 0U) {
        m_isPhysical = true
    };

    public static readonly CBulletDesc dwellerBig = new(0.3f, 0.1f, 10f, 8f, 0U) {
        m_isPhysical = true
    };

    public static readonly CBulletDesc particleSmall = new(0.15f, 0.1f, 10f, 8f, 8126431U);

    public static readonly CBulletDesc particleMedium = new(0.3f, 0.1f, 10f, 8f, 8126431U);

    public static readonly CBulletDesc fireballSmall = new(0.15f, 0.1f, 10f, 8f, 16751950U) {
        m_isPhysical = true,
        m_inflame = true
    };

    public static readonly CBulletDesc fireballBig = new(0.3f, 0.1f, 10f, 8f, 16751950U) {
        m_explosionRadius = 3f,
        m_isPhysical = true,
        m_inflame = true
    };

    public static readonly CBulletDesc grenadeLava = new(0.4f, 0f, 25f, 20f, 16751950U) {
        m_grenadeYSpeed = -12f,
        m_explosionRadius = 4f,
        m_isPhysical = true,
        m_lavaQuantity = 0.5f
    };

    public static readonly CBulletDesc meteor = new(0.4f, 0f, 15f, 15f, 16751950U) {
        m_explosionRadius = 2f,
        m_isPhysical = true,
        m_explosionSetFire = true
    };
}

// public sealed class CBullet {
//     public const float ZF0_LockTime = 3f;
// 
//     public CAttackDesc m_attackDesc;
//     public CUnit m_attacker;
//     public Vector2 m_pos;
//     public float m_angleRad;
//     public float m_distDone;
//     public float m_fireTime = -1f;
//     private float m_rand = 0;
//     private float m_deathTime = -1f;
//     private List<CUnit> m_unitsHit = [];
//     private bool m_isWet;
// 
//     public CBullet(CAttackDesc attackDesc, CUnit attacker, Vector2 firePos, float angle, Vector2 aimedPos) {
//         m_attackDesc = attackDesc;
//         m_attacker = attacker;
//         m_pos = firePos;
//         m_rand = GameRandom.Float();
//         m_fireTime = GameVars.SimuTimeF;
//         m_angleRad = angle + 0.5f * GameRandom.FloatBetween(-Desc.m_dispertionAngleRad, Desc.m_dispertionAngleRad);
//         if (Desc.m_inflame && World.Grid[(int)m_pos.x, (int)m_pos.y].m_water > 0.3f) {
//             return;
//         }
//         // CheckCollisions(false, default(Vector2));
//     }
// 
//     public CBulletDesc Desc => m_attackDesc.m_bulletDesc;
// }
