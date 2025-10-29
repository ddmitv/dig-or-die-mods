


using GameEngine;

public static class SOutgame {
    private static readonly CMode m_mode = new CModeMulti();

    public static CMode Mode => m_mode;
}

public abstract class CMode {
    public abstract CUnitMonster.CDesc[] GetMonstersList(Vector2 pos);
    public virtual float NightSpawnMultiplier => 1f;
}

public class CModeMulti : CMode {
    private static readonly CUnitMonster.CDesc[] volcanoBottomTop = [GUnits.lavaAnt];
    private static readonly CUnitMonster.CDesc[] volcanoBottomBottom = [GUnits.lavaBat];
    private static readonly CUnitMonster.CDesc[] bottomCavern = [GUnits.balrogMini, GUnits.particleBird2];
    private static readonly CUnitMonster.CDesc[] particles2 = [GUnits.particleBird, GUnits.particleGround];
    private static readonly CUnitMonster.CDesc[] crystalCaverns = [GUnits.antClose, GUnits.antDist];
    private static readonly CUnitMonster.CDesc[] bottomOcean = [GUnits.shark];
    private static readonly CUnitMonster.CDesc[] topOcean = [GUnits.fishBlack, GUnits.batBlack];
    private static readonly CUnitMonster.CDesc[] chasmBottom = [GUnits.batBlack];
    private static readonly CUnitMonster.CDesc[] rockLayer = [GUnits.dwellerBlack, GUnits.houndBlack];
    private static readonly CUnitMonster.CDesc[] chasmTop = [GUnits.bat];
    private static readonly CUnitMonster.CDesc[] volcanoCore = [GUnits.dwellerBlack, GUnits.houndBlack];
    private static readonly CUnitMonster.CDesc[] dirtUnderground = [GUnits.dweller, GUnits.hound];
    private static readonly CUnitMonster.CDesc[] volcanoSide = [GUnits.fireflyRed];
    private static readonly CUnitMonster.CDesc[] surface = [GUnits.firefly, GUnits.hound, GUnits.fish];
    private static readonly CUnitMonster.CDesc[] lowIslands = [GUnits.fireflyRed];
    private static readonly CUnitMonster.CDesc[] upperIslands = [GUnits.fireflyBlack];
    private static readonly CUnitMonster.CDesc[] skylands = [GUnits.fireflyExplosive, GUnits.fireflyBlack];

    public override CUnitMonster.CDesc[] GetMonstersList(Vector2 pos) {
        if (pos.y < 580f) {
            if (pos.x > 800f && pos.x < 900f) {
                return (pos.y >= 190f) ? volcanoBottomTop : volcanoBottomBottom;
            }
            if (pos.y < 130f) {
                return bottomCavern;
            }
            if (pos.y < 275f) {
                return particles2;
            }
            if (pos.y < 350f) {
                return crystalCaverns;
            }
            if (pos.y < 400f) {
                return bottomOcean;
            }
            if (pos.y < 450f) {
                return topOcean;
            }
            if (pos.x > 150f && pos.x < 250f) {
                return chasmBottom;
            }
            return rockLayer;
        } else if (pos.y < 755f) {
            if (pos.x > 150f && pos.x < 250f) {
                return chasmTop;
            }
            if (pos.x > 765f && pos.x < 940f) {
                return volcanoCore;
            }
            if (pos.y < 700f) {
                return dirtUnderground;
            }
            if (pos.x > 710f) {
                return volcanoSide;
            }
            return surface;
        } else {
            if (pos.y < 790f) {
                return lowIslands;
            }
            if (pos.y < 920f) {
                return upperIslands;
            }
            return skylands;
        }
    }
}
