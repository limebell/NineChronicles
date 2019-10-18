using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Manager;
using UniRx;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class Combination : IDisposable
    {
        private static readonly ItemSubType[] DimmedTypes =
        {
            ItemSubType.Weapon,
            ItemSubType.RangedWeapon,
            ItemSubType.Armor,
            ItemSubType.Belt,
            ItemSubType.Necklace,
            ItemSubType.Ring,
            ItemSubType.Helm,
            ItemSubType.Set,
            ItemSubType.Food,
            ItemSubType.Shoes
        };

        public enum CombinationState
        {
            Consumable,
            Equipment,
            Enhancement,
            Recipe,
        }

        public readonly ReactiveProperty<CombinationState> State =
            new ReactiveProperty<CombinationState>(CombinationState.Equipment);

        public readonly ReactiveProperty<SimpleItemCountPopup> ItemCountPopup =
            new ReactiveProperty<SimpleItemCountPopup>();

        public readonly ReactiveProperty<CombinationMaterial> EquipmentMaterial =
            new ReactiveProperty<CombinationMaterial>();
        
        public readonly ReactiveCollection<CombinationMaterial> Materials =
            new ReactiveCollection<CombinationMaterial>();

        public readonly ReactiveProperty<EnhanceEquipment> enhanceEquipment = new ReactiveProperty<EnhanceEquipment>();

        public readonly ReactiveCollection<EnhanceEquipment> enhanceMaterials = new ReactiveCollection<EnhanceEquipment>();

        public readonly ReactiveProperty<int> ShowMaterialsCount = new ReactiveProperty<int>();
        public readonly ReactiveProperty<bool> ReadyToCombination = new ReactiveProperty<bool>();

        public readonly Subject<int> OnMaterialAdded = new Subject<int>();
        public readonly Subject<int> OnMaterialRemoved = new Subject<int>();
        
        public Combination()
        {
            ItemCountPopup.Value = new SimpleItemCountPopup();
            ItemCountPopup.Value.TitleText.Value =
                LocalizationManager.Localize("UI_COMBINATION_MATERIAL_COUNT_SELECTION");

            State.Subscribe(SubscribeState);
            ItemCountPopup.Value.OnClickSubmit.Subscribe(OnClickSubmitItemCountPopup);
            Materials.ObserveAdd().Subscribe(_ => OnMaterialAdd(_.Value));
            Materials.ObserveRemove().Subscribe(_ => OnMaterialRemove(_.Value));
        }

        public void Dispose()
        {
            State.Dispose();
            ItemCountPopup.DisposeAll();
            EquipmentMaterial.Dispose();
            Materials.DisposeAllAndClear();
            ShowMaterialsCount.Dispose();
            ReadyToCombination.Dispose();
            enhanceEquipment.Dispose();
        }

        private void SubscribeState(CombinationState value)
        {
            switch (value)
            {
                case CombinationState.Recipe:
                case CombinationState.Consumable:
                    ShowMaterialsCount.Value = 5;
                    break;
                case CombinationState.Enhancement:
                case CombinationState.Equipment:
                    ShowMaterialsCount.Value = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }

            RemoveEquipmentMaterial();

            while (Materials.Count > 0)
            {
                Materials.RemoveAt(0);
            }

            RemoveEnhanceEquipment();
        }

        private void OnClickSubmitItemCountPopup(SimpleItemCountPopup data)
        {
            if (ReferenceEquals(data, null)
                || ReferenceEquals(data.Item.Value, null))
            {
                ItemCountPopup.Value.Item.Value = null;
                return;
            }

            RegisterToStagedItems(data.Item.Value);
            ItemCountPopup.Value.Item.Value = null;
        }

        public void RemoveEquipmentMaterial()
        {
            if (EquipmentMaterial.Value == null)
            {
                return;
            }
            
            var temp = EquipmentMaterial.Value;
            EquipmentMaterial.Value = null;
            OnMaterialRemove(temp);
        }

        public void RemoveMaterialsAll()
        {
            RemoveEquipmentMaterial();
            
            while (Materials.Count > 0)
            {
                Materials.RemoveAt(0);
            }
        }

        public bool RegisterToStagedItems(CountableItem countEditableItem)
        {
            if (countEditableItem is null)
                return false;

            var sum = Materials
                .Where(item => countEditableItem.ItemBase.Value.Data.Id == item.ItemBase.Value.Data.Id)
                .Sum(item => item.Count.Value);

            if (sum >= countEditableItem.Count.Value)
                return false;

            if (State.Value == CombinationState.Enhancement)
            {
                // 강화할 아이템 우선 설정
                if (enhanceEquipment.Value is null)
                {
                    enhanceEquipment.Value = new EnhanceEquipment(countEditableItem.ItemBase.Value);
                    return true;
                }

                foreach (var enhanceMaterial in enhanceMaterials)
                {
                    if (countEditableItem.Count.Value == 0)
                    {
                        enhanceMaterials.Remove(enhanceMaterial);
                    }
                }

                if (enhanceMaterials.Count >= ShowMaterialsCount.Value)
                    return false;

                enhanceMaterials.Add(new EnhanceEquipment(countEditableItem.ItemBase.Value));
                return true;
            }

            if (State.Value == CombinationState.Equipment
                && countEditableItem.ItemBase.Value.Data.ItemSubType == ItemSubType.EquipmentMaterial)
            {
                RemoveEquipmentMaterial();

                EquipmentMaterial.Value = new CombinationMaterial(
                    countEditableItem.ItemBase.Value,
                    1,
                    0,
                    countEditableItem.Count.Value);
                OnMaterialAdd(EquipmentMaterial.Value);

                return true;
            }

            foreach (var material in Materials)
            {
                if (material.ItemBase.Value.Data.Id != 0)
                {
                    continue;
                }

                if (countEditableItem.Count.Value == 0)
                {
                    Materials.Remove(material);
                }

                return true;
            }

            if (Materials.Count >= ShowMaterialsCount.Value)
                return false;

            Materials.Add(new CombinationMaterial(
                countEditableItem.ItemBase.Value,
                1,
                0,
                countEditableItem.Count.Value));

            return true;
        }

        private void OnMaterialAdd(CombinationMaterial value)
        {
            value.Count.Subscribe(count => UpdateReadyForCombination());
            value.OnMinus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }

                if (obj.Count.Value > 1)
                {
                    obj.Count.Value--;
                }
            });
            value.OnPlus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }

                var sum = Materials
                    .Where(item => obj.ItemBase.Value.Data.Id == item.ItemBase.Value.Data.Id)
                    .Sum(item => item.Count.Value);
                if (sum < obj.MaxCount.Value)
                {
                    obj.Count.Value++;
                }
            });
            value.OnDelete.Subscribe(obj =>
            {
                if (!(obj is CombinationMaterial material))
                {
                    return;
                }

                if (State.Value == CombinationState.Equipment
                    && EquipmentMaterial.Value != null
                    && EquipmentMaterial.Value.ItemBase.Value.Data.Id == obj.ItemBase.Value.Data.Id)
                {
                    RemoveEquipmentMaterial();
                }
                else
                {
                    Materials.Remove(material);   
                }
                
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickCombinationRemoveMaterialItem);
            });

            OnMaterialAdded.OnNext(value.ItemBase.Value.Data.Id);
            UpdateReadyForCombination();
        }

        private void OnMaterialRemove(CombinationMaterial value)
        {
            value.Dispose();

            var materialId = value.ItemBase.Value.Data.Id;
            if (Materials.Any(item => item.ItemBase.Value.Data.Id == materialId))
            {
                OnMaterialAdded.OnNext(materialId);
            }
            else
            {
                OnMaterialRemoved.OnNext(materialId);
            }
            UpdateReadyForCombination();
        }

        private void UpdateReadyForCombination()
        {
            switch (State.Value)
            {
                case CombinationState.Recipe:
                case CombinationState.Consumable:
                    ReadyToCombination.Value =
                        Materials.Count >= 2 && States.Instance.currentAvatarState.Value.actionPoint >=
                        Action.Combination.RequiredPoint;
                    break;
                case CombinationState.Equipment:
                    ReadyToCombination.Value = 
                        EquipmentMaterial.Value != null
                        && Materials.Count >= 1
                        && States.Instance.currentAvatarState.Value.actionPoint >= Action.Combination.RequiredPoint;
                    break;
                case CombinationState.Enhancement:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void RemoveEnhanceEquipment()
        {
            enhanceEquipment.Value = null;

            while (enhanceMaterials.Count > 0)
            {
                enhanceMaterials.RemoveAt(0);
            }
        }
    }
}
