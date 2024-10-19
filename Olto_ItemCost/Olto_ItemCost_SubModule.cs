using System;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
//using TaleWorlds.ObjectSystem;
//using TaleWorlds.Localization;

namespace Olto_ItemCost
{
    public class Olto_ItemCost_SubModule : MBSubModuleBase
    {
        public static float cost_mod_armor;
        public static float cost_mod_weapon;
        public static float cost_mod_horse;
        public static float cost_mod_saddle;
        public static float cost_mod_goods;
        public static float cost_mod_banner;

        public static float power_mod_item;
        public static bool appearance_item;
        public static float cost_mod_appearance;
		
        public static bool change_arrows_formula;
        public static int tier_wood;

        public static float cost_mod_only_2h;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            this.loadSettings();
        }
        private void loadSettings()
        {
            Settings settings = new XmlSerializer(typeof(Settings)).Deserialize((Stream)File.OpenRead(Path.Combine(BasePath.Name, "Modules/Olto_ItemCost/settings.xml"))) as Settings;

            Olto_ItemCost_SubModule.cost_mod_armor = settings.cost_mod_armor;
            Olto_ItemCost_SubModule.cost_mod_weapon = settings.cost_mod_weapon;
            Olto_ItemCost_SubModule.cost_mod_horse = settings.cost_mod_horse;
            Olto_ItemCost_SubModule.cost_mod_saddle = settings.cost_mod_saddle;
            Olto_ItemCost_SubModule.cost_mod_goods = settings.cost_mod_goods;
            Olto_ItemCost_SubModule.cost_mod_banner = settings.cost_mod_banner;

            Olto_ItemCost_SubModule.power_mod_item = settings.power_mod_item;
            Olto_ItemCost_SubModule.appearance_item = settings.appearance_item;
            Olto_ItemCost_SubModule.cost_mod_appearance = settings.cost_mod_appearance;
			
            Olto_ItemCost_SubModule.change_arrows_formula = settings.change_arrows_formula;
            Olto_ItemCost_SubModule.tier_wood = settings.tier_wood;

            Olto_ItemCost_SubModule.cost_mod_only_2h = settings.cost_mod_only_2h;

        }
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            gameStarterObject.AddModel((GameModel)new OltoItemValueModel());
        }
    }

    [Serializable]
    public class Settings
    {
        public float cost_mod_armor;
        public float cost_mod_weapon;
        public float cost_mod_horse;
        public float cost_mod_saddle;
        public float cost_mod_goods;
        public float cost_mod_banner;
        
        public float power_mod_item;
        public bool appearance_item;
        public float cost_mod_appearance;
		
        public bool change_arrows_formula;
        public int tier_wood;

        public float cost_mod_only_2h;
    }

    internal class OltoItemValueModel : DefaultItemValueModel
    {
		private float CalculateArmorTier(ArmorComponent armorComponent)
		{
			float num = 1.2f * (float)armorComponent.HeadArmor + 1f * (float)armorComponent.BodyArmor + 1f * (float)armorComponent.LegArmor + 1f * (float)armorComponent.ArmArmor;
			if (armorComponent.Item.ItemType == ItemObject.ItemTypeEnum.LegArmor)
				num *= 1.6f;
			else if (armorComponent.Item.ItemType == ItemObject.ItemTypeEnum.HandArmor)
				num *= 1.7f;
			else if (armorComponent.Item.ItemType == ItemObject.ItemTypeEnum.HeadArmor)
				num *= 1.2f;
			else if (armorComponent.Item.ItemType == ItemObject.ItemTypeEnum.Cape)
				num *= 1.8f;
			
			return num * 0.1f - 0.4f;
		}

		private float CalculateHorseTier(HorseComponent horseComponent)
		{
			return (float)horseComponent.Speed * 0.12f + (float)horseComponent.Maneuver * 0.07f + (float)horseComponent.HitPointBonus * 0.01f + (float)horseComponent.ChargeDamage * 0.15f - 11.5f;
		}

		private float CalculateSaddleTier(SaddleComponent saddleComponent)
		{
			return 0f;
		}

		private float CalculateWeaponTier(WeaponComponent weaponComponent)
		{
			WeaponDesign weaponDesign = weaponComponent.Item?.WeaponDesign;
			if (weaponDesign != null)
			{
				float num = CalculateTierCraftedWeapon(weaponDesign);
				float num2 = CalculateTierMeleeWeapon(weaponComponent);
				return 0.6f * num2 + 0.4f * num;
			}
			return CalculateTierNonCraftedWeapon(weaponComponent);
		}

		private float CalculateTierMeleeWeapon(WeaponComponent weaponComponent)
		{
			float num = float.MinValue;
			float num2 = float.MinValue;
			for (int i = 0; i < weaponComponent.Weapons.Count; i++)
			{
				WeaponComponentData weaponComponentData = weaponComponent.Weapons[i];
				float a = (float)weaponComponentData.ThrustDamage * GetFactor(weaponComponentData.ThrustDamageType) * MathF.Pow((float)weaponComponentData.ThrustSpeed * 0.01f, 1.5f);
				float num3 = (float)weaponComponentData.SwingDamage * GetFactor(weaponComponentData.SwingDamageType) * MathF.Pow((float)weaponComponentData.SwingSpeed * 0.01f, 1.5f);
				float num4 = MathF.Max(a, num3 * 1.1f);

//not work without harmony //fix -20% cost for only 2H
				if (weaponComponentData.WeaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
					num4 *= Olto_ItemCost_SubModule.cost_mod_only_2h;

				if (weaponComponentData.WeaponClass == WeaponClass.ThrowingKnife || weaponComponentData.WeaponClass == WeaponClass.ThrowingAxe)
					num4 *= 1.2f;

				if (weaponComponentData.WeaponClass == WeaponClass.Javelin)
					num4 *= 0.6f;

				float num5 = (float)weaponComponentData.WeaponLength * 0.01f;
				float num6 = 0.06f * (num4 * (1f + num5)) - 3.5f;
				if (num6 > num2)
				{
					if (num6 >= num)
					{
						num2 = num;
						num = num6;
					}
					else
						num2 = num6;
				}
			}
//Clamp 7.5f to 10f, because in game exist playable armor with tier 8-9.1
            num = MathF.Clamp(num, -1.5f, 10f);
			if (num2 != float.MinValue)
				num2 = MathF.Clamp(num2, -1.5f, 10f);
			
			if (weaponComponent.Weapons.Count <= 1)
				return num;

			return num * MathF.Pow(1f + (num2 + 1.5f) / (num + 2.5f), 0.2f);
		}

		private float GetFactor(DamageTypes swingDamageType)
		{
			return swingDamageType switch
			{
				DamageTypes.Pierce => 1.15f,
				DamageTypes.Blunt => 1.45f,
				_ => 1f,
			};
		}

		private float CalculateTierNonCraftedWeapon(WeaponComponent weaponComponent)
		{
			switch (weaponComponent.Item?.ItemType ?? ItemObject.ItemTypeEnum.Invalid)
			{
				case ItemObject.ItemTypeEnum.Bow:
				case ItemObject.ItemTypeEnum.Crossbow:
				case ItemObject.ItemTypeEnum.Pistol:
				case ItemObject.ItemTypeEnum.Musket:
					return CalculateRangedWeaponTier(weaponComponent);
				case ItemObject.ItemTypeEnum.Arrows:
				case ItemObject.ItemTypeEnum.Bolts:
				case ItemObject.ItemTypeEnum.Bullets:
					return CalculateAmmoTier(weaponComponent);
				case ItemObject.ItemTypeEnum.Shield:
					return CalculateShieldTier(weaponComponent);
				default:
					return 0f;
			}
		}

		private float CalculateRangedWeaponTier(WeaponComponent weaponComponent)
		{
			WeaponComponentData weaponComponentData = weaponComponent.Weapons[0];
			ItemObject.ItemTypeEnum num = weaponComponent.Item?.ItemType ?? ItemObject.ItemTypeEnum.Invalid;
			float num2 = 0f;
			if (num == ItemObject.ItemTypeEnum.Crossbow)
				num2 += -1.5f;

			if (weaponComponentData.ItemUsage.Contains("light"))
				num2 += 1.25f;

			if (!weaponComponent.PrimaryWeapon.ItemUsage.Contains("long_bow") && !weaponComponent.PrimaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.CantReloadOnHorseback))
				num2 += 0.5f;
			
			int thrustDamage = weaponComponentData.ThrustDamage;
			int missileSpeed = weaponComponentData.MissileSpeed;
			int accuracy = weaponComponentData.Accuracy;

			return (float)thrustDamage * 0.1f + (float)missileSpeed * 0.02f + (float)accuracy * 0.05f - 9.25f + num2;
		}

		private float CalculateShieldTier(WeaponComponent weaponComponent)
		{
			WeaponComponentData weaponComponentData = weaponComponent.Weapons[0];
			return ((float)weaponComponentData.MaxDataValue + 3f * (float)weaponComponentData.BodyArmor + (float)weaponComponentData.ThrustSpeed) / (6f + weaponComponent.Item.Weight) * 0.13f - 3f;
		}

		private float CalculateAmmoTier(WeaponComponent weaponComponent)
		{
			WeaponComponentData weaponComponentData = weaponComponent.Weapons[0];
			int missileDamage = weaponComponentData.MissileDamage;
			int num = 0;

//fix arrows tier // vanila 1 and 0.1f (+tier = dmg or count 10) // mod 0.5f and 0.167f (+tier = dmg2 or dmg+count3 or count6)
			if (Olto_ItemCost_SubModule.change_arrows_formula == false)
			{
				MathF.Max(0, weaponComponentData.MaxDataValue - 20);
				return (float)missileDamage + (float)num * 0.1f;
			}
			else
			{
				num = MathF.Max(0, weaponComponentData.MaxDataValue - 21);
				return (float)missileDamage * 0.5f + (float)num * 0.167f;
			};
		}

			private float CalculateTierCraftedWeapon(WeaponDesign craftingData)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			WeaponDesignElement[] usedPieces = craftingData.UsedPieces;
			foreach (WeaponDesignElement weaponDesignElement in usedPieces)
			{
				if (!weaponDesignElement.CraftingPiece.IsValid)
					continue;
				
				num += weaponDesignElement.CraftingPiece.PieceTier;
				num2++;
				foreach (var item3 in weaponDesignElement.CraftingPiece.MaterialsUsed)
				{
					CraftingMaterials item = item3.Item1;
					int item2 = item3.Item2;
					int num5 = item switch
					{
						CraftingMaterials.Iron6 => 6,
						CraftingMaterials.Iron5 => 5,
						CraftingMaterials.Iron4 => 4,
						CraftingMaterials.Iron3 => 3,
						CraftingMaterials.Iron2 => 2,
						CraftingMaterials.Iron1 => 1,
//fix wood tier for craft // -1 vanila //mod 1 (=Iron1)
						CraftingMaterials.Wood => Olto_ItemCost_SubModule.tier_wood,
						_ => -1,
					};
					if (num5 >= 0)
					{
						num3 += item2 * num5;
						num4 += item2;
					}
				}
			}

			if (num4 > 0 && num2 > 0)
				return 0.4f * (1.25f * (float)num / (float)num2) + 0.6f * ((float)num3 * 1.3f / ((float)num4 + 0.6f) - 1.3f);
			
			if (num2 > 0)
				return (float)num / (float)num2;
			
			return 0.1f;
		}

		public override int CalculateValue(ItemObject item)
		{
			float num = 1f;
			if (item.ItemComponent != null) num = GetEquipmentValueFromTier(item.Tierf);
			float num2 = 1f;
			if (item.ItemComponent is ArmorComponent)
				num2 = Olto_ItemCost_SubModule.cost_mod_armor * ((item.ItemType == ItemObject.ItemTypeEnum.BodyArmor) ? 120 : ((item.ItemType == ItemObject.ItemTypeEnum.HandArmor) ? 120 : ((item.ItemType == ItemObject.ItemTypeEnum.LegArmor) ? 120 : 100)));
			else if (item.ItemComponent is WeaponComponent)
				num2 = Olto_ItemCost_SubModule.cost_mod_weapon * 100f;
			else if (item.ItemComponent is HorseComponent)
				num2 = Olto_ItemCost_SubModule.cost_mod_armor * 100f;
			else if (item.ItemComponent is SaddleComponent)
				num2 = Olto_ItemCost_SubModule.cost_mod_saddle * 100f;
			
			else if (item.ItemComponent is TradeItemComponent)
				num2 = Olto_ItemCost_SubModule.cost_mod_goods * 100f;
			
			else if (item.ItemComponent is BannerComponent)
				num2 = Olto_ItemCost_SubModule.cost_mod_banner * 100f;
			
//cost ignore appearance (apply all default appearance (0.5f))
			if (Olto_ItemCost_SubModule.appearance_item == false)
				return (int)(num2 * num * (1f + 0.2f * (item.Appearance - 1f)) + 100f * MathF.Max(0f, item.Appearance - 1f));
			else
				return (int)(num2 * num * (1f + 0.2f * (Olto_ItemCost_SubModule.cost_mod_appearance - 1f)) + 100f * MathF.Max(0f, Olto_ItemCost_SubModule.cost_mod_appearance - 1f));

		}

		private float GetWeaponPriceFactor(ItemObject item)
		{
			return 100f;
		}

		public override float GetEquipmentValueFromTier(float itemTierf)
		{
//fix item cost mult vanila Pow 2.75f // tier -1f, 7.5f (mod min to 1.5f)
//          return MathF.Pow(2.75f, MathF.Clamp(itemTierf, -1f, 7.5f));
			return MathF.Pow(Olto_ItemCost_SubModule.power_mod_item, MathF.Clamp(itemTierf, -1.5f, 10f));
		}

		public override float CalculateTier(ItemObject item)
		{
			if (item.ItemComponent is ArmorComponent)
				return CalculateArmorTier(item.ItemComponent as ArmorComponent);
			
			if (item.ItemComponent is BannerComponent)
				return CalculateBannerTier(item, item.ItemComponent as BannerComponent);
			
			if (item.ItemComponent is WeaponComponent)
				return CalculateWeaponTier(item.ItemComponent as WeaponComponent);
			
			if (item.ItemComponent is HorseComponent)
				return CalculateHorseTier(item.ItemComponent as HorseComponent);
			
			if (item.ItemComponent is SaddleComponent)
				return CalculateSaddleTier(item.ItemComponent as SaddleComponent);
			
			return 0f;
		}

		private float CalculateBannerTier(ItemObject item, BannerComponent bannerComponent)
		{
			return GetBannerItemCultureBonus(item.Culture) + GetBannerItemLevelBonus(bannerComponent.BannerLevel);
		}

		private float GetBannerItemCultureBonus(BasicCultureObject culture)
		{
			if (culture == null) return 0f;
			return 1f;
		}

		private float GetBannerItemLevelBonus(int bannerLevel)
		{
			return bannerLevel switch
			{
				3 => 5f,
				2 => 3f,
				_ => 1f,
			};
		}

    }
}