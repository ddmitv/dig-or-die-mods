
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
        public const string jump = "jump";
        public const string fall = "fall";
        public const string fall_water = "fall_water";
        public const string hurt = "hurt";
        public const string outOfAmmo = "outOfAmmo";
        public const string plasma = "plasma";
        public const string shotgun = "shotgun";
        public const string plasmaSnipe = "plasmaSnipe";
        public const string laser = "laser";
        public const string particle = "particle";
        public const string particleShotgun = "particleShotgun";
        public const string storm = "storm";
        public const string stormLight = "stormLight";
        public const string rocketFire = "rocketFire";
        public const string rocketHit = "rocketHit";
        public const string defensePlasma = "defensePlasma";
        public const string particleTurret = "particleTurret";
        public const string mine = "mine";
        public const string ceilingTurret = "ceilingTurret";
        public const string firefly = "firefly";
        public const string hound = "hound";
        public const string dweller = "dweller";
        public const string dwellerBoss = "dwellerBoss";
        public const string fish = "fish";
        public const string birdBomb = "birdBomb";
        public const string monsterBat = "monsterBat";
        public const string ant = "ant";
        public const string bossCrab = "bossCrab";
        public const string bossCrabScream = "bossCrabScream";
        public const string bossBird = "bossBird";
        public const string miniBalrog = "miniBalrog";
        public const string spiders = "spiders";
        public const string balrog = "balrog";
        public const string monsterParticleGround = "monsterParticleGround";
        public const string monsterParticle = "monsterParticle";
        public const string miniaturizor = "miniaturizor";
        public const string rain = "rain";
        public const string rocketCinematic = "rocketCinematic";
        public const string rocketExplosion = "rocketExplosion";
        public const string jetpack = "jetpack";
        public const string waterfall = "waterfall";
        public const string lava = "lava";
        public const string fireForest = "fireForest";
        public const string doorOpen = "doorOpen";
        public const string doorClose = "doorClose";
        public const string teleport = "teleport";
        public const string potions = "potions";
        public const string fireImpact = "fireImpact";
        public const string lavaEruption = "lavaEruption";
        public const string alarm = "alarm";
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
