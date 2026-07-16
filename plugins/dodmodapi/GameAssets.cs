
using System;
using UnityEngine;

namespace DODModAPI;

public static class GameAssets {
    public static class UI {
        public static Sprite Arrow => GetSprite("UI/gui", "Arrow");
        public static Sprite AutobuilderTab => GetSprite("UI/gui", "AutobuilderTab");
        public static Sprite AutobuilderTabSelected => GetSprite("UI/gui", "AutobuilderTabSelected");
        public static Sprite Black => GetSprite("UI/gui", "Black");
        public static Sprite Black50p => GetSprite("UI/gui", "Black50p");
        public static Sprite Black75p => GetSprite("UI/gui", "Black75p");
        public static Sprite Button => GetSprite("UI/gui", "Button");
        public static Sprite ButtonSmall => GetSprite("UI/gui", "ButtonSmall");
        public static Sprite Clock => GetSprite("UI/gui", "Clock");
        public static Sprite ClockNeedle => GetSprite("UI/gui", "ClockNeedle");
        public static Sprite Close => GetSprite("UI/gui", "close");
        public static Sprite Cross => GetSprite("UI/gui", "Cross");
        public static Sprite DropDownList => GetSprite("UI/gui", "DropDownList");
        public static Sprite DropDownListBottom => GetSprite("UI/gui", "DropDownListBottom");
        public static Sprite HelpBox => GetSprite("UI/gui", "help_box");
        public static Sprite HelpLine => GetSprite("UI/gui", "help_line");
        public static Sprite HelpPoint => GetSprite("UI/gui", "help_point");
        public static Sprite HpBarBack => GetSprite("UI/gui", "hpbar_back");
        public static Sprite HpBarFront => GetSprite("UI/gui", "hpbar_front");
        public static Sprite HpBarTransparent => GetSprite("UI/gui", "hpbar_transparent");
        public static Sprite Item => GetSprite("UI/gui", "Item");
        public static Sprite ItemCooldown => GetSprite("UI/gui", "itemCooldown");
        public static Sprite ItemHL => GetSprite("UI/gui", "ItemHL");
        public static Sprite ItemLocked => GetSprite("UI/gui", "ItemLocked");
        public static Sprite ItemNbBack => GetSprite("UI/gui", "itemNbBack");
        public static Sprite ItemOff => GetSprite("UI/gui", "itemOff");
        public static Sprite ItemOn => GetSprite("UI/gui", "itemOn");
        public static Sprite ItemsShortcut => GetSprite("UI/gui", "items_shortcut");
        public static Sprite Link0 => GetSprite("UI/gui", "link0");
        public static Sprite Link1 => GetSprite("UI/gui", "link1");
        public static Sprite Link2 => GetSprite("UI/gui", "link2");
        public static Sprite Link3 => GetSprite("UI/gui", "link3");
        public static Sprite MinimapBorder => GetSprite("UI/gui", "MinimapBorder");
        public static Sprite MinimapBorderCam => GetSprite("UI/gui", "minimapBorderCam");
        public static Sprite MinimapDeath => GetSprite("UI/gui", "minimap_death");
        public static Sprite Panel => GetSprite("UI/gui", "Panel");
        public static Sprite PanelHeader => GetSprite("UI/gui", "PanelHeader");
        public static Sprite RemoveWire => GetSprite("UI/gui", "removeWire");
        public static Sprite ShipMessage => GetSprite("UI/gui", "ShipMessage");
        public static Sprite ShipMessageTold => GetSprite("UI/gui", "ShipMessageTold");
        public static Sprite Slider => GetSprite("UI/gui", "Slider");
        public static Sprite SliderCursor => GetSprite("UI/gui", "SliderCursor");
        public static Sprite Tooltip => GetSprite("UI/gui", "Tooltip");
        public static Sprite White => GetSprite("UI/gui", "White");
    }

    public static class SoundID {
        public static readonly string jump = "jump";
        public static readonly string fall = "fall";
        public static readonly string fall_water = "fall_water";
        public static readonly string hurt = "hurt";
        public static readonly string outOfAmmo = "outOfAmmo";
        public static readonly string plasma = "plasma";
        public static readonly string shotgun = "shotgun";
        public static readonly string plasmaSnipe = "plasmaSnipe";
        public static readonly string laser = "laser";
        public static readonly string particle = "particle";
        public static readonly string particleShotgun = "particleShotgun";
        public static readonly string storm = "storm";
        public static readonly string stormLight = "stormLight";
        public static readonly string rocketFire = "rocketFire";
        public static readonly string rocketHit = "rocketHit";
        public static readonly string defensePlasma = "defensePlasma";
        public static readonly string particleTurret = "particleTurret";
        public static readonly string mine = "mine";
        public static readonly string ceilingTurret = "ceilingTurret";
        public static readonly string firefly = "firefly";
        public static readonly string hound = "hound";
        public static readonly string dweller = "dweller";
        public static readonly string dwellerBoss = "dwellerBoss";
        public static readonly string fish = "fish";
        public static readonly string birdBomb = "birdBomb";
        public static readonly string monsterBat = "monsterBat";
        public static readonly string ant = "ant";
        public static readonly string bossCrab = "bossCrab";
        public static readonly string bossCrabScream = "bossCrabScream";
        public static readonly string bossBird = "bossBird";
        public static readonly string miniBalrog = "miniBalrog";
        public static readonly string spiders = "spiders";
        public static readonly string balrog = "balrog";
        public static readonly string monsterParticleGround = "monsterParticleGround";
        public static readonly string monsterParticle = "monsterParticle";
        public static readonly string miniaturizor = "miniaturizor";
        public static readonly string rain = "rain";
        public static readonly string rocketCinematic = "rocketCinematic";
        public static readonly string rocketExplosion = "rocketExplosion";
        public static readonly string jetpack = "jetpack";
        public static readonly string waterfall = "waterfall";
        public static readonly string lava = "lava";
        public static readonly string fireForest = "fireForest";
        public static readonly string doorOpen = "doorOpen";
        public static readonly string doorClose = "doorClose";
        public static readonly string teleport = "teleport";
        public static readonly string potions = "potions";
        public static readonly string fireImpact = "fireImpact";
        public static readonly string lavaEruption = "lavaEruption";
        public static readonly string alarm = "alarm";
    }

    private static Sprite GetSprite(string sheetName, string spriteName) {
        var asset = SResources.GetSprite(sheetName, spriteName);

        var sprite = asset.Sprite;
        if (sprite is null) {
            throw new InvalidOperationException($"Sprite not found: '{sheetName}:{spriteName}'");
        }
        return sprite;
    }
}
