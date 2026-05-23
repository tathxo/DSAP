import dataclasses
from typing import Iterable, Callable
import enum
from enum import IntEnum
from Options import OptionCounter


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
    extra_conditions: Callable[..., bool]|None = None

    def __post_init__(self):
        _all_skips.append(self)

    def has_rules(self):
        return not (len(self.required_items) == 0 and self.extra_conditions is None)





def get_all_skips() -> Iterable[Skip]:
    return sorted(_all_skips, key=lambda x: x.name)

def get_user_selected_skips(selected_skips_names: list[str]) -> Iterable[Skip]:
    return filter(lambda skip: skip.name in selected_skips_names , get_all_skips())

def parse_skip_options(options:list[OptionCounter]) -> list[str]:

    result = []

    for option in options:
        for key, val in option.items():
            if val> 0:
                result.append(key)
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

# TODO remove from __init__ but set as default
Skip(name="Blighttown fog skip",
     starting_location="Upper Blighttown Depths Side",
     ending_location="Lower Blighttown",

    techniques=[],
    difficulty=SkipDifficulty.EASY     
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



Skip(name="Pinwheel Skip",
     starting_location="The Catacombs - After Door 1",
     ending_location="Tomb of the Giants",

    techniques=[SkipTechniques.WRONG_WARP],
    difficulty=SkipDifficulty.EASY     
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


########
### Skips with items
########


Skip(name=f"Firesage skip",
     starting_location="Demon Ruins",
     ending_location="Demon Ruins - Centipede Demon",

    techniques=[SkipTechniques.OUT_OF_BOUNDS, SkipTechniques.GRAB_CANCEL],
    difficulty=SkipDifficulty.HARD,

    required_items=["Shield"]  # TODO CHANGE TO MEDIUM SHIELD   
)


Skip(name="Quellag Boss Cheese",
     starting_location="Lower Blighttown - After Quelaag",
     ending_location= "Lower Blighttown - Quelaag",

    techniques=[SkipTechniques.BOSS_CHEESE ],
    difficulty=SkipDifficulty.EASY ,

    required_items=["Dungpie"]  # TODO CHANGE    
)



