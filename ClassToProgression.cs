﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.Blueprints.Items;
using static Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.UI.Log;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker;
using UnityEngine;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Designers.Mechanics.WeaponEnchants;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.ResourceLinks;
using Kingmaker.Items;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Alignments;
using HarmonyLib;
using static ArcanistTweaks.Extenders;

namespace ArcanistTweaks
{
    class ClassToProgression
    {
        public enum DomainSpellsType
        {
            NoSpells = 1,
            SpecialList = 2,
            NormalList = 3
        }

        public static void addClassToDomains(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, DomainSpellsType spells_type, BlueprintFeatureSelection domain_selection, BlueprintCharacterClass class_to_check)
        {
            var domains = domain_selection.AllFeatures;
            foreach (var domain_feature in domains)
            {
                addClassToFact(class_to_add, archetypes_to_add, spells_type, domain_feature, class_to_check);
            }
        }


        public static void addClassToProgression(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, DomainSpellsType spells_type, BlueprintProgression progression, BlueprintCharacterClass class_to_check)
        {
            if (progression.Classes.Contains(class_to_check))
            {
                foreach (var archetype in archetypes_to_add)
                {
                    var ArchLevel = new BlueprintProgression.ArchetypeWithLevel();

                    ArchLevel.m_Archetype = archetype.ToReference<BlueprintArchetypeReference>();
                    progression.m_Archetypes = progression.m_Archetypes.AddToArray(ArchLevel);
                }
                var classLevel = new BlueprintProgression.ClassWithLevel();

                classLevel.m_Class = class_to_add.ToReference<BlueprintCharacterClassReference>();
                progression.m_Classes = progression.m_Classes.AddToArray(classLevel);
            }

            foreach (var entry in progression.LevelEntries)
            {
                foreach (var feat in entry.Features)
                {
                    addClassToFact(class_to_add, archetypes_to_add, spells_type, feat, class_to_check);
                }
            }

            addClassToFeat(class_to_add, archetypes_to_add, spells_type, progression, class_to_check);
        }

        static public void addClassToFact(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, DomainSpellsType spells_type, BlueprintUnitFact f, BlueprintCharacterClass class_to_check)
        {
            if (f is BlueprintAbility)
            {
                addClassToAbility(class_to_add, archetypes_to_add, (f as BlueprintAbility), class_to_check);
            }
            else if (f is BlueprintActivatableAbility)
            {
                addClassToBuff(class_to_add, archetypes_to_add, (f as BlueprintActivatableAbility).Buff, class_to_check);
            }
            else if (f is BlueprintFeatureSelection)
            {
                if ((f as BlueprintFeatureSelection).Group == FeatureGroup.Feat
                    || (f as BlueprintFeatureSelection).Group == FeatureGroup.WizardFeat
                    || (f as BlueprintFeatureSelection).Group == FeatureGroup.RogueTalent
                    )
                {
                    return;
                }
                foreach (var af in (f as BlueprintFeatureSelection).AllFeatures)
                {
                    if (af.HasGroup(FeatureGroup.Feat, FeatureGroup.WizardFeat, FeatureGroup.RagePower, FeatureGroup.RogueTalent))
                    {
                        return;
                    }
                    addClassToFact(class_to_add, archetypes_to_add, spells_type, af, class_to_check);
                }
                addClassToFeat(class_to_add, archetypes_to_add, spells_type, (f as BlueprintFeatureBase), class_to_check);
            }
            else if (f is BlueprintProgression)
            {
                addClassToProgression(class_to_add, archetypes_to_add, spells_type, (f as BlueprintProgression), class_to_check);
            }
            else if (f is BlueprintFeature)
            {
                addClassToFeat(class_to_add, archetypes_to_add, spells_type, (f as BlueprintFeatureBase), class_to_check);
            }


        }


        static public void addClassToAbility(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, BlueprintAbility a, BlueprintCharacterClass class_to_check)
        {
            var components = a.ComponentsArray.ToArray();

            foreach (var c in components.ToArray())
            {
                if (c is AbilityVariants)
                {
                    foreach (var v in (c as AbilityVariants).Variants)
                    {
                        addClassToAbility(class_to_add, archetypes_to_add, v, class_to_check);
                    }
                }
                else if (c is ContextRankConfig)
                {
                    addClassToContextRankConfig(class_to_add, archetypes_to_add, c as ContextRankConfig, a.name, class_to_check);
                }
                else if (c is ContextCalculateAbilityParamsBasedOnClass)
                {
                    var c_typed = c as ContextCalculateAbilityParamsBasedOnClass;
                    if (c_typed.CharacterClass == class_to_check)
                    {
                        a.ReplaceComponent(c, Helpers.createContextCalculateAbilityParamsBasedOnClassesWithArchetypes(new BlueprintCharacterClass[] { c_typed.CharacterClass, class_to_add }.Distinct().ToArray(), archetypes_to_add, c_typed.StatType));
                    }
                }
                else if (c is NewMechanics.ContextCalculateAbilityParamsBasedOnClasses)
                {
                    var c_typed = c as NewMechanics.ContextCalculateAbilityParamsBasedOnClasses;
                    if (c_typed.CharacterClasses.Contains(class_to_check))
                    {
                        a.ReplaceComponent(c, Helpers.createContextCalculateAbilityParamsBasedOnClassesWithArchetypes(c_typed.CharacterClasses.AddToArray(class_to_add).Distinct().ToArray(), c_typed.archetypes.AddRangeToArray(archetypes_to_add).Distinct().ToArray(), c_typed.StatType));
                    }
                }
                else if (c is AbilityEffectRunAction)
                {
                    addClassToActionList(class_to_add, archetypes_to_add, (c as AbilityEffectRunAction).Actions, class_to_check);
                }
                else if (c is AbilityEffectStickyTouch)
                {
                    addClassToAbility(class_to_add, archetypes_to_add, (c as AbilityEffectStickyTouch).TouchDeliveryAbility, class_to_check);
                }
            }
        }

        public static void addClassToContextRankConfig(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, ContextRankConfig c, string archetypes_list_prefix, BlueprintCharacterClass class_to_check)
        {
            var classes = Helpers.GetField<BlueprintCharacterClassReference[]>(c, "m_Class");

            if (classes == null || classes.Empty() || !classes.HasReference(class_to_check.ToReference<BlueprintCharacterClassReference>()))
            {
                return;
            }

            classes = classes.AddToArray(class_to_add.ToReference<BlueprintCharacterClassReference>()).Distinct().ToArray();

            Helpers.SetField(c, "m_Class", classes);


            if (!archetypes_to_add.Empty())
            {
                var base_value_type = Helpers.GetField<ContextRankBaseValueType>(c, "m_BaseValueType");
                var rank_type = Helpers.GetField<AbilityRankType>(c, "m_Type");
                if (base_value_type == ContextRankBaseValueType.ClassLevel || base_value_type == ContextRankBaseValueType.SummClassLevelWithArchetype)
                {
                    var archetypes_list = Helpers.CreateFeature(archetypes_list_prefix + rank_type.ToString() + "ArchetypesListFeature",
                                            "",
                                            "",
                                            "",
                                            null,
                                            FeatureGroup.None,
                                            Helpers.Create<ContextRankConfigArchetypeList>(a => a.archetypes = archetypes_to_add)
                                            );
                    Helpers.SetField(c, "m_BaseValueType", ContextRankBaseValueTypeExtender.SummClassLevelWithArchetypes.ToContextRankBaseValueType());
                    Helpers.SetField(c, "m_Feature", archetypes_list);
                    Resources.AddBlueprint(archetypes_list);
                }
                else if (base_value_type == ContextRankBaseValueType.MaxClassLevelWithArchetype)
                {
                    var archetypes_list = Helpers.CreateFeature(archetypes_list_prefix + rank_type.ToString() + "ArchetypesListFeature",
                        "",
                        "",
                        "",
                        null,
                        FeatureGroup.None,
                        Helpers.Create<ContextRankConfigArchetypeList>(a => a.archetypes = archetypes_to_add)
                        );
                    Helpers.SetField(c, "m_BaseValueType", ContextRankBaseValueTypeExtender.MaxClassLevelWithArchetypes.ToContextRankBaseValueType());
                    Helpers.SetField(c, "m_Feature", archetypes_list);
                    Resources.AddBlueprint(archetypes_list);
                }
                else if (base_value_type == ContextRankBaseValueTypeExtender.SummClassLevelWithArchetypes.ToContextRankBaseValueType() ||
                         base_value_type == ContextRankBaseValueTypeExtender.MaxClassLevelWithArchetypes.ToContextRankBaseValueType())
                {
                    var archetypes_list = Helpers.GetField<BlueprintFeature>(c, "m_Feature");
                    archetypes_list.ReplaceComponent<ContextRankConfigArchetypeList>(a => a.archetypes = a.archetypes.AddRangeToArray(archetypes_to_add).Distinct().ToArray());
                }
            }
        }

        static void addClassToActionList(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, ActionList action_list, BlueprintCharacterClass class_to_check)
        {
            foreach (var a in action_list.Actions)
            {
                if (a == null)
                {
                    continue;
                }
                if (a is ContextActionApplyBuff)
                {
                    addClassToBuff(class_to_add, archetypes_to_add, (a as ContextActionApplyBuff).Buff, class_to_check);
                }
                else if (a is ContextActionSpawnAreaEffect)
                {
                    addClassToAreaEffect(class_to_add, archetypes_to_add, (a as ContextActionSpawnAreaEffect).AreaEffect, class_to_check);
                }
                else if (a is Conditional)
                {
                    var a_conditional = (a as Conditional);
                    addClassToActionList(class_to_add, archetypes_to_add, a_conditional.IfTrue, class_to_check);
                    addClassToActionList(class_to_add, archetypes_to_add, a_conditional.IfFalse, class_to_check);
                }
                else if (a is ContextActionConditionalSaved)
                {
                    var a_conditional = (a as ContextActionConditionalSaved);
                    addClassToActionList(class_to_add, archetypes_to_add, a_conditional.Failed, class_to_check);
                    addClassToActionList(class_to_add, archetypes_to_add, a_conditional.Succeed, class_to_check);
                }
            }
        }


        public static void addClassToBuff(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, BlueprintBuff b, BlueprintCharacterClass class_to_check)
        {
            var components = b.ComponentsArray;
            foreach (var c in components.ToArray())
            {
                if (c is Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect)
                {
                    addClassToAreaEffect(class_to_add, archetypes_to_add, (c as AddAreaEffect).AreaEffect, class_to_check);
                }
                else if (c is AddFactContextActions)
                {
                    var c_typed = c as AddFactContextActions;
                    if (c_typed.NewRound != null)
                    {
                        addClassToActionList(class_to_add, archetypes_to_add, c_typed.NewRound, class_to_check);
                    }
                    if (c_typed.Activated != null)
                    {
                        addClassToActionList(class_to_add, archetypes_to_add, c_typed.Activated, class_to_check);
                    }
                    if (c_typed.Deactivated != null)
                    {
                        addClassToActionList(class_to_add, archetypes_to_add, c_typed.Deactivated, class_to_check);
                    }
                }
                else if (c is AddFacts)
                {
                    var c_typed = c as AddFacts;
                    foreach (var f in c_typed.Facts)
                    {
                        addClassToFact(class_to_add, archetypes_to_add, DomainSpellsType.NoSpells, f, class_to_check);
                    }
                }
            }
        }


        public static void addClassToAreaEffect(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, BlueprintAbilityAreaEffect a, BlueprintCharacterClass class_to_check)
        {
            var components = a.ComponentsArray;
            foreach (var c in components)
            {
                if (c is AbilityAreaEffectBuff)
                {
                    addClassToBuff(class_to_add, archetypes_to_add, (c as AbilityAreaEffectBuff).Buff, class_to_check);
                }
                else if (c is AbilityAreaEffectRunAction)
                {
                    var c_typed = c as AbilityAreaEffectRunAction;
                    if (c_typed.Round != null)
                    {
                        addClassToActionList(class_to_add, archetypes_to_add, c_typed.Round, class_to_check);
                    }
                    if (c_typed.UnitEnter != null)
                    {
                        addClassToActionList(class_to_add, archetypes_to_add, c_typed.UnitEnter, class_to_check);
                    }
                    if (c_typed.UnitEnter != null)
                    {
                        addClassToActionList(class_to_add, archetypes_to_add, c_typed.UnitExit, class_to_check);
                    }
                    if (c_typed.UnitMove != null)
                    {
                        addClassToActionList(class_to_add, archetypes_to_add, c_typed.UnitMove, class_to_check);
                    }
                }
            }
        }


        static public void addClassToFeat(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, DomainSpellsType spells_type, BlueprintFeatureBase feat, BlueprintCharacterClass class_to_check)
        {
            foreach (var c in feat.ComponentsArray.ToArray())
            {
                if (c is IncreaseSpellDamageByClassLevel)
                {
                    var c_typed = c as IncreaseSpellDamageByClassLevel;
                    if (c_typed.CharacterClass == class_to_check || c_typed.AdditionalClasses.Contains(class_to_check))
                    {
                        foreach (var archetype in archetypes_to_add)
                        {
                            c_typed.m_Archetypes = c_typed.m_Archetypes.AddToArray(archetype.ToReference<BlueprintArchetypeReference>()).Distinct().ToArray();
                        }
                        c_typed.m_AdditionalClasses = c_typed.m_AdditionalClasses.AddToArray(class_to_add.ToReference<BlueprintCharacterClassReference>()).Distinct().ToArray();
                    }
                }
                if (c is AddFeatureOnApply)
                {
                    var c_typed = c as AddFeatureOnApply;
                    addClassToFact(class_to_add, archetypes_to_add, spells_type, c_typed.Feature, class_to_check);
                }
                else if (c is AddFeatureOnClassLevel)
                {
                    var c_typed = c as AddFeatureOnClassLevel;
                    if (c_typed.Class == class_to_check || c_typed.AdditionalClasses.Contains(class_to_check))
                    {
                        foreach (var archetype in archetypes_to_add)
                        {
                            c_typed.m_Archetypes = c_typed.m_Archetypes.AddToArray(archetype.ToReference<BlueprintArchetypeReference>()).Distinct().ToArray();
                        }
                        c_typed.m_AdditionalClasses = c_typed.m_AdditionalClasses.AddToArray(class_to_add.ToReference<BlueprintCharacterClassReference>()).Distinct().ToArray();
                        addClassToFact(class_to_add, archetypes_to_add, spells_type, c_typed.Feature as BlueprintUnitFact, class_to_check);
                    }
                }
                else if (c is LevelUpMechanics.AddFeatureOnClassLevelRange)
                {
                    var c_typed = c as LevelUpMechanics.AddFeatureOnClassLevelRange;
                    if (c_typed.classes.Contains(class_to_check))
                    {
                        c_typed.classes = c_typed.classes.AddToArray(class_to_add).Distinct().ToArray();
                        c_typed.archetypes = c_typed.archetypes.AddRangeToArray(archetypes_to_add).Distinct().ToArray();
                        if (!c_typed.class_bonuses.Empty())
                        {
                            c_typed.class_bonuses = c_typed.class_bonuses.AddToArray(c_typed.class_bonuses.Last());
                        }
                        addClassToFact(class_to_add, archetypes_to_add, spells_type, c_typed.Feature as BlueprintUnitFact, class_to_check);
                    }
                }
                else if (c is AddFacts)
                {
                    var c_typed = c as AddFacts;
                    foreach (var f in c_typed.Facts)
                    {
                        addClassToFact(class_to_add, archetypes_to_add, spells_type, f, class_to_check);
                    }

                }
                else if (c is AddFeatureIfHasFact)
                {
                    var c_typed = c as AddFeatureIfHasFact;
                    addClassToFact(class_to_add, archetypes_to_add, spells_type, c_typed.Feature as BlueprintUnitFact, class_to_check);
                }
                else if (c is AddSpecialSpellList && spells_type == DomainSpellsType.SpecialList)
                {
                    var c_typed = c as AddSpecialSpellList;

                    if (c_typed.CharacterClass != class_to_add && c_typed.CharacterClass == class_to_check)
                    {
                        var c2 = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddSpecialSpellList>();
                        c2.m_CharacterClass = class_to_add.ToReference<BlueprintCharacterClassReference>();
                        c2.m_SpellList = c_typed.SpellList.ToReference<BlueprintSpellListReference>();
                        feat.AddComponent(c2);
                    }
                }
                else if (c is AddOppositionSchool && spells_type == DomainSpellsType.SpecialList)
                {
                    var c_typed = c as AddOppositionSchool;
                    if (c_typed.CharacterClass != class_to_add && c_typed.CharacterClass == class_to_check)
                    {
                        var c2 = Helpers.Create<AddOppositionSchool>();
                        c2.m_CharacterClass = class_to_add.ToReference<BlueprintCharacterClassReference>();
                        c2.School = c_typed.School;
                        feat.AddComponent(c2);
                    }
                }
                else if (c is FactSinglify)
                {
                    var c_typed = c as FactSinglify;
                    foreach (var f in c_typed.NewFacts)
                    {
                        addClassToFact(class_to_add, archetypes_to_add, spells_type, f, class_to_check);
                    }
                }
                else if (c is ReplaceCasterLevelOfAbility)
                {
                    var c_typed = c as ReplaceCasterLevelOfAbility;
                    if (c_typed.Class == class_to_check || c_typed.AdditionalClasses.Contains(class_to_check))
                    {
                        foreach (var archetype in archetypes_to_add)
                        {
                            c_typed.m_Archetypes = c_typed.m_Archetypes.AddToArray(archetype.ToReference<BlueprintArchetypeReference>()).Distinct().ToArray();
                        }
                        c_typed.m_AdditionalClasses = c_typed.m_AdditionalClasses.AddToArray(class_to_add.ToReference<BlueprintCharacterClassReference>()).Distinct().ToArray();
                    }
                }
                else if (c is BindAbilitiesToClass)
                {
                    var c_typed = c as BindAbilitiesToClass;
                    if (c_typed.CharacterClass == class_to_check || c_typed.AdditionalClasses.Contains(class_to_check))
                    {
                        foreach(var archetype in archetypes_to_add)
                        {
                            c_typed.m_Archetypes = c_typed.m_Archetypes.AddToArray(archetype.ToReference<BlueprintArchetypeReference>()).Distinct().ToArray();
                        }
                        c_typed.m_AdditionalClasses = c_typed.m_AdditionalClasses.AddToArray(class_to_add.ToReference<BlueprintCharacterClassReference>()).Distinct().ToArray();
                        
                    }
                }
                else if (c is ContextRankConfig)
                {
                    addClassToContextRankConfig(class_to_add, archetypes_to_add, c as ContextRankConfig, feat.name, class_to_check);
                }
                else if (c is LearnSpellList && spells_type == DomainSpellsType.NormalList)
                {
                    if ((c as LearnSpellList).CharacterClass == class_to_check)
                    {
                        var spell_list = (c as LearnSpellList)?.SpellList;
                        if (spell_list == null)
                        {
                            continue;
                        }
                        if (archetypes_to_add.Empty())
                        {
                            var learn_spells_fact = Helpers.Create<Kingmaker.UnitLogic.FactLogic.LearnSpellList>();
                            learn_spells_fact.m_SpellList = spell_list.ToReference<BlueprintSpellListReference>(); ;
                            learn_spells_fact.m_CharacterClass = class_to_add.ToReference<BlueprintCharacterClassReference>();
                            feat.AddComponent(learn_spells_fact);

                        }
                        else
                        {
                            foreach (var ar_type in archetypes_to_add)
                            {
                                var learn_spells_fact = Helpers.Create<Kingmaker.UnitLogic.FactLogic.LearnSpellList>();
                                learn_spells_fact.m_SpellList = spell_list.ToReference<BlueprintSpellListReference>();
                                learn_spells_fact.m_CharacterClass = class_to_add.ToReference<BlueprintCharacterClassReference>();
                                learn_spells_fact.m_Archetype = ar_type.ToReference<BlueprintArchetypeReference>();
                                feat.AddComponent(learn_spells_fact);
                            }
                        }
                    }
                }
                else if (c is AddKnownSpell && spells_type == DomainSpellsType.NormalList)
                {
                    if ((c as AddKnownSpell).CharacterClass == class_to_check)
                    {
                        if (archetypes_to_add.Empty())
                        {
                            var learn_spells_fact = Helpers.Create<AddKnownSpell>();
                            learn_spells_fact.m_Spell = (c as AddKnownSpell).Spell.ToReference<BlueprintAbilityReference>();
                            learn_spells_fact.SpellLevel = (c as AddKnownSpell).SpellLevel;
                            learn_spells_fact.m_CharacterClass = class_to_add.ToReference<BlueprintCharacterClassReference>();
                            feat.AddComponent(learn_spells_fact);

                        }
                        else
                        {
                            foreach (var ar_type in archetypes_to_add)
                            {
                                var learn_spells_fact = Helpers.Create<AddKnownSpell>();
                                learn_spells_fact.m_Spell = (c as AddKnownSpell).Spell.ToReference<BlueprintAbilityReference>();
                                learn_spells_fact.SpellLevel = (c as AddKnownSpell).SpellLevel;
                                learn_spells_fact.m_CharacterClass = class_to_add.ToReference<BlueprintCharacterClassReference>();
                                learn_spells_fact.m_Archetype = ar_type.ToReference<BlueprintArchetypeReference>();
                                feat.AddComponent(learn_spells_fact);
                            }
                        }
                    }
                }
            }
        }
    }
}
