import dataclasses
from typing import Iterable, Callable
import enum
from enum import IntEnum
from Options import OptionCounter
from BaseClasses import CollectionState

from worlds.AutoWorld import World

from . import Items, Groups

# Skips automatically populate this with their init method
_all_skips: list["Skip"] = []

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
    
    #state and player
    extra_conditions: Callable[[CollectionState, int], bool]|None = None

    def __post_init__(self):
        _all_skips.append(self)

    def has_rules(self):
        return not (len(self.required_items) + len(self.required_items_groups) == 0 and self.extra_conditions is None)


def get_all_skips() -> Iterable[Skip]:
    return sorted(_all_skips, key=lambda x: x.name)


def get_user_selected_skips(options:list[OptionCounter]) -> Iterable[Skip]: 
    enabled_skips = []
    for option in options:
        for key, val in option.items():
            if val != 0:
                enabled_skips.append(key)
    return filter(lambda skip: skip.name in enabled_skips, get_all_skips())



def required_item_pool_for_skips(world: World, current_required_item_pool: list[str]) -> list[str]: 
    skip_options = [world.options.skip_logic_easy, 
                    world.options.skip_logic_medium, 
                    world.options.skip_logic_hard, 
                    world.options.skip_logic_very_hard]
    enabled_skips = get_user_selected_skips(skip_options)
    skip_progression_item_groups: set[str] = set()
    skip_progression_items: set[str] = set()

    for skip in enabled_skips:
        skip_progression_item_groups = skip_progression_item_groups.union(skip.required_items_groups)
        skip_progression_items = skip_progression_items.union(skip.required_items)

    for group in skip_progression_item_groups:
        
        skip_progression_items.add(world.random.choice(Groups.item_name_groups[group]))

    result = []
    for item in skip_progression_items:
        if item not in current_required_item_pool:
            result.append(item)
    # print(result)
    return result
    







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

    techniques=[SkipTechniques.PLATFORMING],
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



########
### Skips with items
########

#TODO: After shop sanity gets implemented conditions for this need to change, the extra condition lambda -> required item check


Skip(name=f"Firesage skip",
     starting_location="Demon Ruins",
     ending_location="Demon Ruins - Centipede Demon",

    techniques=[SkipTechniques.OUT_OF_BOUNDS, SkipTechniques.GRAB_CANCEL],
    difficulty=SkipDifficulty.HARD,

    required_items_groups=["Medium Shields"]  
)




Skip(name="Pinwheel Skip",
     starting_location="The Catacombs - After Door 1",
     ending_location="Tomb of the Giants",

    techniques=[SkipTechniques.WRONG_WARP],
    difficulty=SkipDifficulty.EASY,

    extra_conditions= lambda state, player: state.can_reach_location("The Depths", player), # You can farm basilisks there
    # required_items=["Eye of death"] 
)

Skip(name="Quellag Boss Cheese",
     starting_location="Lower Blighttown - After Quelaag",
     ending_location= "Lower Blighttown - Quelaag",

    techniques=[SkipTechniques.BOSS_CHEESE ],
    difficulty=SkipDifficulty.EASY ,

    extra_conditions= lambda state, player: state.can_reach_location("Lower Undead Burg", player),
    # required_items=["Renewable Dung Pie"] 
)


Skip(name="Lost Izalith Shortcut",
     starting_location="Demon Ruins",
     ending_location= "Lost Izalith" ,

    techniques=[],
    difficulty=SkipDifficulty.EASY ,

    extra_conditions= lambda state, player: state.can_reach_location("The Depths", player), # You can farm rats there
    # required_items=["Renewable Humanity"] 
)

Skip(name="Anor londo rafters drop",
     starting_location="Anor Londo",
     ending_location=  "Anor Londo - Painting Room",

    techniques=[SkipTechniques.FALL_CONTROL_QUITOUT ],
    difficulty=SkipDifficulty.MEDIUM ,

    extra_conditions= lambda state, player: state.can_reach_location("Lower Undead Burg - After Residence Key", player),
    required_items_groups=["Catalysts"],
    # required_items=["Fall Control"] 
)


Skip(name="Four kings skip",
     starting_location="Upper New Londo Ruins",
     ending_location="The Abyss",

    techniques=[SkipTechniques.OUT_OF_BOUNDS],
    difficulty=SkipDifficulty.MEDIUM,

    required_items=["Covenant of Artorias"] 
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
    required_items_groups=["Ranged Weapons"],
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

    extra_conditions= lambda state, player: state.can_reach_location("Lower Undead Burg - After Residence Key", player),
    required_items_groups=["Catalysts"],
    # required_items=["Fall Control"] 
)


Skip(name="Artorias Cheese",
     starting_location= "Oolacile Township",
     ending_location="Royal Wood - Artorias",

    techniques=[SkipTechniques.BOSS_CHEESE ],
    difficulty=SkipDifficulty.EASY ,

    required_items_groups=["Bows"] 
)


Skip(name="Great Hollow Skip",
     starting_location="Upper Blighttown Depths Side",
     ending_location="Ash Lake" ,

    techniques=[SkipTechniques.OUT_OF_BOUNDS, SkipTechniques.SEAM_WALKING, SkipTechniques.FALL_CONTROL_QUITOUT ],
    difficulty=SkipDifficulty.HARD ,

    extra_conditions= lambda state, player: state.can_reach_location("Lower Undead Burg - After Residence Key", player),
    required_items_groups=["Catalysts"],
    # required_items=["Fall Control"] 
)



