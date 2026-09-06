
using System.Collections.Generic;
using System.Linq;

namespace DSAP.Models
{
    public class ShopHintTrigger // map point of interest (rooms, entrances, etc)
    {
        public int ConditionFlag { get; set; }
        public List<long> HintLocs { get; set; }
        public List<(int proximity, MapPoi poi, List<int> flagList)> PositionConditionList {  get; set; }
        public ShopHintTrigger(int conditionFlag, IEnumerable<ShopLineupEntry> shopLineups, string prefix)
        {
            ConditionFlag = conditionFlag;
            HintLocs = shopLineups.Where(x => x.Name.StartsWith(prefix)).Select(x => (long)x.Id).ToList();
            PositionConditionList = [];
        }
        public ShopHintTrigger(int conditionFlag, IEnumerable<ShopLineupEntry> shopLineups, string prefix, List<(int proximity, MapPoi poi, List<int> flagList)> positionConditionList)
            : this (conditionFlag, shopLineups, prefix)
        {
            PositionConditionList = positionConditionList;
        }
    }
}
