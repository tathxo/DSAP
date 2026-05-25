import typing
from dataclasses import dataclass
from Options import Toggle, DefaultOnToggle, Option, Range, Choice, ItemDict, OptionList, DeathLink, PerGameCommonOptions, OptionCounter
from Options import OptionGroup
from . import Skips


# QoL
class CanWarpWithoutLordvessel(DefaultOnToggle):
    """Gain the ability to warp as soon as you have rested at any warpable bonfire."""
    display_name = "Can Warp Without Lordvessel"

class WarpToAllBonfires(DefaultOnToggle):
    """Makes all bonfires eligible to be warped to via the in-game menu.
    No matter your settings, you will still need to actually rest at the warpable points to be able to warp to them.

    As a side-effect, this will also make warping set your destination as your "return" point upon next death, homeward, etc.
    Warpable bonfires are synced between all saves on the slot, regardless of your choice."""
    display_name = "Warp To All Bonfires"

# Difficulty
class GhostDifficulty(Choice):
    """How to manage difficulty for ghosts.

    - **Normal:** Ghost's behavior is unchanged and no mitigations are made to make them easier to deal with.
    - **Ghosts Are Not Ghostly:** Ghosts can be damaged by all weapons and items.
      Similar to having a transient curse always active.
    - **Rickert Sells Curses:** Rickert, the blacksmith/shopkeeper in the safe part of New Londo, 
      will sell unlimited transient curse items, 1500 souls apiece.
    """
    display_name = "Ghost Difficulty"
    option_normal = 0
    option_ghosts_are_not_ghostly = 1
    option_rickert_sells_curses = 2
    default = 2

class SoulMultiplierBase(Range):
    """Base value for a multiplier applied to enemy & boss soul drops, expressed as a percentage.
    100 is vanilla."""
    display_name = "Soul Multiplier - Minimum Percentage"
    range_start = 20
    range_end = 500
    default = 100

class SoulMultiplierMax(Range):
    """Maximum value for a multiplier applied to enemy & boss soul drops, expressed as a percentage.
    Only takes effect if not equal to soul_multiplier_base, and soul_multiplier_steps is non-zero."""
    display_name = "Soul Multiplier - Maximum Percentage"
    range_start = 20
    range_end = 500
    default = 100

class SoulMultiplierSteps(Range):
    """Number of steps from the base to the maximum multiplier. 
    This is how many "Progressive Soul Multiplier" items will be added to the item pool.
    
    Upon receiving one, the soul multiplier will increase (1/(this value)) * (the distance from max to base).
    As an example, if your soul_multiplier_base is 100, soul_multiplier_max is 200, and this is set to 4, then the
    first such item will set the soul drop multiplier to 125%, 2nd to 150%, and so on.

    Has no effect if the soul_multiplier_base and soul_multiplier_max are equal.

    If zero, soul_multiplier_base is applied as a static multiplier."""
    display_name = "Soul Multiplier Steps"
    range_start = 0
    range_end = 10
    option_4 = 4
    option_8 = 8
    default = 0

# Game Options
class GuaranteedItemsOption(ItemDict):
    """Guarantees that the specified items will be in the item pool"""
    display_name = "Guaranteed Items"

class ExcludedLocationBehaviorOption(Choice):
    """How to choose items for excluded locations in DSR.

    - **Forbid Useful:** Neither progression items nor useful items can be placed in excluded
      locations.
    - **Do Not Randomize:** Excluded locations always contain the same item as in vanilla
      Dark Souls: Remastered.
    """
    display_name = "Excluded Locations Behavior"
    option_forbid_useful = 2
    option_do_not_randomize = 3
    default = 2

class FogwallSanity(DefaultOnToggle):
    """Makes area fog walls uninteractable until you receive their items from the item pool. 
    Fog walls will also give checks when you pass through them.
    The Undead Asylum fog wall will not be locked, to avoid super-early BK.
    Recommended to turn this on."""
    display_name = "Fogwall Sanity" 

class BossFogwallSanity(Toggle):
    """Makes boss fog walls uninteractable until you receive their items from the item pool.
    Boss fog walls will also give checks when you pass through them.
    The following will be neither locations nor checks as they all do not have fog walls
    upon your first entry into their arena:
      Asylum Demon (first encounter),
      Sif,
      Seath (2nd encounter), and
      Kalameet."""
    display_name = "Boss Fogwall Sanity" 





def skip_logic_helper(difficulty: Skips.SkipDifficulty): 
    available_skips = Skips.get_all_skips()
    valid_keys = {skip.name for skip in available_skips if skip.difficulty == difficulty}
    default = {skip.name: 0 for skip in available_skips if skip.difficulty == difficulty}

    return valid_keys, default

class SkipLogicEasy(OptionCounter):
    """ In the following section you can enable specific skips to perform. The skips are losely categorized by their difficulty. 
        Numbers other than 0 put the skip into logic.
        Recommended to edit in the yaml. 

        For more detailed description of each individual skip visit https://docs.google.com/spreadsheets/d/1X7CHM0lT8vMiZmlGNtzdr3UXz71onuuRC2FyrNRw-UQ/edit?usp=sharing
    """
    display_name = "Enabled Easy Skips"
    difficulty = Skips.SkipDifficulty.EASY
    valid_keys, default = skip_logic_helper(difficulty)
    cull_zeroes=True


class SkipLogicMedium(OptionCounter):
    display_name = "Enabled Medium Skips"
    difficulty=Skips.SkipDifficulty.MEDIUM
    valid_keys, default = skip_logic_helper(difficulty)


class SkipLogicHard(OptionCounter):
    display_name = "Enabled Hard Skips"
    difficulty=Skips.SkipDifficulty.HARD
    valid_keys, default = skip_logic_helper(difficulty)

class SkipLogicVeryHard(OptionCounter):
    display_name = "Enabled Very Hard Skips"
    difficulty=Skips.SkipDifficulty.VERY_HARD
    valid_keys, default = skip_logic_helper(difficulty)


class LogicToAccessCatacombs(Choice):
    """Artificial logic for The Catacombs access. 
     Before the chosen condition, The Catacombs will be considered "out of logic"

    - **no_logic:** (not recommended) Catacombs is in-logic as soon as you get to Firelink Shrine.
    - **undead_merchant:** Access to Undead Merchant in the Upper Undead Burg puts Catacombs in-logic.
    - **andre:** Access to Andre puts Catacombs in-logic.
    - **andre_or_undead_merchant:** (default) Access to either Andre or Undead Merchant puts Catacombs in-logic.
    - **ornstein_and_smough:** Access to Ornstein and Smough puts Catcombs in-logic."""
    display_name = "Logic Requirement to Access Catacombs"
    option_no_logic = 0
    option_undead_merchant = 1
    option_andre = 2
    option_andre_or_undead_merchant = 3
    option_ornstein_and_smough = 4
    default = 3

class RandomizeStartingLoadouts(DefaultOnToggle):
    """Randomize each class's starting weapons, shields, and armors.
    This will also replace the thief's master key, which would break logic.
    Shields will always be 1-handed-wieldable with starting stats.
    Weapons will always be at least 2-handed-wieldable with starting stats.
    You can see what weapons/shield/item each class starts with in their description.
    Starting spells are randomized separately.""" 
    display_name = "Randomize Starting Loadout"

class RandomizeStartingGifts(DefaultOnToggle):
    """Randomize the starting gift options to a subset of a custom item pool.
    This pool includes many additional consumable options, and some rings.
    This pool also includes most of the default gifts, except Master key, which breaks logic.
    """
    display_name = "Randomize Starting Gifts"

class RequireOneHandedStartingWeapons(DefaultOnToggle):
    """If randomize_starting_loadouts is on, this option determines if each class's weapons can be wielded with one hand with their starting stats.
    Ignored if no_weapon_requirements is true."""
    display_name = "Require One Handed Starting Weapons"

class ExtraStartingWeaponForMeleeClasses(Toggle):
    """If randomize_starting_loadouts is on, this option determines if melee classes will get a second melee weapon."""
    display_name = "Extra Starting Weapon For Melee Classes"
    
class ExtraStartingShieldForAllClasses(Toggle):
    """If randomize_starting_loadouts is on, this option determines if all classes will get a second starting shield."""
    display_name = "Extra Starting Shield For All Classes"

class StartingSorcery(Choice):
    """How to randomize starting sorceries.
    Limited to spells the sorcerer can cast with their starting stats, unless "remove_spell_stat_requirements" is on.

    - **Soul Arrow:** Default option - vanilla starting sorcery
    - **Any:** Any sorcery
    - **Attack:** Any attack sorcery"""
    display_name = "Starting Sorcery"
    option_soul_arrow = 0
    option_any = 1
    option_attack = 2
    default = 0

class StartingMiracle(Choice):
    """How to randomize starting miracles.
    Limited to spells the cleric can cast with their starting stats, unless "remove_spell_stat_requirements" is on.

    - **Heal:** Default option - vanilla starting miracle
    - **Any:** Any miracle
    - **Healing:** Any healing miracle"""
    display_name = "Starting Miracle"
    option_heal = 0
    option_any = 1
    option_healing = 3
    default = 0

class StartingPyromancy(Choice):
    """How to randomize starting pyromancies.

    - **Fireball:** Default option - vanilla starting pyromancy
    - **Any:** Any pyromancy
    - **Attack:** Any attack pyromancy"""
    display_name = "Starting Pyromancy"
    option_fireball = 0
    option_any = 1
    option_attack = 2
    default = 0

class NoWeaponRequirements(Toggle):
    """Removes weapon and shield stat requirements."""
    display_name = "No Weapon Stat Requirements"

class NoSpellStatRequirements(Toggle):
    """Removes stat requirements for casting all miracles and sorceries."""
    display_name = "No Spell Stat Requirements"

class NoMiracleCovenantRequirements(DefaultOnToggle):
    """Removes covenant requirements for casting certain miracles."""
    display_name = "No Miracle Covenant Requirements"

class UpgradedWeaponsPercentage(Range):
    """Percentage of weapons (including shields) in the pool that will be replaced with upgraded versions, if possible.
    Does not affect starting equipment.
    Choose a higher value for an easier time."""
    display_name = "Upgraded Weapons Percentage"
    range_start = 0
    range_end = 100
    default = 0

class UpgradedWeaponsAllowedInfusions(OptionList):
    """Which infusions are allowed if UpgradedWeapons.
    Available infusion types are Normal, Raw, Magic, Fire, Divine, Chaos, Enchanted, Occult, Crystal, and Lightning.
    If "Normal" is removed, all upgraded weapons will have a different, available infusion."""
    display_name = "Upgraded Weapons - Allowed Infusions"
    default = {"Normal", "Raw", "Magic", "Fire", "Divine", "Chaos", "Enchanted", "Occult", "Crystal", "Lightning"}
    valid_keys = ["Normal", "Raw", "Magic", "Fire", "Divine", "Chaos", "Enchanted", "Occult", "Crystal", "Lightning"]

class UpgradedWeaponsAdjustedLevels(DefaultOnToggle):
    """For upgraded weapons added to the pool, Whether to 'adjust' weapon levels for applying the ranges.
    When true, the min/max levels will apply to an calculated "level" of an infused weapon adjusted by:
     +5 for Raw/Magic/Fire/Divine
     +10 for Chaos/Enchanted/Occult/Crystal/Lightning 
    When false, the min/max levels will not be adjusted."""
    display_name = "Upgraded Weapons - Adjust Ranges"

class UpgradedWeaponsMinLevel(Range):
    """Minimum upgrade value on upgraded weapons in the pool.
    This can exclude certain infusions if their calculated level cannot be at the minimum level. """
    display_name = "Upgraded Weapons - Minimum Level"
    range_start = 0
    range_end = 15
    default = 0


class UpgradedWeaponsMaxLevel(Range):
    """Maximum upgrade plus value on upgraded weapons in the pool."""
    display_name = "Upgraded Weapons - Maximum Level"
    range_start = 0
    range_end = 15
    default = 15

class EnableDeathlinkOption(Toggle):
    """Includes Deathlink"""
    display_name = "Enable Deathlink"

class GoalConditionOption(Choice):
    """Set the goal condition
    
    - **Gwyn:** Default option -Beat Gwyn, Lord of Cinder
    - **Bosses:** Defeat all bosses"""
    display_name = "Goal Condition"
    option_gwyn = 0
    option_all_bosses = 1
    default = 0
    
# Group relevant options
option_groups = [
    OptionGroup("Quality of Life", [
        CanWarpWithoutLordvessel,
        WarpToAllBonfires,
        ]),
    OptionGroup("Difficulty", [
        GhostDifficulty,
        SoulMultiplierBase,
        SoulMultiplierMax,
        SoulMultiplierSteps,
        ]),
    OptionGroup("Sanity", [
        FogwallSanity,
        BossFogwallSanity,
        ]),
    OptionGroup("Logic", [
        LogicToAccessCatacombs,
        SkipLogicEasy,
        SkipLogicMedium,
        SkipLogicHard,
        SkipLogicVeryHard
        ]),
    OptionGroup("Equipment", [
        RandomizeStartingLoadouts,
        RandomizeStartingGifts,
        RequireOneHandedStartingWeapons,
        ExtraStartingWeaponForMeleeClasses,
        ExtraStartingShieldForAllClasses,
        StartingSorcery,
        StartingMiracle,
        StartingPyromancy,
        NoWeaponRequirements,
        NoSpellStatRequirements,
        NoMiracleCovenantRequirements
        ]),
    OptionGroup("Upgraded Weapons", [
        UpgradedWeaponsPercentage,
        UpgradedWeaponsAllowedInfusions,
        UpgradedWeaponsAdjustedLevels,
        UpgradedWeaponsMinLevel,
        UpgradedWeaponsMaxLevel,
        ])
    ]


@dataclass
class DSROption(PerGameCommonOptions):

    # Game Options
    enable_deathlink: EnableDeathlinkOption
    goal_condition: GoalConditionOption
    guaranteed_items: GuaranteedItemsOption
    excluded_location_behavior: ExcludedLocationBehaviorOption

    # QoL
    can_warp_without_lordvessel: CanWarpWithoutLordvessel
    warp_to_all_bonfires: WarpToAllBonfires

    # Difficulty
    ghost_difficulty: GhostDifficulty
    soul_multiplier_base: SoulMultiplierBase
    soul_multiplier_max: SoulMultiplierMax
    soul_multiplier_steps: SoulMultiplierSteps

    # Sanity
    fogwall_sanity: FogwallSanity
    boss_fogwall_sanity: BossFogwallSanity

    # Logic
    logic_to_access_catacombs: LogicToAccessCatacombs
    skip_logic_easy: SkipLogicEasy
    skip_logic_medium: SkipLogicMedium
    skip_logic_hard: SkipLogicHard
    skip_logic_very_hard: SkipLogicVeryHard

    # Equipment
    randomize_starting_loadouts: RandomizeStartingLoadouts
    randomize_starting_gifts: RandomizeStartingGifts
    require_one_handed_starting_weapons: RequireOneHandedStartingWeapons
    extra_starting_weapon_for_melee_classes: ExtraStartingWeaponForMeleeClasses
    extra_starting_shield_for_all_classes: ExtraStartingShieldForAllClasses
    starting_sorcery: StartingSorcery
    starting_miracle: StartingMiracle
    starting_pyromancy: StartingPyromancy
    no_weapon_requirements: NoWeaponRequirements
    no_spell_stat_requirements: NoSpellStatRequirements
    no_miracle_covenant_requirements: NoMiracleCovenantRequirements

    # Upgraded Weapons
    upgraded_weapons_percentage: UpgradedWeaponsPercentage
    upgraded_weapons_allowed_infusions: UpgradedWeaponsAllowedInfusions
    upgraded_weapons_adjusted_levels : UpgradedWeaponsAdjustedLevels
    upgraded_weapons_min_level: UpgradedWeaponsMinLevel
    upgraded_weapons_max_level: UpgradedWeaponsMaxLevel
