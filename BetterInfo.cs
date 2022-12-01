using BepInEx;
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Runtime.Serialization;
//using System.Runtime.InteropServices;

namespace BetterInfoNS
{

	[BepInPlugin("BetterInfo", "BetterInfo", "1.3.2")]
	public class BetterInfoPlugin : BaseUnityPlugin
	{
		public static BepInEx.Logging.ManualLogSource L;
		public static Harmony HarmonyInstance;
		
		
		private void Awake()
		{
			L = Logger;
						
			try
			{
				HarmonyInstance = new Harmony("BetterInfoPlugin");
				HarmonyInstance.PatchAll(typeof(BetterInfoPlugin));
			}
			catch(Exception e)
			{
				Log("Patching failed: " + e.Message);
			}
		}
		
		public static void Log(string s) => L.LogInfo($"{DateTime.Now.ToString("H:mm:ss")}: {s}");
		
		[HarmonyPatch(typeof(Combatable), "UpdateCard")]
		[HarmonyPrefix]
		private static void Combatable__UpdateCard_Prefix(out string __state, string ___descriptionOverride)
		{
			__state = ___descriptionOverride;
		}
		[HarmonyPatch(typeof(Combatable), "UpdateCard")]
		[HarmonyPostfix]
		private static void Combatable__UpdateCard_Postfix(ref Combatable __instance, ref string __state, ref string ___descriptionOverride)
		{
			if (!string.IsNullOrEmpty(__state))  
			{
				//Log("Combatable__UpdateCard_PostfixDoesNothing");
				___descriptionOverride = __state;
			}
			else
			{
				//Log("Combatable__UpdateCard_PostfixDoesStuff");
				string text = "";
				if (__instance.MyGameCard != null && !__instance.MyGameCard.IsDemoCard)
				{
					text = text + SokLoc.Translate("label_health_info", LocParam.Create("health", __instance.HealthPoints.ToString()), LocParam.Create("maxhealth", __instance.ProcessedCombatStats.MaxHealth.ToString())) + "\n";
				}
				int num = Mathf.RoundToInt(__instance.RealBaseCombatStats.CombatLevel);
				int num2 = Mathf.RoundToInt(__instance.ProcessedCombatStats.CombatLevel);
				if (num2 != num)
				{
					text = text + SokLoc.Translate("label_base_combatlevel", LocParam.Create("level", num.ToString())) + "\n";
					text += SokLoc.Translate("label_total_combatlevel", LocParam.Create("level", num2.ToString()));
				}
				else
				{
					text += SokLoc.Translate("label_combatlevel", LocParam.Create("level", num2.ToString()));
				}
				string text2 = __instance.ProcessedCombatStats.SummarizeSpecialHits();
				if (text2.Length > 0)
				{
					text = text + "\n\n" + text2;
				}
				___descriptionOverride += text;
				
				if (!__instance.MyGameCard.IsDemoCard)
				{
					___descriptionOverride += __instance.GetCombatableDescriptionAdvanced();


					if (__instance is Mob mob)
					{
						___descriptionOverride += "\n\n" + BetterInfoPlugin.GetSummaryFromAllCards(mob.Drops, "label_can_drop");
					}
				}
			}
		}
		
		[HarmonyPatch(typeof(Combatable), "OnEquipItem")]
		[HarmonyPostfix]
		private static void Combatable__OnEquipItem_Postfix(ref CardData __instance, ref string ___descriptionOverride)
		{
			___descriptionOverride = "";
		}
		[HarmonyPatch(typeof(Combatable), "Damage")]
		[HarmonyPostfix]
		private static void Combatable__Damage_Postfix(ref string ___descriptionOverride)
		{
			___descriptionOverride = "";
		}
		[HarmonyPatch(typeof(Combatable), "ExitConflict")]
		[HarmonyPostfix]
		private static void Combatable__ExitConflict_Postfix(ref string ___descriptionOverride)
		{
			___descriptionOverride = "";
		}
		[HarmonyPatch(typeof(Combatable), "GetDamage")]
		[HarmonyPostfix]
		private static void Combatable__GetDamage_Postfix(ref string ___descriptionOverride)
		{
			___descriptionOverride = "";
		}
		
		[HarmonyPatch(typeof(CardData), "OnUnequipItem")]
		[HarmonyPostfix]
		private static void CardData__OnUnequipItem_Postfix(ref CardData __instance, ref string ___descriptionOverride)
		{
			if (__instance is Combatable)
				___descriptionOverride = "";
		}
		
		
		[HarmonyPatch(typeof(Combatable), "GetCombatableDescription")]
		[HarmonyPrefix]
		private static bool Combatable__GetCombatableDescription_Prefix(ref Combatable __instance, ref string __result)
		{
			__result = "";
			return false;
		}
		
		[HarmonyPatch(typeof(Combatable), "GetCombatableDescriptionAdvanced")]
		[HarmonyPostfix]
		private static void Combatable__GetCombatableDescriptionAdvanced_Postfix(ref Combatable __instance, ref string __result)
		{
		//Log("Combatable__GetCombatableDescriptionAdvanced_Postfix");
			string text = SokLoc.Translate("label_combat_speed");
			string text2 = SokLoc.Translate("label_hit_chance");
			string text3 = SokLoc.Translate("label_damage");
			string text4 = SokLoc.Translate("label_defence");
			
			float baseAttackDamageFromEnum1 = CombatStats.GetAttackDamageFromEnum(__instance.ProcessedCombatStats.AttackDamage);
			float baseAttackDamageFromEnum2 = CombatStats.GetAttackDamageFromEnum(CombatStats.IncrementEnum(__instance.ProcessedCombatStats.AttackDamage, 1));
			float baseAttackDamageFromEnum = Mathf.RoundToInt( baseAttackDamageFromEnum1 * 0.5f + baseAttackDamageFromEnum2 * 0.5f - 0.5f);
			float calculatedDmg = 0f;
			float num = 0f;
			float hitChanceFromEnum = CombatStats.GetHitChanceFromEnum(__instance.ProcessedCombatStats.HitChance);
			float attackTimeFromEnum = CombatStats.GetAttackTimeFromEnum(__instance.ProcessedCombatStats.AttackSpeed);
			
			foreach (SpecialHit specialHit in __instance.ProcessedCombatStats.SpecialHits)
			{
				num += specialHit.Chance;
				float specialDmg = 0f;
				if (specialHit.Target == SpecialHitTarget.Target || specialHit.Target == SpecialHitTarget.RandomEnemy || specialHit.Target == SpecialHitTarget.AllEnemy)
				{
					switch (specialHit.HitType)
					{
						case SpecialHitType.Poison:
							specialDmg += (specialHit.Chance / 100f) * (baseAttackDamageFromEnum + 0.1f);
							break;
						case SpecialHitType.Crit:
							specialDmg += (specialHit.Chance / 100f) * (baseAttackDamageFromEnum * 2f);
							break;
						case SpecialHitType.Bleeding:
							specialDmg += (specialHit.Chance / 100f) * (baseAttackDamageFromEnum + 2.5f);
							break;
						case SpecialHitType.Stun:
						case SpecialHitType.LifeSteal:
						case SpecialHitType.Damage:
							specialDmg += (specialHit.Chance / 100f) * (baseAttackDamageFromEnum);
							break;
					}
				}
				
				if (specialHit.Target == SpecialHitTarget.AllEnemy)
				{
					specialDmg *= 2.5f;
				}
				
				if (specialHit.Target == SpecialHitTarget.Self && specialHit.HitType == SpecialHitType.Frenzy)
				{
					float possibl = Mathf.Clamp((10f / attackTimeFromEnum) * (specialHit.Chance / 100f), 0, 1);
					attackTimeFromEnum = (1 - possibl) * attackTimeFromEnum + possibl * CombatStats.GetAttackTimeFromEnum(CombatStats.IncrementEnum(__instance.ProcessedCombatStats.AttackSpeed, 1));
				}
				
				calculatedDmg += specialDmg;
			}
			calculatedDmg += baseAttackDamageFromEnum * (100f - num) / 100f;
			
			float defenceFromEnum = Mathf.RoundToInt((float)CombatStats.GetDefenceFromEnum(__instance.ProcessedCombatStats.Defence) * 0.5f);
			
			string text5 = __instance.ProcessedCombatStats.AttackSpeed.TranslateEnum() + " (" + CombatStats.GetAttackTimeFromEnum(__instance.ProcessedCombatStats.AttackSpeed) +"s)";
			string text6 = __instance.ProcessedCombatStats.AttackDamage.TranslateEnum() + " (" + baseAttackDamageFromEnum1 +"dmg)";
			string text7 = __instance.ProcessedCombatStats.HitChance.TranslateEnum() + " (" + (hitChanceFromEnum * 100f).ToString("0") +"%)";
			string text8 = __instance.ProcessedCombatStats.Defence.TranslateEnum() + " (" + defenceFromEnum +"dmg)";
			
			string text9 = "Estimated dmg/s";
			string averageAttackDamagePerSecond = (calculatedDmg / attackTimeFromEnum * hitChanceFromEnum).ToString("0.00");
			
			__result = "\n\n<size=80%>" + text + " " + text5 + "\n" + text2 + " " + text7 + "\n" + text3 + " " + text6 + "\n" + text4 + ": " + text8 + "\n" + text9 + ": " + averageAttackDamagePerSecond + "</size>";
		}
		
		
		[HarmonyPatch(typeof(CardData), "GetPossibleDrops")]
		[HarmonyPostfix]
		private static void CardData__GetPossibleDrops_Postfix(ref CardData __instance, ref  List<string> __result)
		{
		//Log("CardData__GetPossibleDrops_Postfix");
			if (__result.Count == 0 && __instance is Mob mob && __instance is not Enemy)
			{
				__result.AddRange(mob.Drops.GetCardsInBag());
				if (mob.CanHaveInventory)
				{
					__result.AddRange((from x in mob.PossibleEquipables
						where x.blueprint != null
						select x.blueprint.Id).ToList());
				}
			}
		}
		
		[HarmonyPatch(typeof(StablePortal), "UpdateCard")]
		[HarmonyPrefix]
		private static void StablePortal__UpdateCard_Prefix(out bool __state, ref string ___descriptionOverride)
		{
			__state = string.IsNullOrWhiteSpace(___descriptionOverride);
		}
		
		[HarmonyPatch(typeof(StablePortal), "UpdateCard")]
		[HarmonyPostfix]
		private static void StablePortal__UpdateCard_Postfix(bool __state, ref string ___descriptionOverride, ref CardData __instance)
		{
			if (__state && !string.IsNullOrWhiteSpace(___descriptionOverride) && !__instance.MyGameCard.IsDemoCard)
			{
				___descriptionOverride += "\n" + SokLoc.Translate("label_wave", LocParam.Create("wave", WorldManager.instance.CurrentRunVariables.ForestWave.ToString()));
			}
		}
		
		[HarmonyPatch(typeof(Boosterpack), "GetSummary")]
		[HarmonyPrefix]
		private static bool Boosterpack__GetSummary_Prefix(out string __result, Boosterpack __instance)
		{
		//Log("Boosterpack__GetSummary_Prefix");
			__result = "";
			List<CardChance> list = new List<CardChance>();
			foreach (CardBag cardBag in __instance.CardBags)
			{
				__result += BetterInfoPlugin.GetSummaryFromAllCards(cardBag) + "\n";
			}
			//__result = BetterInfoPlugin.GetSummaryFromAllCards(list);
			return false;
		}
		
		[HarmonyPatch(typeof(Harvestable), "UpdateDescription")]
		[HarmonyPrefix]
		private static bool Harvestable__UpdateDescription_Prefix(Harvestable __instance, ref string ___descriptionOverride)
		{
			//Log("Harvestable__UpdateDescription_Prefix");
			___descriptionOverride = SokLoc.Translate(__instance.DescriptionTerm) + "\n\n";
			if (__instance.Id == "catacombs")
			{
				CardBag cardBag = new CardBag();
				cardBag.CardBagType = CardBagType.Chances;
				cardBag.CardsInPack = 1;
				cardBag.Chances = new List<CardChance> (__instance.MyCardBag.Chances);
				cardBag.Chances.Add(new CardChance("goblet", __instance.Amount > 1 ? 2 : 0 ));
				___descriptionOverride += BetterInfoPlugin.GetSummaryFromAllCards(cardBag, "label_can_drop", __instance.IsUnlimited ? 0 : __instance.Amount) + "\n";
			}
			else if (__instance.Id == "cave")
			{
				CardBag cardBag = new CardBag();
				cardBag.CardBagType = CardBagType.Chances;
				cardBag.CardsInPack = 1;
				cardBag.Chances = new List<CardChance>();
				
				foreach (CardChance chance in __instance.MyCardBag.Chances)
				{
				
					cardBag.Chances.Add(chance.Id == "treasure_map" ? new CardChance(chance.Id, __instance.Amount > 1 ? 2 : 0 ) : chance);
				}
				___descriptionOverride += BetterInfoPlugin.GetSummaryFromAllCards(cardBag, "label_can_drop", __instance.IsUnlimited ? 0 : __instance.Amount) + "\n";
			}
			else if (__instance.Id == "old_tome")
			{
				List<CardData> list = WorldManager.instance.CardDataPrefabs.Where((CardData x) => x.MyCardType == CardType.Ideas && !WorldManager.instance.HasFoundCard(x.Id) && !x.HideFromCardopedia).ToList();
				if (!WorldManager.instance.CurrentRunVariables.VisitedIsland)
				{
					list.RemoveAll((CardData x) => x.IsIslandCard);
				}
				CardBag cardBag = new CardBag();
				cardBag.CardBagType = CardBagType.Chances;
				cardBag.CardsInPack = 1;
				cardBag.Chances = new List<CardChance>();
				if (list.Count <= 0)
				{
					cardBag.Chances.Add(new CardChance("map",1));
				}
				else
				{
					list.ForEach((CardData x) => cardBag.Chances.Add(new CardChance(x.Id,1)));
				}
				___descriptionOverride += BetterInfoPlugin.GetSummaryFromAllCards(cardBag, "label_can_drop", __instance.IsUnlimited ? 0 : __instance.Amount) + "\n";	
			}
			else if (__instance is FishingSpot)
			{
			// -------------------------------------------------------------------------------------------------------TODO
				foreach (CardBag cardBag in __instance.GetCardBags())
				{
					___descriptionOverride += BetterInfoPlugin.GetSummaryFromAllCards(cardBag, "label_can_drop", __instance.IsUnlimited ? 0 : __instance.Amount) + "\n";
				}
			}
			else foreach (CardBag cardBag in __instance.GetCardBags())
			{
				___descriptionOverride += BetterInfoPlugin.GetSummaryFromAllCards(cardBag, "label_can_drop", __instance.IsUnlimited ? 0 : __instance.Amount) + "\n";
			}
			
			return false;
		}
		
		[HarmonyPatch(typeof(CombatableHarvestable), "UpdateCard")]
		[HarmonyPrefix]
		private static void CombatableHarvestable__UpdateDescription_Prefix(CombatableHarvestable __instance, ref string ___descriptionOverride)
		{
			if(string.IsNullOrEmpty(___descriptionOverride))
			{
		//Log("CombatableHarvestable__UpdateDescription_Prefix");
				___descriptionOverride = SokLoc.Translate(__instance.DescriptionTerm) + "\n\n";
				foreach (CardBag cardBag in __instance.GetCardBags())
				{
					___descriptionOverride += BetterInfoPlugin.GetSummaryFromAllCards(cardBag, "label_can_drop",__instance.IsUnlimited ? 0 : __instance.Amount) + "\n";
				}
			}
		}
		
		[HarmonyPatch(typeof(CardopediaScreen), "GetDropSummaryFromCard")]
		[HarmonyPrefix]
		private static bool CardData__GetDropSummaryFromCard_Prefix(out string __result, CardData cardData)
		{
		//Log("CardData__GetDropSummaryFromCard_Prefix");
			__result = "";
			if (cardData is Mob mob)
			{
				foreach (CardBag cardBag in cardData.GetCardBags())
				{
					__result += BetterInfoPlugin.GetSummaryFromAllCards(cardBag, "label_can_drop") + "\n";
				}
				return false;
			}
			return cardData is not Harvestable && cardData is not CombatableHarvestable;
		}
		
		
		public static List<CardChance> CardBagToChance(CardBag bag)
		{
		//Log("CardBagToChance");
			List<CardChance> chances = new List<CardChance>();
			
			if (bag.CardBagType == CardBagType.SetCardBag)
			{
				chances = CardBag.GetChancesForSetCardBag(WorldManager.instance.GameDataLoader, bag.SetCardBag);
			}
			else if (bag.CardBagType == CardBagType.SetPack)
			{
				chances = bag.SetPackCards.Select((string x) => new CardChance(x, 1)).ToList();
			}
			else if (bag.CardBagType == CardBagType.Chances)
			{
				chances = bag.Chances;
			}
			else if (bag.CardBagType == CardBagType.Enemies)
			{
				
				SetCardBag setCardBagForEnemyCardBag = WorldManager.instance.GameDataLoader.GetSetCardBagForEnemyCardBag(bag.EnemyCardBag);
				chances = CardBag.GetChancesForSetCardBag(WorldManager.instance.GameDataLoader, setCardBagForEnemyCardBag);
				chances.RemoveAll(delegate(CardChance x)
				{
					Combatable combatable = WorldManager.instance.GameDataLoader.GetCardFromId(x.Id) as Combatable;
					return (combatable != null && combatable.ProcessedCombatStats.CombatLevel > bag.StrengthLevel) ? true : false;
				});
			}
			
			List<CardChance> chances2 = new List<CardChance>();
			foreach (CardChance c in chances)
			{
				if (c.IsEnemy)
				{
					chances2.Add(c);
					continue;
				}
				if ((c.HasMaxCount && WorldManager.instance.AllCards.Count((GameCard card) => card.CardData.Id == c.Id && card.MyBoard.IsCurrent) >= c.MaxCountToGive)
					|| (c.HasPrerequisiteCard && !WorldManager.instance.GivenCards.Contains(c.PrerequisiteCardId)))
				{
					continue;
				}
				CardData cardPrefab = WorldManager.instance.GetCardPrefab(c.Id);
				if ((!WorldManager.instance.CurrentRunOptions.IsPeacefulMode || !(cardPrefab is Enemy))
					&& (!WorldManager.instance.CurrentRunOptions.IsPeacefulMode || !(cardPrefab.Id == "catacombs"))
					&& ((cardPrefab.MyCardType != CardType.Ideas && cardPrefab.MyCardType != CardType.Rumors) || !WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(c.Id)))
				{
					chances2.Add(c);
				}
			}
			
			float num = 0f;
			
			foreach (CardChance chance in chances2)
			{
				num += (float)chance.Chance;
			}
			foreach (CardChance chance2 in chances2)
			{
				chance2.PercentageChance = (float)chance2.Chance / num;
			}
			List<CardChance> chances3 = new List<CardChance>();
			
			foreach (CardChance c in chances2)
			{
				if (c.IsEnemy)
				{
					//Log("attempting to calculate enemy chance");
					SetCardBag setCardBagForEnemyCardBag = WorldManager.instance.GameDataLoader.GetSetCardBagForEnemyCardBag(c.EnemyBag);
					List<CardChance> chancesForSetCardBag = CardBag.GetChancesForSetCardBag(WorldManager.instance.GameDataLoader, setCardBagForEnemyCardBag);
					
					//Log(chancesForSetCardBag.Count.ToString());
					/*
					List<CardIdWithEquipmentCombat> allPossibleEnemiesWithEquipment = GetAllPossibleEnemiesWithEquipment(GetEnemyPoolFromCardbags(chancesForSetCardBag.AsList(), true));
					allPossibleEnemiesWithEquipment.RemoveAll((CardIdWithEquipmentCombat x) => x.TotalCombatLevel > c.Strength);
					allPossibleEnemiesWithEquipment = allPossibleEnemiesWithEquipment.OrderByDescending((CardIdWithEquipmentCombat x) => x.TotalCombatLevel).ToList();
					if (allPossibleEnemiesWithEquipment.Count == 0)
					{
						return null;
					}
					*/
					
					chancesForSetCardBag.RemoveAll((CardChance x) => ((WorldManager.instance.GameDataLoader.GetCardFromId(x.Id) as Combatable).ProcessedCombatStats.CombatLevel > c.Strength) ? true : false);
					
					float chanceSumm = 0f;
					foreach (CardChance chance in chancesForSetCardBag)
					{
						//Log(chance.Id);
						chanceSumm += (float)chance.Chance;
					}
					foreach (CardChance chance4 in chancesForSetCardBag)
					{
						chance4.PercentageChance = (float)chance4.Chance * c.PercentageChance / chanceSumm;
					}
					//Log(chancesForSetCardBag.Count.ToString());
					
					chances3.AddRange(chancesForSetCardBag);
					continue;
				}
				else
				{
					//Log("asasa");
					chances3.Add(c);
				}
			}
			
			return chances3.OrderByDescending((CardChance x) => x.PercentageChance).ToList();;
		}
		
		
		public static string GetSummaryFromAllCards(CardBag cardBag, string prefix = "label_may_contain", int customNumber = -1)
		{
			//Log("getting summary");
			List<CardChance> allCards = BetterInfoPlugin.CardBagToChance(cardBag);
			if (allCards.Count == 0)
			{
				return "";
			}
			List<CardChance> list = allCards.Distinct().ToList();
			List<string> list2 = new List<string>();
			int num = 0;
			float unDiscChance = 0f;
			foreach (CardChance item2 in list)
			{
				CardData cardPrefab = WorldManager.instance.GetCardPrefab(item2.Id);
				string item = cardPrefab.FullName;
				if (cardPrefab.MyCardType == CardType.Ideas)
				{
					item = SokLoc.Translate("label_an_idea");
				}
				if (cardPrefab.MyCardType == CardType.Rumors)
				{
					item = SokLoc.Translate("label_a_rumor");
				}
				item += ": " + (item2.PercentageChance * 100).ToString("0");
				if (!WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(item2.Id))
				{
					num++;
					unDiscChance += item2.PercentageChance * 100;
				}
				else if (!list2.Contains(item))
				{
					list2.Add(item);
				}
			}
			list2 = list2.Select((string x) => "  • " + x + "%").ToList();
			string text = string.Join("\n", list2);
			string text2 = "";
			
			if (!string.IsNullOrEmpty(prefix))
			{
				text2 = SokLoc.Translate(prefix) + "\n";
			}
			
			if (customNumber == -1)
			{
				text2 += cardBag.CardsInPack.ToString() + "x\n";
			}
			else if (customNumber > 0)
			{
				text2 += customNumber.ToString() + "x\n";
			}
			
			if (num > 0)
			{
				text2 = text2 + "  • " + SokLoc.Translate("label_undiscovered_cards", LocParam.Plural("count", num)) + ": " + unDiscChance.ToString("0") +"%\n";
			}
			return text2 + text + "\n";
		}
		
		
		[HarmonyPatch(typeof(Equipable), "GetEquipableInfo")]
		[HarmonyPrefix]
		private static bool Equipable__GetEquipableInfo_Prefix()
		{
			return false;
		}
		
		[HarmonyPatch(typeof(Equipable), "UpdateCard")]
		[HarmonyPrefix]
		private static void Equipable__UpdateCard_Prefix(out string __state, string ___descriptionOverride)
		{
			__state = ___descriptionOverride;
		}
		[HarmonyPatch(typeof(Equipable), "UpdateCard")]
		[HarmonyPostfix]
		private static void Equipable__UpdateCard_Postfix(ref Equipable __instance, ref string __state, ref string ___descriptionOverride)
		{
			if (!string.IsNullOrEmpty(__state))  
			{
				___descriptionOverride = __state;
			}
			else
			{
				string text = "";
				text += SokLoc.Translate("label_itemlevel", LocParam.Create("level", Mathf.RoundToInt(__instance.MyStats.CombatLevel).ToString()));
				string text2 = __instance.MyStats.SummarizeSpecialHits();
				if (text2.Length > 0)
				{
					text = text + "\n\n" + text2;
				}
				___descriptionOverride += text;
				
				if (!__instance.MyGameCard.IsDemoCard)
				{
					___descriptionOverride += __instance.GetEquipableInfoAdvanced();
				}
			}
		}
		
		/*
		[HarmonyPatch(typeof(Boosterpack), "Update")]
		[HarmonyPrefix]
		private static void Boosterpack__Updaten_Prefix(CombatableHarvestable __instance, ref string ___descriptionOverride)
		{
			if(string.IsNullOrEmpty(___descriptionOverride))
			{
				___descriptionOverride = SokLoc.Translate(__instance.DescriptionTerm) + "\n\n";
				foreach (CardBag cardBag in __instance.GetCardBags())
				{
					___descriptionOverride += BetterInfoPlugin.GetSummaryFromAllCards(cardBag) + "\n";
				}
			}
		}*/
	}
	
	
}
