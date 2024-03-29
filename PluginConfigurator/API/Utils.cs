﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API
{
    public static class Utils
    {
        public static void AddScrollEvents(EventTrigger trigger, ScrollRect scrollView)
        {
            EventTrigger.Entry entryBegin = new EventTrigger.Entry(), entryDrag = new EventTrigger.Entry(), entryEnd = new EventTrigger.Entry(), entrypotential = new EventTrigger.Entry()
                , entryScroll = new EventTrigger.Entry();

            entryBegin.eventID = EventTriggerType.BeginDrag;
            entryBegin.callback.AddListener((data) => { scrollView.OnBeginDrag((PointerEventData)data); });
            trigger.triggers.Add(entryBegin);

            entryDrag.eventID = EventTriggerType.Drag;
            entryDrag.callback.AddListener((data) => { scrollView.OnDrag((PointerEventData)data); });
            trigger.triggers.Add(entryDrag);

            entryEnd.eventID = EventTriggerType.EndDrag;
            entryEnd.callback.AddListener((data) => { scrollView.OnEndDrag((PointerEventData)data); });
            trigger.triggers.Add(entryEnd);

            entrypotential.eventID = EventTriggerType.InitializePotentialDrag;
            entrypotential.callback.AddListener((data) => { scrollView.OnInitializePotentialDrag((PointerEventData)data); });
            trigger.triggers.Add(entrypotential);

            entryScroll.eventID = EventTriggerType.Scroll;
            entryScroll.callback.AddListener((data) => { scrollView.OnScroll((PointerEventData)data); });
            trigger.triggers.Add(entryScroll);
        }

        public static void SetupResetButton(GameObject field, ScrollRect rect, UnityAction<BaseEventData> _mouseOn, UnityAction<BaseEventData> _mouseOff)
        {
            EventTrigger trigger = field.AddComponent<EventTrigger>();
            EventTrigger.Entry mouseOn = new EventTrigger.Entry() { eventID = EventTriggerType.PointerEnter };
            mouseOn.callback.AddListener(_mouseOn);
            EventTrigger.Entry mouseOff = new EventTrigger.Entry() { eventID = EventTriggerType.PointerExit };
            mouseOff.callback.AddListener(_mouseOff);
            trigger.triggers.Add(mouseOn);
            trigger.triggers.Add(mouseOff);
            AddScrollEvents(trigger, rect);
        }

        public static T GetComponentInParent<T>(Transform obj) where T : Component
        {
            T comp = obj.GetComponent<T>();
            if (comp != null)
                return comp;
            if (obj.parent == null)
                return null;
            return GetComponentInParent<T>(obj.parent);
        }
    }
}
