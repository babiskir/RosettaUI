﻿using System;
using RosettaUI.Builder;
using RosettaUI.Reactive;
using RosettaUI.UIToolkit.UnityInternalAccess;
using UnityEngine.UIElements;

namespace RosettaUI.UIToolkit.Builder
{
    public partial class UIToolkitBuilder
    {
        
        private VisualElement Build_DynamicElement(Element element)
        {
            var ve = new VisualElement();
            ve.AddToClassList(UssClassName.DynamicElement);
            return Build_ElementGroupContents(ve, element);
        }

        private VisualElement Build_Window(Element element)
        {
            var windowElement = (WindowElement) element;
            var window = new Window();
            window.TitleBarContainerLeft.Add(Build(windowElement.header));
            window.CloseButton.clicked += () => windowElement.Enable = !windowElement.Enable;

            windowElement.enableRx.SubscribeAndCallOnce(isOpen =>
            {
                if (isOpen)
                {
                    window.BringToFront();
                    window.Show();
                }
                
                else window.Hide();
            });
            
            // Focusable.ExecuteDefaultEvent() 内の this.focusController?.SwitchFocusOnEvent(evt) で
            // NavigationMoveEvent 方向にフォーカスを移動しようとする
            // キー入力をしている場合などにフォーカスが移ってしまうのは避けたいのでWindow単位で抑制しておく
            // UnityデフォルトでもTextFieldは抑制できているが、IntegerField.inputFieldでは出来ていないなど挙動に一貫性がない
            window.RegisterCallback<NavigationMoveEvent>(evt => evt.PreventDefault());

            Build_ElementGroupContents(window.contentContainer, element);
            return window;
        }

        private VisualElement Build_Fold(Element element)
        {
            var foldElement = (FoldElement) element;
            var fold = new Foldout();
            
            var toggle = fold.Q<Toggle>();
            toggle.Add(Build(foldElement.header));
            
            // disable 中でもクリック可能
            UIToolkitUtility.SetAcceptClicksIfDisabled(toggle);
            
            // Foldout 直下の Toggle は marginLeft が default.uss で書き換わるので上書きしておく
            // セレクタ例： .unity-foldout--depth-1 > .unity-fold__toggle
            toggle.style.marginLeft = 0;
            
            // Indentがあるなら１レベルキャンセル
            if (foldElement.CanMinusIndent())
            {
                fold.style.marginLeft = -LayoutSettings.IndentSize;
            }
            
            foldElement.IsOpenRx.SubscribeAndCallOnce(isOpen => fold.value = isOpen);
            fold.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == fold)
                {
                    foldElement.IsOpen = evt.newValue;
                }
            });

            var ret =  Build_ElementGroupContents(fold, foldElement);
            return ret;
        }

        private VisualElement Build_WindowLauncher(Element element)
        {
            var launcherElement = (WindowLauncherElement) element;
            var windowElement = launcherElement.window;
            var window = (Window) Build(windowElement);

            var toggle = Build_Field<bool, Toggle>(launcherElement, false);
            toggle.AddToClassList(UssClassName.WindowLauncher);
            toggle.RegisterCallback<PointerUpEvent>(OnPointUpEventFirst);

            var labelElement = launcherElement.label;
            labelElement.SubscribeValueOnUpdateCallOnce(v => toggle.text = v);

            return toggle;

            void OnPointUpEventFirst(PointerUpEvent evt)
            {
                toggle.UnregisterCallback<PointerUpEvent>(OnPointUpEventFirst);
                // panel==null（初回）はクリックした場所に出る
                // 以降は以前の位置に出る
                // Toggleの値が変わるのはこのイベントの後
                if (!windowElement.Enable && window.panel == null) window.Show(evt.originalMousePosition, toggle);
            }
        }

        private VisualElement Build_Row(Element element)
        {
            var row = CreateRowVisualElement();

            return Build_ElementGroupContents(row, element);
        }

        private static VisualElement CreateRowVisualElement()
        {
            var row = new VisualElement();
            row.AddToClassList(UssClassName.Row);
            return row;
        }

        private VisualElement Build_Column(Element element)
        {
            var column = new VisualElement();
            //column.AddToClassList(FieldClassName.Column);

            return Build_ElementGroupContents(column, element);
        }

        private VisualElement Build_Box(Element element)
        {
            var box = new Box();
            return Build_ElementGroupContents(box, element);
        }
        
        private VisualElement Build_HelpBox(Element element)
        {
            var helpBoxElement = (HelpBoxElement) element;
        
            var helpBox = new HelpBox(null, GetHelpBoxMessageType(helpBoxElement.helpBoxType));
            helpBoxElement.label.SubscribeValueOnUpdateCallOnce(str => helpBox.text = str);

            return helpBox;

            static HelpBoxMessageType GetHelpBoxMessageType(HelpBoxType helpBoxType)
            {
                return helpBoxType switch
                {
                    HelpBoxType.None => HelpBoxMessageType.None,
                    HelpBoxType.Info => HelpBoxMessageType.Info,
                    HelpBoxType.Warning => HelpBoxMessageType.Warning,
                    HelpBoxType.Error => HelpBoxMessageType.Error,
                    _ => throw new ArgumentOutOfRangeException(nameof(helpBoxType), helpBoxType, null)
                };
            }
        }
        

        VisualElement Build_ScrollView(Element element)
        {
            var scrollViewElement = (ScrollViewElement) element;
            var scrollViewMode = GetScrollViewMode(scrollViewElement.type);
            
            var scrollView = new ScrollView(scrollViewMode); 
            return Build_ElementGroupContents(scrollView, element);
            
            
            static ScrollViewMode GetScrollViewMode(ScrollViewType type)
            {
                return type switch
                {
                    ScrollViewType.Vertical => ScrollViewMode.Vertical,
                    ScrollViewType.Horizontal => ScrollViewMode.Horizontal,
                    ScrollViewType.VerticalAndHorizontal => ScrollViewMode.VerticalAndHorizontal,
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };
            }
        }

        VisualElement Build_Indent(Element element)
        {
            var indentElement = (IndentElement) element;
            
            var ve = new VisualElement
            {
                style =
                {
                    marginLeft = LayoutSettings.IndentSize * indentElement.level
                }
            };

            return Build_ElementGroupContents(ve, element);
        }

        private VisualElement Build_CompositeField(Element element)
        {
            var compositeFieldElement = (CompositeFieldElement) element;

            var field = new VisualElement();
            field.AddToClassList(UssClassName.UnityBaseField);
            field.AddToClassList(UssClassName.CompositeField);

            var labelElement = compositeFieldElement.header;
            if (labelElement != null)
            {
                var label = Build(labelElement);
                label.AddToClassList(UssClassName.UnityBaseFieldLabel);
                field.Add(label);
            }

            var contentContainer = new VisualElement();
            contentContainer.AddToClassList(UssClassName.CompositeFieldContents);
            field.Add(contentContainer);
            Build_ElementGroupContents(contentContainer, element);

            return field;
        }

        private VisualElement Build_ElementGroupContents(VisualElement container, Element element, Action<VisualElement, int> setupContentsVe = null)
        {
            var group = (ElementGroup) element;

            container.name = group.DisplayName;
            
            var i = 0;
            foreach (var ve in Build_ElementGroupContents(group))
            {
                setupContentsVe?.Invoke(ve, i);
                container.Add(ve);
                i++;
            }
            
            return container;
        }

    }
}