﻿using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ItemInformationTooltip : VerticalTooltipWidget<Model.ItemInformationTooltip>
    {
        public Text titleText;
        public Module.ItemInformation itemInformation;
        public GameObject submitGameObject;
        public GameObject footerRoot;
        public Button submitButton;
        public Text submitButtonText;
        public GameObject priceContainer;
        public Text priceText;
        
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public new Model.ItemInformationTooltip Model { get; private set; }

        public RectTransform Target => Model.target.Value;

        protected override void Awake()
        {
            base.Awake();
            
            Model = new Model.ItemInformationTooltip();
            submitButton.OnClickAsObservable().Subscribe(_ => Model.OnSubmit.OnNext(this)).AddTo(gameObject);
        }

        private void OnDestroy()
        {
            Model.Dispose();
            Model = null;
        }

        public void Show(RectTransform target, InventoryItem item, Action<ItemInformationTooltip> onClose = null)
        {
            Show(target, item, null, null, null, onClose);
        }

        public void Show(RectTransform target, InventoryItem item, Func<CountableItem, bool> submitEnabledFunc, string submitText, Action<ItemInformationTooltip> onSubmit, Action<ItemInformationTooltip> onClose = null)
        {
            if (item is null)
            {
                return;
            }

            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = target;
            Model.ItemInformation.item.Value = item;
            Model.SubmitButtonEnabledFunc.Value = submitEnabledFunc;
            Model.SubmitButtonText.Value = submitText;
            
            // Show(Model)을 먼저 호출함으로써 Widget.Show()가 호출되고, 게임 오브젝트가 활성화 됨. 그래야 레이아웃 정리가 가능함.
            Show(Model);
            // itemInformation UI의 모든 요소에 적절한 값이 들어가야 레이아웃 정리가 유효함.
            itemInformation.SetData(Model.ItemInformation);
            
            Model.TitleText.SubscribeToText(titleText).AddTo(_disposablesForModel);
            Model.PriceEnabled.Subscribe(priceContainer.SetActive).AddTo(_disposablesForModel);
            Model.PriceEnabled.SubscribeToBehaviour(priceText).AddTo(_disposablesForModel);
            Model.Price.SubscribeToText(priceText).AddTo(_disposablesForModel);
            Model.SubmitButtonText.SubscribeToText(submitButtonText).AddTo(_disposablesForModel);
            Model.SubmitButtonEnabled.Subscribe(submitGameObject.SetActive).AddTo(_disposablesForModel);
            Model.OnSubmit.Subscribe(onSubmit).AddTo(_disposablesForModel);
            if (onClose != null)
            {
                Model.OnClose.Subscribe(onClose).AddTo(_disposablesForModel);
            }
            Model.FooterRootActive.Subscribe(footerRoot.SetActive).AddTo(_disposablesForModel);
            // Model.itemInformation.item을 마지막으로 구독해야 위에서의 구독으로 인해 바뀌는 레이아웃 상태를 모두 반영할 수 있음.
            Model.ItemInformation.item.Subscribe(value => base.SubscribeTarget(Model.target.Value))
                .AddTo(_disposablesForModel);

            StartCoroutine(CoUpdate());
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Model.OnClose.OnNext(this);
            _disposablesForModel.DisposeAllAndClear();
            Model.target.Value = null;
            Model.ItemInformation.item.Value = null;
            base.Close(ignoreCloseAnimation);
        }
        
        protected override void SubscribeTarget(RectTransform target)
        {
            // 타겟이 바뀔 때, 아이템도 바뀌니 아이템을 구독하는 쪽 한 곳에 로직을 구현한다. 
        }

        private IEnumerator CoUpdate()
        {
            var temp = EventSystem.current.currentSelectedGameObject;
            
            while (enabled)
            {
                if (EventSystem.current.currentSelectedGameObject != temp)
                {
                    var current = EventSystem.current.currentSelectedGameObject;
                    if (current == submitButton.gameObject)
                    {
                        yield break;
                    }
                    
                    Close();
                    yield break;
                }
                
                yield return null;
            }
        }
    }
}
