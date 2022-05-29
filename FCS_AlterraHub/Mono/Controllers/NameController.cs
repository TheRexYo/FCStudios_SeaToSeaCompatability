﻿using System;
using FCSCommon.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace FCS_AlterraHub.Mono.Controllers
{
    public class NameController : MonoBehaviour
    {
        public Action<string, NameController> OnLabelChanged;
        public object Tag { get; set; }
        private string _title;
        private string _name;
        private string _submitLabel;
        private int _maxChar = 25;

        public void Initialize(string submitLabel, string title)
        {
            QuickLogger.Debug("Initializing Name Controller");
            _submitLabel = submitLabel;
            _title = title;
        }

        public void Show()
        {
            uGUI.main.userInput.RequestString(_title, _submitLabel, _name, _maxChar, SetCurrentName);
        }

        public string GetCurrentName()
        {
            return _name;
        }

        public void SetCurrentName(string name)
        {
            QuickLogger.Debug($"Setting unit name to : {name}");
            _name = name;
            OnLabelChanged?.Invoke(name, this);
        }

        public void SetCurrentName(string name, GameObject gameObject)
        {
            var textComponent = gameObject.GetComponent<Text>();

            if (textComponent == null)
            {
                QuickLogger.Error<NameController>("Text Component is null");
                return;
            }
            textComponent.text = name;

            SetCurrentName(name);
        }

        public void SetCurrentName(string name, Text textComponent)
        {
            if (textComponent == null)
            {
                QuickLogger.Error<NameController>("Text Component is null");
                return;
            }
            textComponent.text = name;

            SetCurrentName(name);
        }

        public void SetMaxChar(int amount)
        {
            _maxChar = amount;
        }
    }
}
