from.Items import _all_items, key_item_names, DSRItemCategory, item_dictionary, _all_items_base, DSRUpgradeType



from worlds.AutoWorld import World




def BuildRequiredItemPool(world, count):
    item_pool = []
    remaining_count = count

    key_items = [item for item in _all_items if item.name in key_item_names or item.category == DSRItemCategory.KEY_ITEM]
    for item in key_items:
        if item.name not in ["Dungeon Cell Key", "Estus Flask", "Undead Asylum F2 East Key", "Big Pilgrim's Key", "Master Key"]:
            item_pool.append(item)
            remaining_count = remaining_count - 1

    if(world.options.fogwall_sanity.value == True):
        fogwalls = [item for item in _all_items if item.category in [DSRItemCategory.FOGWALL] and item.name != "Fog Wall Key - Northern Undead Asylum"]
        for item in fogwalls:
            item_pool.append(item)
            remaining_count = remaining_count - 1

    if (world.options.boss_fogwall_sanity.value == True):
        bossfogwalls = [item for item in _all_items if item.category in [DSRItemCategory.BOSSFOGWALL]]
        for item in bossfogwalls:
            item_pool.append(item)
            remaining_count = remaining_count - 1

    useful_items = [item for item in _all_items if item.category in [DSRItemCategory.EMBER, DSRItemCategory.FIRE_KEEPER_SOUL] ]
    for item in useful_items:
        item_pool.append(item)
        remaining_count = remaining_count - 1

    if world.options.soul_multiplier_steps > 0:
        for i in range(world.options.soul_multiplier_steps):
            item = item_dictionary["Progressive Soul Multiplier"]
            item_pool.append(item)
            remaining_count = remaining_count - 1

    if world.options.weight_multiplier_steps > 0:
        for i in range(world.options.weight_multiplier_steps):
            item = item_dictionary["Progressive Weight Reducer"]
            item_pool.append(item)
            remaining_count = remaining_count - 1

    world.random.shuffle(item_pool)
    return item_pool

def BuildGuaranteedItemPool(world):
    item_pool = []
    if world.options.guaranteed_items.value:
        for item_name, item_quant in world.options.guaranteed_items.value.items():
            item = item_dictionary[item_name]
            item_pool += [item] * item_quant
    world.random.shuffle(item_pool)
    return item_pool

def UpgradeEquipment(itemcode, options, world):
    upg = None
    if itemcode == None:
        return upg
    # Find the weapon by getting matching row from base
    for row in _all_items_base:
        if itemcode == row[1] + 11110000: 
            if (row[2] != DSRItemCategory.WEAPON and row[2] != DSRItemCategory.SHIELD): # Item found but not a weapon
                break
            if (world.random.randint(1,100) > options.upgraded_weapons_percentage): # Didn't roll RNG well enough
                break
            # print(f'upgrading, itemcode = {itemcode} name={row[0]}')
            # print(f'row {row[0]} {row[1]} {row[2]}')
            if row[4] == DSRUpgradeType.Infusable: 
                itype = world.random.choice([typ for typ in infusion_types if typ[0] in options.upgraded_weapons_allowed_infusions])
            elif row[4] == DSRUpgradeType.InfusableRestricted:
                allowed_types = [typ for typ in restricted_infusion_types if typ[0] in options.upgraded_weapons_allowed_infusions]
                if len(allowed_types) == 0: # Can't upgrade restricted items because none of its types are allowed
                    break;
                itype = world.random.choice(allowed_types)
            elif row[4] == DSRUpgradeType.Unique: 
                if "Normal" not in options.upgraded_weapons_allowed_infusions:
                    break
                itype = unique_infusion_types[0]
            else:
                break  # an unhandled infusion type - done with item

            # Normal types - exclude +0 weapon, and don't include name prefix
            if (itype[0] == "Normal"):
                minlvl = max(1, options.upgraded_weapons_min_level.value)
                # If min level > normal max, skip upg (happens for unique items))
                if options.upgraded_weapons_min_level.value > itype[2]:
                    break;
                maxlvl = min(itype[2], options.upgraded_weapons_max_level.value)
                
                if maxlvl < minlvl:  # could never use this infusion
                    break;  #done with item

                lvl = world.random.randint(minlvl, maxlvl)
                
                upg = f'{itype[0]}:{lvl}'
                # print(f"{upg}")
                return upg
            # Everything else - Include +0 weapon, and add prefix to name
            else:
                adj = options.upgraded_weapons_adjusted_levels

                # Min level is greater of "0" or "user specified min level minus potential adjustment"
                minlvl = max(0, options.upgraded_weapons_min_level.value - (itype[3] if adj else 0))
                # Min level is lesser of "type max level" or "user specified max level minus potential adjustment"
                maxlvl = min(itype[2], options.upgraded_weapons_max_level.value - (itype[3] if adj else 0))
                
                if maxlvl < minlvl:  # could never use this infusion
                    break;  #done with item
                lvl = world.random.randint(minlvl, maxlvl)
                
                upg = f'{itype[0]}:{lvl}'
                # print(f"{upg}")
                return upg
            break;
    return upg
