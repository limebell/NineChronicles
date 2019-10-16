using System;
using Assets.SimpleLocalization;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountPopup<T> : IDisposable where T : ItemCountPopup<T>
    {
        private int _originalCount;
        
        public readonly ReactiveProperty<string> TitleText = new ReactiveProperty<string>("");
        public readonly ReactiveProperty<CountEditableItem> Item = new ReactiveProperty<CountEditableItem>(null);
        public readonly ReactiveProperty<bool> CountEnabled = new ReactiveProperty<bool>(true);
        public readonly ReactiveProperty<string> SubmitText = new ReactiveProperty<string>("");
        
        public readonly Subject<T> OnClickMinus = new Subject<T>();
        public readonly Subject<T> OnClickPlus = new Subject<T>();
        public readonly Subject<T> OnClickSubmit = new Subject<T>();
        public readonly Subject<T> OnClickCancel = new Subject<T>();

        public ItemCountPopup()
        {
            SubmitText.Value = LocalizationManager.Localize("UI_OK");
            
            Item.Subscribe(value =>
            {
                if (ReferenceEquals(value, null))
                {
                    _originalCount = 0;
                    return;
                }
                
                _originalCount = value.Count.Value;
            });
            
            OnClickMinus.Subscribe(value =>
            {
                if (ReferenceEquals(value, null) ||
                    value.Item.Value.Count.Value <= Item.Value.MinCount.Value)
                {
                    return;
                }

                value.Item.Value.Count.Value--;
            });
            
            OnClickPlus.Subscribe(value =>
            {
                if (ReferenceEquals(value, null) ||
                    value.Item.Value.Count.Value >= Item.Value.MaxCount.Value)
                {
                    return;
                }

                value.Item.Value.Count.Value++;
            });

            OnClickCancel.Subscribe(value => value.Item.Value.Count.Value = _originalCount);
        }
        
        public virtual void Dispose()
        {
            TitleText.Dispose();
            Item.Dispose();
            CountEnabled.Dispose();
            SubmitText.Dispose();
            
            OnClickMinus.Dispose();
            OnClickPlus.Dispose();
            OnClickSubmit.Dispose();
            OnClickCancel.Dispose();
        }
    }
}
