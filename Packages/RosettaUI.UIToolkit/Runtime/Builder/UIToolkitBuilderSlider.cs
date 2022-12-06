﻿using System;
using RosettaUI.Reactive;
using UnityEngine;
using UnityEngine.UIElements;

#if !UNITY_2022_1_OR_NEWER
using RosettaUI.UIToolkit.UnityInternalAccess;
#endif

namespace RosettaUI.UIToolkit.Builder
{
    public partial class UIToolkitBuilder
    {
        private bool Bind_Slider<TValue, TSlider>(Element element, VisualElement visualElement)
            where TValue : IComparable<TValue>
            where TSlider : BaseSlider<TValue>, new()
        {
            if (element is not SliderElement<TValue> sliderElement || visualElement is not TSlider slider) return false;

            slider.showInputField = sliderElement.showInputField;
            
            BindRangeFieldElement(sliderElement,
                (min) => slider.lowValue = min,
                (max) => slider.highValue = max
            );
            
            return Bind_Field<TValue, TSlider>(element, visualElement);
        }

        private bool Bind_MinMaxSlider<TValue, TTextField>(Element element, VisualElement visualElement)
            where TTextField : TextValueField<TValue>, new()
        {
            if (element is not MinMaxSliderElement<TValue> sliderElement 
                || visualElement is not MinMaxSliderWithField<TValue, TTextField> slider) return false;
            
            Bind_ExistingLabel(sliderElement.Label,  slider.labelElement, str => slider.label = str);
            
            slider.ShowInputField = sliderElement.showInputField;
            
            
            var viewBridge = sliderElement.GetViewBridge();
            viewBridge.SubscribeValueOnUpdateCallOnce(minMax => slider.value = new Vector2(ToFloat(minMax.min), ToFloat(minMax.max)));
            slider.RegisterValueChangedCallback(OnValueChanged);
            viewBridge.onUnsubscribe +=  () => slider?.UnregisterValueChangedCallback(OnValueChanged);
            
            BindRangeFieldElement(sliderElement,
                (min) => slider.lowLimit = ToFloat(min),
                (max) => slider.highLimit = ToFloat(max)
            );
            

            return true;


            void OnValueChanged(ChangeEvent<Vector2> evt)
            {
                var minMax = MinMax.Create(
                    ToTValue(evt.newValue.x),
                    ToTValue(evt.newValue.y)
                    );
                
                viewBridge.SetValueFromView(minMax);
            }
            
            float ToFloat(TValue value) => MinMaxSliderWithField<TValue, TTextField>.ToFloat(value);
            TValue ToTValue(float floatValue) => MinMaxSliderWithField<TValue, TTextField>.ToTValue(floatValue);
        }


        private static void BindRangeFieldElement<T, TRange>(
            RangeFieldElement<T, TRange> rangeFieldElement,
            Action<TRange> updateMin,
            Action<TRange> updateMax
        )
        {
            if (rangeFieldElement.IsMinConst)
            {
                updateMin(rangeFieldElement.Min);
            }
            else
            {
                var disposable = rangeFieldElement.minRx.SubscribeAndCallOnce(updateMin);
                rangeFieldElement.GetViewBridge().onUnsubscribe += disposable.Dispose;
            }

            if (rangeFieldElement.IsMaxConst)
            {
                updateMax(rangeFieldElement.Max);
            }
            else
            {
                var disposable = rangeFieldElement.maxRx.SubscribeAndCallOnce(updateMax);
                rangeFieldElement.GetViewBridge().onUnsubscribe += disposable.Dispose;
            }
        }

    }
}