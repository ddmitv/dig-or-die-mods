

-- groupId: autobuilder group ; list_autobuilders: autobuilders that will have those recipse ; ...: list of CRecipe
function CreateRecipesGroup( groupId, list_autobuilders, ... )
	local obj = Create("CRecipesGroup");
	obj.LuaInit(groupId, list_autobuilders, { ... } );
	return obj;
end

-- idOut: crafted item id ; nbOut: nb of crafted items ; idInt1: first component item ; (...); isUpgrade: is the first component an item to be upgraded (removed)?
function CreateRecipe(idOut, nbOut, idInt1, nbIn1, idIn2, nbIn2, idIn3, nbIn3, isUpgrade)
	local recipe = Create("CRecipe");
	recipe.InitLua(idOut, nbOut, idInt1, nbIn1, idIn2, nbIn2, idIn3, nbIn3, isUpgrade)
	return recipe;
end


recipesMK1 = CreateRecipesGroup( "MK I", { "autoBuilderMK1", "autoBuilderMK2", "autoBuilderMK3", "autoBuilderMK4", "autoBuilderMK5", "autoBuilderUltimate" }
	, CreateRecipe( "potionHp", 1, "flowerBlue", 3, "dogHorn", 1 )

	, CreateRecipe( "gunRifle", 1, "iron", 2, "waterLight", 2 )
	, CreateRecipe( "gunShotgun", 1, "iron", 5, "waterLight", 5 )
	
	, CreateRecipe( "platform", 3, "wood", 1 ) -- plant		
	, CreateRecipe( "wallWood", 2, "wood", 1 ) -- plant
	
	, CreateRecipe( "turret360", 1, "iron", 5, "lightGem", 1 )--  // loot from firefly1
	
	, CreateRecipe( "autoBuilderMK1", 1, "iron", 10, "lightGem", 3 ) --// loot from firefly1
	, CreateRecipe( "autoBuilderMK2", 1, "autoBuilderMK1", 1, "lightGem", 3, nil, 0, true ) -- // loot from firefly1
	
	, CreateRecipe( "iron", 1, "metalScrap", 2 )
)

recipesMK2 = CreateRecipesGroup( "MK II", { "autoBuilderMK2", "autoBuilderMK3", "autoBuilderMK4", "autoBuilderMK5", "autoBuilderUltimate"}

	, CreateRecipe( "miniaturizorMK2", 1, "miniaturizorMK1", 1, "lightGem", 2, null, 0, true ) -- OK - loot from firefly1
	, CreateRecipe( "potionHpRegen", 1, "flowerBlue", 3, "bloodyFlesh1", 7 )
	, CreateRecipe( "potionPheromones", 1, "tree", 3, "bloodyFlesh1", 7 )	
	, CreateRecipe( "armorMk1", 1, "iron", 5, "moleShell", 3 ) -- OK - loot from dweller2
	, CreateRecipe( "flashLight", 1, "iron", 3, "waterLight", 4 ) -- OK		
	, CreateRecipe( "minimapper", 1, "iron", 3, "bat2Sonar", 2, "lightGem", 1 ) -- OK		
	, CreateRecipe( "effeilGlasses", 1, "iron", 3, "bat2Sonar", 2, "bloodyFlesh1", 10 ) --  loot from bat2

	, CreateRecipe( "gunMachineGun", 1, "iron", 15, "coal", 10, "lightGem", 2 )--, "bloodyFlesh1", 10 ) // OK /// ??????????????????????
	
	, CreateRecipe( "wallConcrete", 2, "dirt", 1, "rock", 1 ) -- OK
	, CreateRecipe( "wallIronSupport", 2, "iron", 1 ) -- OK
	, CreateRecipe( "backwall", 3, "dirt", 1, "rock", 1 ) -- OK

	, CreateRecipe( "turretGatling", 1, "iron", 8, "lightGem", 1, "bloodyFlesh1", 3 )  -- OK - loot from firefly1
	, CreateRecipe( "turretReparator", 1, "iron", 8, "waterCoral", 3, "fish2Regen", 2 ) -- loot from fish2

	, CreateRecipe( "light", 2, "wood", 1, "waterLight", 1 ) -- OK		
	, CreateRecipe( "autoBuilderMK3", 1, "autoBuilderMK2", 1, "fernRed", 5, "bloodyFlesh1", 15, true ) -- OK 
);

-- MK3: monsters 2 & 3, rock layer, chasm, mountain (objective: close islands & ocean surface)
recipesMK3 = CreateRecipesGroup( "MK III", { "autoBuilderMK3", "autoBuilderMK4", "autoBuilderMK5", "autoBuilderUltimate"}

	, CreateRecipe( "miniaturizorMK3", 1, "miniaturizorMK2", 1, "energyGem", 3, "waterCoral", 5, true ) --/ ?????????????????????? 
	, CreateRecipe( "potionHpBig", 1, "fernRed", 3, "dogHorn3", 1 ) -- loot from hound3 --/ ?????????????????????? 
	, CreateRecipe( "potionCritics", 1, "treePine", 3, "bloodyFlesh2", 7 )
	, CreateRecipe( "armorMk2", 1, "armorMk1", 1, "iron", 10, "moleShellBlack", 3, true ) --  loot from dweller3
	, CreateRecipe( "waterDetector", 1, "aluminium", 3, "waterBush", 10, "bloodyFlesh2", 10 ) --/ ?????????????????????? 
	, CreateRecipe( "metalDetector", 1, "iron", 5, "gold", 2, "bossMadCrabSonar", 1 ) -- loot from bossMadCrab
	, CreateRecipe( "drone", 1, "aluminium", 5, "gold", 2 ) --/ ?????????????????????? 

	, CreateRecipe( "gunLaser", 1, "aluminium", 10, "flowerWhite", 7, "energyGem", 5 ) -- loot from firefly2
	, CreateRecipe( "gunSnipe", 1, "aluminium", 10, "coal", 20, "bloodyFlesh1", 10 ) --  
	, CreateRecipe( "gunRocket", 1, "aluminium", 10, "blackGrass", 10, "bloodyFlesh1", 15 ) -- 
	
	, CreateRecipe( "wallReinforced", 2, "iron", 1, "dirt", 1, "rock", 1 ) -- 
	, CreateRecipe( "wallDoor", 1, "iron", 1, "dirt", 1, "rock", 1 ) -- 
	, CreateRecipe( "platformSteel", 4, "iron", 1, "coal", 1 ) -- 		
	, CreateRecipe( "generatorWater", 1, "iron", 5, "copper", 1, "gold", 1 ) -- 
	, CreateRecipe( "waterPump", 1, "iron", 5, "copper", 1, "gold", 1 ) -- 
	
	, CreateRecipe( "turretHeavy", 1, "aluminium", 8, "coal", 7 ) --
	, CreateRecipe( "turretReparatorMK2", 1, "aluminium", 8, "flowerWhite", 3, "fish3Regen", 2 ) -- loot from fish
	, CreateRecipe( "turretMine", 1, "aluminium", 2, "coal", 5, "bloodyFlesh2", 10 ) --/ ??????????????????????
	, CreateRecipe( "turretSpikes", 1, "aluminium", 3, "bloodyFlesh2", 10 ) --/ ?????????????????????? ###################################################
	
	, CreateRecipe( "lightSticky", 3, "iron", 1, "waterLight", 1 ) -- 
	, CreateRecipe( "electricWire", 10, "copper", 1 ) -- 
	, CreateRecipe( "generatorSun", 1, "iron", 2, "copper", 1, "coal", 5 ) --
	, CreateRecipe( "lightSun", 1, "iron", 2, "copper", 1, "flowerWhite", 2 ) --
	, CreateRecipe( "teleport", 1, "iron", 15, "copper", 1, "gold", 3 ) --
	, CreateRecipe( "elecSwitch", 2, "iron", 1, "copper", 1 )		
	, CreateRecipe( "autoBuilderMK4", 1, "autoBuilderMK3", 1, "rockGaz", 2, "fish3Regen", 5, true ) -- loot from fish3

	, CreateRecipe( "coal", 1, "wood", 10 )

	, CreateRecipe( "betterPotionHpRegen", 1, "bloodyFlesh2", 5 )
);

-- MK4: monsters 3 & 4, ocean, high islands, crystal layer (objective:skylands, black crystals)
recipesMK4 = CreateRecipesGroup( "MK IV", { "autoBuilderMK4", "autoBuilderMK5", "autoBuilderUltimate"}
	, CreateRecipe( "miniaturizorMK4", 1, "miniaturizorMK3", 1, "blackMushroom", 5, "bloodyFlesh2", 10, true )
	, CreateRecipe( "flashLightMK2", 1, "flashLight", 1, "aluminium", 1, "crystalLight", 5, true )--/ ??????????????????????
	, CreateRecipe( "potionArmor", 1, "treeWater", 3, "bloodyFlesh2", 7 )
	, CreateRecipe( "potionInvisibility", 1, "treeSky", 3, "bloodyFlesh3", 7 )	
	, CreateRecipe( "waterBreather", 1, "aluminium", 2, "sharkSkin", 2, "bloodyFlesh2", 10 )
	, CreateRecipe( "jetpack", 1, "aluminium", 5, "masterGem", 1 ) -- loot from bossFirefly (Energy Master Gem)
	, CreateRecipe( "armorMk3", 1, "armorMk2", 1, "iron", 10, "antShell", 3, true ) -- loot from antClose4 (ant shell)
	, CreateRecipe( "invisibilityDevice", 1, "aluminium", 5, "uranium", 1, "bloodyFlesh2", 20 ) --/ ??????????????????????
	-- , CreateRecipe( "droneCombat", 1, "drone", 1, "aluminium", 5, "gold", 6, "uranium", 2 ) --/ ?????????????????????? 
	, CreateRecipe( "droneCombat", 1, "drone", 1, "aluminium", 5, "uranium", 3 ) --/ ?????????????????????? 

	, CreateRecipe( "gunMegaSnipe", 1, "aluminium", 10, "coal", 30, "bossMadCrabMaterial", 1 ) -- loot from bossMadCrab
	, CreateRecipe( "gunZF0", 1, "aluminium", 10, "bat3Sonar", 4, "crystalBlack", 2 ) -- loot from bat3
	, CreateRecipe( "gunLaserGatling", 1, "aluminium", 10, "uranium", 1, "darkGem", 5 ) --  loot from firefly3
	, CreateRecipe( "gunStorm", 1, "aluminium", 10, "skyBush", 5, "bloodyFlesh2", 10 ) -- 
	, CreateRecipe( "gunGrenadeLaunch", 1, "aluminium", 10, "unstableGemResidue", 4 ) --/ ??????????????????????
				
	, CreateRecipe( "wallComposite", 3, "granit", 3, "aluminium", 1, "coal", 1 ) -- 
	, CreateRecipe( "wallCompositeDoor", 1, "granit", 3, "aluminium", 1, "coal", 1 ) -- 
	, CreateRecipe( "wallCompositeSupport", 4, "crystal", 2, "aluminium", 1, "coal", 1 ) -- 
	, CreateRecipe( "wallCompositeLight", 3, "crystal", 1, "rockFlying", 2, "coal", 1 ) -- 

	, CreateRecipe( "turretCeiling", 1, "aluminium", 5, "skyBush", 5, "bat3Sonar", 2 )  -- loot from bat3 ###################################################
	, CreateRecipe( "turretLaser", 1, "aluminium", 5, "crystalLight", 6, "darkGem", 2 ) -- loot from firefly3
	, CreateRecipe( "turretTesla", 1, "aluminium", 3, "gold", 5 ) --/ ??????????????????????

	, CreateRecipe( "elecSwitchPush", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecSwitchRelay", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecCross", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecSignal", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecClock", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecToggle", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecDelay", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecWaterSensor", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecProximitySensor", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecDistanceSensor", 2, "iron", 1, "copper", 1 )	
	, CreateRecipe( "elecAND", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecOR", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecXOR", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecNOT", 2, "iron", 1, "copper", 1 )
	, CreateRecipe( "elecLight", 2, "iron", 1, "copper", 1, "waterLight", 1 )
	, CreateRecipe( "elecAlarm", 2, "iron", 1, "copper", 1, "waterLight", 1 )	
	, CreateRecipe( "autoBuilderMK5", 1, "autoBuilderMK4", 1, "crystalBlack", 4, "rockGaz", 30, true )  --/ ??????????????????????
);

-- MK5: monsters 5, moria, volcano, organic island
recipesMK5 = CreateRecipesGroup( "MK V", { "autoBuilderMK5", "autoBuilderUltimate"}
	, CreateRecipe( "miniaturizorMK5", 1, "miniaturizorMK4", 1, "crystalLight", 10, "uranium", 5, true ) --/ ??????????????????????
	, CreateRecipe( "potionHpMega", 1, "lavaPlant", 1, "lootMiniBalrog", 1 ) -- loot from hound3 --/ ?????????????????????? 
	, CreateRecipe( "potionSpeed", 1, "treeGranit", 3, "bloodyFlesh3", 7 ) --/ loot from Mini balrogs ??????????????????????
	, CreateRecipe( "defenseShield", 1, "titanium", 10, "lootDwellerLord", 1, "sapphire", 1 ) --/ ?????????????????????? Loot from Dweller Lord
	-- , CreateRecipe( "droneWar", 1, "droneCombat", 1, "titanium", 5, "gold", 6, "thorium", 4 ) --/ ?????????????????????? 
	, CreateRecipe( "droneWar", 1, "droneCombat", 1, "titanium", 5, "thorium", 5 ) --/ ?????????????????????? 

	, CreateRecipe( "gunParticlesShotgun", 1, "titanium", 10, "lootParticleGround", 5, "bushGranit", 10 ) -- loot from particles ground --/ ?????????????????????? 
	, CreateRecipe( "gunParticlesSniper", 1, "titanium", 10, "lightonium", 15, "lootParticleBirds", 5 ) -- loot from particles birds -/ ??????????????????????
	, CreateRecipe( "gunFlamethrower", 1, "titanium", 10, "sulfur", 20, "lootLavaSpider", 5 ) -- loot from large lava spider ??????????????????????

	, CreateRecipe( "turretFlame", 1, "titanium", 5, "lavaFlower", 5, "lootLavaBat", 2 ) --/ loot from large lava bat ??????????????????????
	, CreateRecipe( "turretParticles", 1, "titanium", 5, "woodGranit", 10, "sapphire", 1 )   --/ ??????????????????????
	, CreateRecipe( "explosive", 1, "titanium", 5, "sulfur", 30, "unstableGemResidue", 6 )   --/ ??????????????????????

	, CreateRecipe( "reactor", 1, "titanium", 10, "thorium", 10, "lootLargeParticleBirds", 7 ) -- loot from Large Particle Birds -/ ??????????????????????

	, CreateRecipe( "rocketTop", 1, "titanium", 30, "diamonds", 1, "lootBalrog", 1 ) --  ????????????????????????
	, CreateRecipe( "rocketTank", 1, "titanium", 10, "rockGaz", 10, "bloodyFlesh3", 10 ) -- ????????????????????????
	, CreateRecipe( "rocketEngine", 1, "titanium", 30, "organicRockHeart", 3, "sapphire", 5 ) -- ????????????????????????

	-- Needs about 20 lava blocks to make something interesting ?
	, CreateRecipe( "iron", 1, "lava", 3, "tree", 3, "waterBush", 3 )
	, CreateRecipe( "copper", 1, "lava", 3, "bush", 3, "waterCoral", 3 )
	, CreateRecipe( "gold", 1, "lava", 5, "bushGranit", 5, "waterLight", 10 )
	, CreateRecipe( "aluminium", 1, "lava", 3, "flowerBlue", 5, "treeWater", 3 )
	, CreateRecipe( "rockGaz", 1, "lava", 5, "treeSky", 5, "flowerWhite", 5 )	
	, CreateRecipe( "uranium", 1, "lava", 5, "flowerWhite", 5, "tree", 5 )	
	, CreateRecipe( "titanium", 1, "lava", 3, "blackGrass", 3, "treePine", 3 )
	, CreateRecipe( "thorium", 1, "lava", 3, "fernRed", 3, "treeGranit", 3 )
	, CreateRecipe( "sulfur", 1, "lava", 2, "blackMushroom", 2, "lavaFlower", 2 )
	, CreateRecipe( "sapphire", 1, "lava", 20, "lavaPlant", 10, "treeGranit", 10 )
	
	, CreateRecipe( "rock", 1, "lava", 1, "woodwater", 1 )
	, CreateRecipe( "granit", 1, "lava", 1, "woodGranit", 1 )
	, CreateRecipe( "rockFlying", 1, "lava", 1, "woodSky", 1 )
	
	, CreateRecipe( "dirt", 1, "deadPlant", 10 )

	, CreateRecipe( "flashLightMK3", 1, "flashLightMK2", 1, "titanium", 5, "masterGem", 1, true )
	, CreateRecipe( "miniaturizorMK6", 1, "miniaturizorMK5", 1, "reactor", 1, "lootBalrog", 1, true )
	, CreateRecipe( "defenseShieldMK2", 1, "defenseShield", 1, "diamonds", 1, nil, nil, true)
	, CreateRecipe( "waterBreatherMK2", 1, "waterBreather", 2, "coal", 100, "reactor", 1, true )
	, CreateRecipe( "jetpackMK2", 1, "aluminium", 5, "masterGem", 1 )
);

recipesUltimate = CreateRecipesGroup( "ULTIMATE", { "autoBuilderUltimate"}
	, CreateRecipe( "miniaturizorUltimate", 1 )
	, CreateRecipe( "ultimateJetpack", 1 )
	, CreateRecipe( "ultimateBrush", 1 )
	, CreateRecipe( "armorUltimate", 1 )
	, CreateRecipe( "ultimateRebreather", 1 )
	, CreateRecipe( "gunUltimateGrenadeLauncher", 1 )
	, CreateRecipe( "gunUltimateParticlesGatling", 1 )
	, CreateRecipe( "wallUltimate", 1 )
	, CreateRecipe( "autoBuilderUltimate", 1 )
	, CreateRecipe( "ultimateWaterPistol", 1 )
	, CreateRecipe( "ultimateLavaPistol", 1 )
	, CreateRecipe( "ultimateSpongePistol", 1 )
	, CreateRecipe( "ultimateTotoroGun", 1 )
	, CreateRecipe( "ultimateMonstersGun", 1 )
	, CreateRecipe( "metalScrap", 1 )

	, CreateRecipe( "antiGravityWall", 1 )
	, CreateRecipe( "turretReparatorMK3", 1 )
	, CreateRecipe( "turretParticlesMK2", 1 )
	, CreateRecipe( "turretTeslaMK2", 1 )
	, CreateRecipe( "blueLightSticky", 1 )
	, CreateRecipe( "redLightSticky", 1 )
	, CreateRecipe( "greenLightSticky", 1 )
	, CreateRecipe( "collector", 1 )
	, CreateRecipe( "basaltCollector", 1 )
	, CreateRecipe( "turretLaser360", 1 )
	, CreateRecipe( "gunMeltdown", 1 )
);

list_recipesgroups = { recipesMK1, recipesMK2, recipesMK3, recipesMK4, recipesMK5, recipesUltimate }


-- /*
-- fernRed // peu de dispos (bien � cultiver), pas utilis� en end game
-- blackGrass // dur � trouver (bien � cultiver), utilis� une fois	
-- blackMushroom	 // dur � trouver (bien � cultiver), utilis� une fois
-- bushGranit // peu de dispo (bien � cultiver), utilis� qu'une fois
-- lavaPlant // difficile � trouver, jamais utilis�
-- 
-- flowerBlue // facile � trouver, utilis�
-- bush // facile � trouver, jamais utilis�
-- lavaFlower // difficile � trouver, peu utilsi�
-- 
-- tree // moyen � trouver, jamais utilis�		
-- treePine // moyen � trouver, jamais utilis�
-- treeWater // moyen � trouver, jamais utilis�
-- treeGranit // moyen � trouver, jamais utilis�
-- 
-- waterCoral // facile � trouver, pas utilis� en end game			
-- waterLight // facile � trouver, utilis�
-- woodwater // facile � trouver, jamais utilis�
-- 
-- waterBush // facile � trouver, utilis� une fois
-- skyBush // facile � trouver, pas utilis� en end game
-- flowerWhite // facile � trouver, utilis�
-- 
--  * 
--  * 
-- woodGranit // moyen � trouver, utilis�
-- woodSky	// trop facile � trouver
-- treeSky	// trop facile � trouver			
-- wood // trop utilis�
-- 
-- */