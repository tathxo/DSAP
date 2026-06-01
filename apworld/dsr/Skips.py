import dataclasses
from typing import Iterable
import enum
from enum import IntEnum
from Options import OptionCounter
from rule_builder.rules import Rule, True_, And, HasGroup, CanReachRegion, False_

from worlds.AutoWorld import World


# Skips automatically populate this with their init method
_all_skips: list["Skip"] = []


def get_all_skips() -> Iterable["Skip"]:
    return sorted(_all_skips, key=lambda x: x.name)

class SkipDifficulty(IntEnum): 
    EASY=enum.auto()
    MEDIUM=enum.auto()
    HARD=enum.auto()
    VERY_HARD=enum.auto()



class SkipTechniques(IntEnum):
    PLATFORMING=enum.auto()
    DEATHCAM=enum.auto()
    OUT_OF_BOUNDS=enum.auto()
    GRAB_CANCEL=enum.auto()
    SEAM_WALKING=enum.auto()
    BOSS_CHEESE=enum.auto()
    WRONG_WARP=enum.auto()
    FALL_CONTROL_QUITOUT=enum.auto()
    SLOPE_ROLL = enum.auto()
    STANDUP_WALL_BREAK = enum.auto()




@dataclasses.dataclass
class Skip():
    name:str

    starting_location: str
    ending_location: str

    # These are checked when parsing options
    techniques: Iterable[SkipTechniques] 
    difficulty: SkipDifficulty

    # These are used for Rules
    required_items: list[str] = dataclasses.field(default_factory=list)
    required_items_groups: list[str] = dataclasses.field(default_factory=list)
    
    # extra_conditions
    extra_rules: list[Rule] = dataclasses.field(default_factory=list)

    def __post_init__(self):
        _all_skips.append(self)

    def has_rules(self):
        return len(self.required_items) + len(self.required_items_groups) + len(self.extra_rules) > 0
    
    def get_rule(self, world: World) -> Rule:        
        skip_rules_list = []

        match self.difficulty:
            case SkipDifficulty.EASY:  
                option_counter = world.options.skip_logic_easy
            case SkipDifficulty.MEDIUM:  
                option_counter = world.options.skip_logic_medium
            case SkipDifficulty.HARD:  
                option_counter = world.options.skip_logic_hard
            case SkipDifficulty.VERY_HARD:  
                option_counter = world.options.skip_logic_very_hard
            case _:
                raise AssertionError(f"Unhandled SkipDifficulty (should be exhaustive): {self.difficulty}")
            
        if option_counter[self.name] == 0:
            skip_rules_list.append(False_())
        else:
            skip_rules_list.append(True_())
            
        skip_rules_list.extend([HasGroup(group) for group in self.required_items_groups])

        skip_rules_list.extend(self.extra_rules)

        skip_rule = And(*skip_rules_list)

        return skip_rule





def get_user_selected_skips(options:list[OptionCounter]) -> Iterable[Skip]: 
    enabled_skips = []
    for option in options:
        for key, val in option.items():
            if val != 0:
                enabled_skips.append(key)
    return filter(lambda skip: skip.name in enabled_skips, get_all_skips())







###########
#### Skips with no Items
###########

Skip(name=f"Lower Undead Burg Skip",
     starting_location=f"Upper Undead Burg - Before Fog",
     ending_location=f"Lower Undead Burg",

    techniques=[SkipTechniques.PLATFORMING],
    difficulty=SkipDifficulty.EASY
)


Skip(name=f"Sens gate skip",
     starting_location="Undead Parish",
     ending_location="Sen's Fortress",

    techniques=[SkipTechniques.DEATHCAM],
    difficulty=SkipDifficulty.MEDIUM     
)



Skip(name=f"Early Asylum Return",
     starting_location="Firelink Shrine",
     ending_location="Firelink Shrine - After Undead Parish Elevator",

    techniques=[SkipTechniques.PLATFORMING],
    difficulty=SkipDifficulty.EASY     
)

Skip(name="Depths key skip",
     starting_location="Upper Undead Burg - Before Fog",
     ending_location="Depths",

    techniques=[SkipTechniques.OUT_OF_BOUNDS, SkipTechniques.SEAM_WALKING],
    difficulty=SkipDifficulty.MEDIUM     
)

Skip(name="Blighttown Key Skip - From Undeadburg",
     starting_location="Upper Undead Burg - Before Fog",
     ending_location="Upper Blighttown Depths Side",

    techniques=[SkipTechniques.OUT_OF_BOUNDS, SkipTechniques.SEAM_WALKING],
    difficulty=SkipDifficulty.MEDIUM     
)


Skip(name="The Annex skip",
     starting_location="Painted World of Ariamis",
     ending_location="Painted World of Ariamis - After Annex Key",

    techniques=[SkipTechniques.PLATFORMING],
    difficulty=SkipDifficulty.EASY     
)

Skip(name="Painted world fog gate skip",
     starting_location="Painted World of Ariamis",
     ending_location="Painted World of Ariamis - After Fog",

    techniques=[SkipTechniques.PLATFORMING],
    difficulty=SkipDifficulty.EASY     
)

Skip(name="Gold Pine Resin skip",
     starting_location= "Upper Undead Burg",
     ending_location="Upper Undead Burg - Pine Resin Chest",

    techniques=[SkipTechniques.PLATFORMING],
    difficulty=SkipDifficulty.EASY     
)

Skip(name="Quellag Skip",
     starting_location="Lower Blighttown",
     ending_location="Lower Blighttown - After Quelaag",

    techniques=[SkipTechniques.DEATHCAM],
    difficulty=SkipDifficulty.HARD     
)


Skip(name="Kiln Wrong warp",
     starting_location= "Firelink Altar",
     ending_location="Kiln of the First Flame",

    techniques=[SkipTechniques.WRONG_WARP], #TODO consider marking this in some way, as it skips a huge part of the game
    difficulty=SkipDifficulty.EASY     
)

Skip(name="Darkroot fogwall bypass",
     starting_location="Darkroot Garden - Before Fog",
     ending_location="Darkroot Garden",

    techniques=[SkipTechniques.PLATFORMING],
    difficulty=SkipDifficulty.EASY     
)

Skip(name="Gaping Dragon Boss Fog wall skip",
     starting_location="Depths",
     ending_location="Depths - Gaping Dragon",

    techniques=[SkipTechniques.PLATFORMING],
    difficulty=SkipDifficulty.VERY_HARD
)


Skip(name="Taurus Demon Skip",
     starting_location= "Upper Undead Burg",
     ending_location="Darkroot Garden - Before Fog",

    techniques=[SkipTechniques.OUT_OF_BOUNDS, SkipTechniques.WRONG_WARP],
    difficulty=SkipDifficulty.MEDIUM
)


Skip(name="Sens Fortress First fogwall Skip",
     starting_location= "Sen's Fortress",
     ending_location="Sen's Fortress - After First Fog",

    techniques=[SkipTechniques.PLATFORMING, SkipTechniques.STANDUP_WALL_BREAK],
    difficulty=SkipDifficulty.VERY_HARD
)


Skip(name="Tomb of Giants fogwall skip",
     starting_location= "Tomb of the Giants",
     ending_location="Tomb of the Giants - After White Fog",

    techniques=[SkipTechniques.PLATFORMING],
    difficulty=SkipDifficulty.EASY
)


Skip(name="OS fogwall Cheese",
     starting_location="Anor Londo - After Second Fog",
     ending_location="Anor Londo - Ornstein and Smough" ,

    techniques=[SkipTechniques.BOSS_CHEESE],
    difficulty=SkipDifficulty.MEDIUM ,
)




Skip(name="Moonlight Butterfly Skip",
     starting_location="Darkroot Garden",
     ending_location= "Darkroot Garden - After Moonlight Butterfly",

    techniques=[SkipTechniques.DEATHCAM ],
    difficulty=SkipDifficulty.VERY_HARD ,
)


Skip(name="Seal Skip",
     starting_location="Upper New Londo Ruins",
     ending_location="Lower New Londo Ruins - Reachable with Seal Skip",

    techniques=[SkipTechniques.OUT_OF_BOUNDS],
    difficulty=SkipDifficulty.MEDIUM,

)


Skip(name=f"Firesage Cheese",
     starting_location="Demon Ruins - After Demon Firesage",
     ending_location="Demon Ruins - Demon Firesage",

    techniques=[SkipTechniques.BOSS_CHEESE],
    difficulty=SkipDifficulty.HARD,
)


Skip(name=f"No Charred Ring in Demon Ruins",
     starting_location="Demon Ruins - Ceaseless Discharge",
     ending_location="Demon Ruins - Items in Lava",

    techniques=[],
    difficulty=SkipDifficulty.MEDIUM,
)


Skip(name=f"Darkmoon Seance Ring Skip",
    starting_location="Anor Londo - After First Fog",
    ending_location="Anor Londo - Behind Gwyndolin Statue",
    
    techniques=[SkipTechniques.STANDUP_WALL_BREAK],
    difficulty=SkipDifficulty.MEDIUM,
    )

########
### Skips with items
########

# When adding a new Item Group, make sure to add 2 version 1 with "Skip Tools - " prepended and 1 without

#TODO: After shop sanity gets implemented conditions for this need to change, the extra condition lambda -> required item check


Skip(name=f"Firesage skip",
     starting_location="Demon Ruins",
     ending_location="Demon Ruins - Centipede Demon",

    techniques=[SkipTechniques.OUT_OF_BOUNDS, SkipTechniques.GRAB_CANCEL],
    difficulty=SkipDifficulty.HARD,

    required_items_groups=["Medium Shields"]  
)

Skip(name=f"Ceaseless skip",
     starting_location="Demon Ruins - Early",
     ending_location= "Demon Ruins",

    techniques=[SkipTechniques.PLATFORMING, SkipTechniques.SLOPE_ROLL],
    difficulty=SkipDifficulty.HARD,

    required_items_groups=["Bows"],
    required_items=["Rite of Kindling"]
)


Skip(name=f"No Charred Ring in Lost Izalith",
     starting_location="Demon Ruins - Centipede Demon",
     ending_location="Lost Izalith",

    techniques=[],
    difficulty=SkipDifficulty.VERY_HARD,
    # required_items=["Rite of Kindling", "Flash Sweat"]
    required_items=["Rite of Kindling"],

    extra_rules=[CanReachRegion("Lower Blighttown - After Quelaag"),# In order to buy Flash Sweat
                 CanReachRegion("Depths"), # Humanity for extra healing 
                 HasGroup("Fire Keeper Souls", count=4) # Estus upgrades
                       ], 
)


Skip(name="Pinwheel Skip",
     starting_location="The Catacombs - After Door 1",
     ending_location="Tomb of the Giants",

    techniques=[SkipTechniques.WRONG_WARP],
    difficulty=SkipDifficulty.EASY,

    extra_rules=[CanReachRegion("Depths")], # You can farm basilisks there for one eye of death
    # required_items=["Eye of death"] 
)

Skip(name="Quellag Boss Cheese",
     starting_location="Lower Blighttown - After Quelaag",
     ending_location= "Lower Blighttown - Quelaag",

    techniques=[SkipTechniques.BOSS_CHEESE ],
    difficulty=SkipDifficulty.EASY ,

    extra_rules= [CanReachRegion("Lower Undead Burg")],
    # required_items=["Renewable Dung Pie"] 
)


Skip(name="Anor londo rafters drop",
     starting_location="Anor Londo",
     ending_location=  "Anor Londo - Painting Room",

    techniques=[SkipTechniques.FALL_CONTROL_QUITOUT ],
    difficulty=SkipDifficulty.MEDIUM ,

    extra_rules= [CanReachRegion("Lower Undead Burg - After Residence Key")],
    required_items_groups=["Catalysts"],
    # required_items=["Fall Control"] 
)


Skip(name="Four kings fogwall",
     starting_location="Upper New Londo Ruins",
     ending_location="The Abyss",

    techniques=[SkipTechniques.OUT_OF_BOUNDS],
    difficulty=SkipDifficulty.MEDIUM,

    required_items=["Covenant of Artorias"] 
)

Skip(name="Duke skip",
     starting_location="The Duke's Archives",
     ending_location= "The Duke's Archives - After First Seath Encounter",

    techniques=[SkipTechniques.PLATFORMING],
    difficulty=SkipDifficulty.MEDIUM,

    required_items_groups=["Bows"] 
)


# There is also a version of this without firebombs
Skip(name="Capra Cheese",
     starting_location= "Lower Undead Burg",
     ending_location="Lower Undead Burg - Capra Demon",

    techniques=[SkipTechniques.BOSS_CHEESE ],
    difficulty=SkipDifficulty.EASY ,

        ### Player can always reach undead merchant (female) from this location, so no condition necessary till shop sanity
    # extra_conditions= lambda state, player: state.can_reach_location("Lower Undead Burg", player), 
    # required_items_groups=["Renewable Throwable"] 
)

Skip(name="Manus Cheese",
     starting_location="Chasm of the Abyss",
     ending_location= "Chasm of the Abyss - Manus" ,

    techniques=[SkipTechniques.BOSS_CHEESE ],
    difficulty=SkipDifficulty.EASY ,
    required_items_groups=["Ranged Weapons"], # option to buy arrows or bolts somewhere, currently its always possible to buy them from vamos
    required_items=["Hawk Ring"] 
)


Skip(name="Archives Golden Fog gate skip",
     starting_location="Anor Londo",
     ending_location="The Duke's Archives",

    techniques=[SkipTechniques.OUT_OF_BOUNDS, SkipTechniques.SEAM_WALKING ],
    difficulty=SkipDifficulty.HARD ,

    required_items_groups=["Bows"] 
)


Skip(name="Artorias skip",
     starting_location="Royal Wood",
     ending_location="Chasm of the Abyss" ,

    techniques=[SkipTechniques.FALL_CONTROL_QUITOUT ],
    difficulty=SkipDifficulty.MEDIUM ,

    extra_rules=[CanReachRegion("Lower Undead Burg - After Residence Key")],
    required_items_groups=["Catalysts"],
    # required_items=["Fall Control"] 
)


Skip(name="Artorias Cheese",
     starting_location= "Oolacile Township",
     ending_location="Royal Wood - Artorias",

    techniques=[SkipTechniques.BOSS_CHEESE ],
    difficulty=SkipDifficulty.EASY ,

    required_items_groups=["Bows"] # option to buy arrows somewhere, currently its always possible to buy them from vamos
)


Skip(name="Great Hollow Skip",
     starting_location="Upper Blighttown Depths Side",
     ending_location="Ash Lake" ,

    techniques=[SkipTechniques.OUT_OF_BOUNDS, SkipTechniques.SEAM_WALKING, SkipTechniques.FALL_CONTROL_QUITOUT ],
    difficulty=SkipDifficulty.HARD ,

    extra_rules = [CanReachRegion("Lower Undead Burg - After Residence Key")],
    required_items_groups=["Catalysts"],
    # required_items=["Fall Control"] 
)


Skip(name="Lost Izalith Shortcut",
     starting_location="Demon Ruins - Between Firesage and Golden Foggate",
     ending_location=  "Lost Izalith - City" ,

    techniques=[],
    difficulty=SkipDifficulty.EASY ,

    extra_rules = [CanReachRegion("Depths")],
    # required_items=["30 Humanity"] 
)

