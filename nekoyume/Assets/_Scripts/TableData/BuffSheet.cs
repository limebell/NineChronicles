using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Nekoyume.Model.Stat;
using UnityEngine;

namespace Nekoyume.TableData
{
    [Serializable]
    public class BuffSheet : Sheet<int, BuffSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }

            /// <summary>
            /// GroupId는 같은 GroupId를 공유하는 버프 중 가장 작은 값으로 함.
            /// Id가 다르더라도 GroupId가 같으면, 서로 덮어씀.
            /// 이때의 우선순위는 Id가 큰 것이 남는 것으로 구현함.
            /// </summary>
            public int GroupId { get; private set; }

            /// <summary>
            /// 100: 100%
            /// </summary>
            public int Chance { get; private set; }

            /// <summary>
            /// Turn count.
            /// </summary>
            public int Duration { get; private set; }

            public SkillTargetType TargetType { get; private set; }
            public StatModifier StatModifier { get; private set; }
            public string IconResource { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                GroupId = int.Parse(fields[1]);
                Chance = int.Parse(fields[2]);
                Duration = int.Parse(fields[3]);
                TargetType = (SkillTargetType) Enum.Parse(typeof(SkillTargetType), fields[4]);
                StatModifier = new StatModifier(
                    (StatType) Enum.Parse(typeof(StatType), fields[5]),
                    (StatModifier.OperationType) Enum.Parse(typeof(StatModifier.OperationType), fields[6]),
                    int.Parse(fields[7]));
                IconResource = fields[8];
            }
        }

        public BuffSheet() : base(nameof(BuffSheet))
        {
        }
    }

    public static class BuffSheetRowExtension
    {
        public static string GetLocalizedName(this BuffSheet.Row row)
        {
            return LocalizationManager.Localize($"BUFF_NAME_{row.Id}");
        }

        public static string GetLocalizedDescription(this BuffSheet.Row row)
        {
            return LocalizationManager.Localize($"BUFF_DESCRIPTION_{row.Id}");
        }

        public static Sprite GetIcon(this BuffSheet.Row row)
        {
            return SpriteHelper.GetBuffIcon(row.IconResource);
        }
    }
}
