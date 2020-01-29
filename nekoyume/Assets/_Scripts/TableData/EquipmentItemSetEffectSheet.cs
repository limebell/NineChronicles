using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;

namespace Nekoyume.TableData
{
    [Serializable]
    public class EquipmentItemSetEffectSheet : Sheet<int, EquipmentItemSetEffectSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public Dictionary<int, StatModifier> StatModifiers { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                StatModifiers = new Dictionary<int, StatModifier>
                {
                    {
                        int.Parse(fields[1]),
                        new StatModifier(
                            (StatType) Enum.Parse(typeof(StatType), fields[2]),
                            (StatModifier.OperationType) Enum.Parse(typeof(StatModifier.OperationType), fields[3]),
                            int.Parse(fields[4]))
                    }
                };
            }
        }
        
        public EquipmentItemSetEffectSheet() : base(nameof(EquipmentItemSetEffectSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }
            
            if (value.StatModifiers.Count == 0)
                return;

            var pair = value.StatModifiers.First();
            if (row.StatModifiers.ContainsKey(pair.Key))
                throw new Exception($"[{nameof(EquipmentItemSetEffectSheet)}]Already contained key: {pair.Key}");
            
            row.StatModifiers.Add(pair.Key, pair.Value);
        }
    }

    public static class SetEffectExtension
    {
        public static List<EquipmentItemSetEffectSheet.Row> GetSetEffectRows(this EquipmentItemSetEffectSheet sheet, IEnumerable<Equipment> equipments)
        {
            var setInfo = new Dictionary<int, int>();
            foreach (var equipment in equipments)
            {
                var key = equipment.Data.SetId;
                if (!setInfo.ContainsKey(key))
                {
                    setInfo[key] = 0;
                }

                setInfo[key] += 1;
            }

            var rows = new List<EquipmentItemSetEffectSheet.Row>();
            foreach (var setInfoPair in setInfo)
            {
                if (!sheet.TryGetValue(setInfoPair.Key, out var row))
                    continue;

                foreach (var statModifierPair in row.StatModifiers)
                {
                    if (statModifierPair.Key > setInfoPair.Value)
                        break;

                    rows.Add(row);
                }
            }
            
            return rows;
        }
    }
}
