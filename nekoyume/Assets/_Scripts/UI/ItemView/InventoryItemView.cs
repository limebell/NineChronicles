using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.ItemView
{
    [RequireComponent(typeof(Button))]
    public class InventoryItemView : CountableItemView<Model.Inventory.Item>
    {
        public Image coverImage;
        public Image selectionImage;
        
        private Button _button;

        private readonly List<IDisposable> _dataDisposables = new List<IDisposable>();
        private IDisposable _buttonOnClickDisposable;

        #region Mono

        protected override void Awake()
        {
            base.Awake();
            
            if (ReferenceEquals(coverImage, null) ||
                ReferenceEquals(selectionImage, null))
            {
                throw new SerializeFieldNullException();
            }

            _button = gameObject.GetComponent<Button>();
            _buttonOnClickDisposable = _button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Data.OnClick.OnNext(Data);
                    AudioController.PlaySelect();
                });
        }

        private void OnDestroy()
        {
            _buttonOnClickDisposable.Dispose();
            _dataDisposables.ForEach(d => d.Dispose());
        }

        #endregion

        #region override

        public override void SetData(Model.Inventory.Item data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }
            
            base.SetData(data);
            
            _dataDisposables.ForEach(d => d.Dispose());

            Data.Covered.Subscribe(SetCover).AddTo(_dataDisposables);
            Data.Dimmed.Subscribe(SetDim).AddTo(_dataDisposables);
            Data.Selected.Subscribe(SetSelect).AddTo(_dataDisposables);

            Data.OnCountChanged.Subscribe(SetCount).AddTo(_dataDisposables);
            
            coverImage.enabled = Data.Covered.Value;
            selectionImage.enabled = Data.Selected.Value;
            SetDim(Data.Dimmed.Value);
        }
        
        public override void SetDim(bool isDim)
        {
            base.SetDim(isDim);
            
            selectionImage.color = isDim ? DimColor : DefaultColor;
        }

        public override void Clear()
        {
            base.Clear();
            
            _dataDisposables.ForEach(d => d.Dispose());
            
            Data = null;
            
            selectionImage.enabled = false;
        }

        #endregion

        private void SetCount(int count)
        {
            Count = count;
        }
        private void SetCover(bool isCover)
        {
            coverImage.enabled = isCover;
        }

        private void SetSelect(bool isSelect)
        {
            selectionImage.enabled = isSelect;
        }
    }
}
